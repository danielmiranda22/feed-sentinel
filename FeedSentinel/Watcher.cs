using FeedSentinel.Worker.Config;
using Microsoft.Extensions.Options;

namespace FeedSentinel.Worker;

public class Watcher(
    ILogger<Watcher> logger,
    IOptions<WatcherOptions> options,
    IScraper scraper,
    IWriter writer,
    IParser parser) : BackgroundService
{
    private readonly TimeSpan _interval = options.Value.Interval;
    private readonly List<WatcherSource> _sources = options.Value.Sources ?? [];
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.SentinelStarted(_sources, _interval);

        using var timer = new PeriodicTimer(_interval);

        do {
            foreach (var source in _sources) {
                try {
                    logger.RequestingData(source.Url!);

                    var data = await scraper.ScrapeDataAsync(source.Url!, stoppingToken);
                    var parsedData = parser.Parse(data, stoppingToken);
                    await writer.SaveDataAsync(parsedData, source.Name, stoppingToken);

                } catch (Exception ex) when (!stoppingToken.IsCancellationRequested) {
                    logger.FetchFailed(source.Url!, ex);
                }
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}