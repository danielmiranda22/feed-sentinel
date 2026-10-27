namespace FeedSentinel.Worker;

/// <summary>
/// Resposible for fetching the data to the correspondet endpoint
/// </summary>
public class FeedFetcher(IHttpClientFactory httpClientFactory) : IScraper
{
    public async Task<string> ScrapeDataAsync(Uri url, CancellationToken ct)
    {
        using var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}

public interface IScraper
{
    Task<string> ScrapeDataAsync(Uri url, CancellationToken ct);
}