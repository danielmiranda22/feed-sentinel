using FeedSentinel.Worker.Config;
using Microsoft.Extensions.Options;

namespace FeedSentinel.Worker;

public class Worker(
    ILogger<Worker> logger,
    IHttpClientFactory httpClientFactory,
    IOptions<WatcherOptions> options) : BackgroundService
{
    private readonly TimeSpan _interval = options.Value.Interval;
    private readonly Uri _url = options.Value.Source!;
    private readonly string _outputPath = options.Value.OutputPath;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.SentinelStarted(_url, _interval);

        Directory.CreateDirectory(_outputPath);

        using var timer = new PeriodicTimer(_interval);

        do {
            try {
                logger.RequestingData(_url);

                var client = httpClientFactory.CreateClient();
                using var resp = await client.GetAsync(_url, stoppingToken);
                resp.EnsureSuccessStatusCode();

                var data = await resp.Content.ReadAsStringAsync(stoppingToken);
                await SaveDataAsync(data, stoppingToken);
            } catch (Exception ex) when (!stoppingToken.IsCancellationRequested) {
                logger.FetchFailed(_url, ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SaveDataAsync(string data, CancellationToken ct)
    {
        var fileName = $"posts-{DateTime.Now:yyyyMMdd-HHmmss}.json";
        var filePath = Path.Combine(_outputPath, fileName);

        await File.WriteAllTextAsync(filePath, data, ct);
        logger.DataSaved(data.Length, filePath);
    }
}