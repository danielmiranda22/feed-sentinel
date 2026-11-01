using FeedSentinel.Worker.Config;
using FeedSentinel.Worker.Logging;
using Microsoft.Extensions.Options;

namespace FeedSentinel.Worker;

public class Watcher(
    ILogger<Watcher> logger,
    IOptions<WatcherOptions> options,
    IFeedFetcher fetcher,
    IParser parser,
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
                    logger.RequestingData(source.Url!);

                    var data = await fetcher.FetchAsync(source.Url!, stoppingToken);
                    var feed = parser.Parse(data);
                    await writer.SaveDataAsync(feed, source.Name, stoppingToken);

                } catch (Exception ex) when (!stoppingToken.IsCancellationRequested) {
                    logger.FetchFailed(source.Url!, ex);
                }
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}