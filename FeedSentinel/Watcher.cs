using FeedSentinel.Worker.Config;
using FeedSentinel.Worker.Logging;
using Microsoft.Extensions.Options;

namespace FeedSentinel.Worker;

/// <summary>
/// Runs a feed check at a fixed interval.
/// </summary>
public class Watcher(
    ILogger<Watcher> logger,
    IOptions<WatcherOptions> options,
    IFeedChecker feedChecker
) : BackgroundService
{
    private readonly TimeSpan _interval = options.Value.Interval;
    private readonly List<WatcherSource> _sources = options.Value.Sources ?? [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.SentinelStarted(_sources.Count, _interval);

        // Make every insecure opt-in visible in the logs.
        foreach (var source in _sources.Where(s => s.Url!.Scheme == Uri.UriSchemeHttp)) {
            logger.InsecureSourceAllowed(source.Name, source.Url!);
        }

        using var timer = new PeriodicTimer(_interval);

        do {
            await feedChecker.CheckAllFeedsAsync(_sources, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}