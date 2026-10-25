namespace FeedSentinel.Worker;

public class Worker(ILogger<Worker> logger, IHttpClientFactory httpClient) : BackgroundService
{
    private const int _interval = 5000;
    private const string url = "https://jsonplaceholder.typicode.com/posts";
    private readonly IHttpClientFactory httpClient = httpClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Sentinel started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Requesting data: {time}", DateTimeOffset.Now);

            var client = httpClient.CreateClient();
            var resp = await client.GetAsync(url);

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
