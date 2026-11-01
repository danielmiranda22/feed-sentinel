namespace FeedSentinel.Worker;

public interface IFeedFetcher
{
    Task<string> FetchAsync(Uri url, CancellationToken ct);
}

/// <summary>
/// Downloads the raw content of a feed.
/// </summary>
public class FeedFetcher(HttpClient httpClient) : IFeedFetcher
{
    public async Task<string> FetchAsync(Uri url, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}

