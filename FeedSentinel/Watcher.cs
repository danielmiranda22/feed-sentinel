using FeedSentinel.Worker.Config;
using FeedSentinel.Worker.Logging;
using Microsoft.Extensions.Options;

namespace FeedSentinel.Worker;

public class Watcher(
    ILogger<Watcher> logger,
    IOptions<WatcherOptions> options,
    IFeedFetcher fetcher,
    IParser parser,
    ISeenStore seenStore,
    IWriter writer) : BackgroundService
{
    private readonly TimeSpan _interval = options.Value.Interval;
    private readonly List<WatcherSource> _sources = options.Value.Sources ?? [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.SentinelStarted(_sources.Count, _interval);

        // Make every insecure opt-in visible in the logs.
        foreach (var source in _sources.Where(s => s.Url!.Scheme == Uri.UriSchemeHttp))
            logger.InsecureSourceAllowed(source.Name, source.Url!);

        using var timer = new PeriodicTimer(_interval);

        do {
            foreach (var source in _sources) {
                try {
                    await CheckSourceAsync(source, stoppingToken);
                } catch (Exception ex) when (!stoppingToken.IsCancellationRequested) {
                    logger.FetchFailed(source.Url!, ex);
                }
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckSourceAsync(WatcherSource source, CancellationToken ct)
    {
        var url = source.Url!;
        logger.RequestingData(url);

        var data = await fetcher.FetchAsync(url, ct);
        var feed = parser.Parse(data);
        var currentIds = feed.Items.Select(i => i.Id).ToHashSet();

        // An empty feed is suspicious (outage, bad response): keep the old state
        if (currentIds.Count == 0)
            return;

        var seen = seenStore.GetSeen(url);
        var newItems = feed.Items.Where(i => !seen.Contains(i.Id)).ToList();

        if (newItems.Count > 0) {
            await writer.SaveDataAsync(feed with { Items = newItems }, source.Name, ct);
            logger.NewEntriesFound(source.Name, newItems.Count);
        }

        await seenStore.SetSeenAsync(url, currentIds, ct);
    }
}