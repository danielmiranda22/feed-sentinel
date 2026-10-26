namespace FeedSentinel.Worker;

public class Worker(ILogger<Worker> logger, IHttpClientFactory httpClient) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
    private const string url = "https://jsonplaceholder.typicode.com/posts";
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Sentinel started at: {time}", DateTimeOffset.Now);

        using var timer = new PeriodicTimer(_interval);

        do
        {
            logger.LogInformation("Requesting data: {time}", DateTimeOffset.Now);

            var client = httpClient.CreateClient();
            var resp = await client.GetAsync(url);

        }
        while (!stoppingToken.IsCancellationRequested);
    }
}
