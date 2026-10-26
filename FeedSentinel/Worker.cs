
using FeedSentinel.Worker.Models;
using Microsoft.Extensions.Options;

namespace FeedSentinel.Worker;
public class Worker(
    ILogger<Worker> logger,
    IHttpClientFactory httpClient,
    IOptions<WatcherOptions> options) : BackgroundService
{
    private readonly TimeSpan _interval = options.Value.Interval;
    private readonly string Url = options.Value.Source;
    private readonly string _outputPath = options.Value.OutputPath;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Sentinel started at: {time}", DateTimeOffset.Now);

        Directory.CreateDirectory(_outputPath);

        using var timer = new PeriodicTimer(_interval);

        do
        {
            logger.LogInformation("Requesting data: {time}", DateTimeOffset.Now);

            var client = httpClient.CreateClient();
            var resp = await client.GetAsync(Url, stoppingToken);
            var data = await resp.Content.ReadAsStringAsync(stoppingToken);

            await SaveDataAsync(data, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SaveDataAsync(string data, CancellationToken ct)
    {
        var fileName = $"posts-{DateTime.Now:yyyyMMdd-HHmmss}.json";
        var filePath = Path.Combine(_outputPath, fileName);

        await File.WriteAllTextAsync(filePath, data, ct);
        logger.LogInformation("Saved {Length} chars to {FilePath}", data.Length, filePath);
    }
}
