using FeedSentinel.Worker.Config;
using FeedSentinel.Worker.Logging;
using Microsoft.Extensions.Options;

namespace FeedSentinel.Worker;

public interface IFeedChecker
{
    Task CheckAllFeedsAsync(IReadOnlyList<WatcherSource> sources, CancellationToken ct);
}

/// <summary>
/// Runs one check over all sources: fetch, parse, keep only new entries, save.
/// Up to <see cref="WatcherOptions.MaxConcurrency"/> sources are checked at the same time.
/// </summary>
public class FeedChecker(
    ILogger<FeedChecker> logger,
    IOptions<WatcherOptions> options,
    IFeedFetcher fetcher,
    IParser parser,
    ISeenStore seenStore,
    IWriter writer) : IFeedChecker
{
    private readonly int _maxConcurrency = options.Value.MaxConcurrency;

    public Task CheckAllFeedsAsync(IReadOnlyList<WatcherSource> sources, CancellationToken ct)
    {
        // MaxConcurrency = 1 is the old sequential loop.
        return Parallel.ForEachAsync(
            sources,
            new ParallelOptions { MaxDegreeOfParallelism = _maxConcurrency, CancellationToken = ct },
            async (source, token) => {
                try {
                    await CheckSourceAsync(source, token);
                } catch (Exception ex) when (!token.IsCancellationRequested) {
                    // One failing source must not stop the others.
                    logger.FetchFailed(source.Url!, ex);
                }
            });
    }

    private async Task CheckSourceAsync(WatcherSource source, CancellationToken ct)
    {
        var url = source.Url!;
        logger.RequestingData(url);

        var data = await fetcher.FetchAsync(url, ct);
        var feed = parser.Parse(data);
        var currentIds = feed.Items.Select(i => i.Id).ToHashSet();

        // An empty feed is suspicious (outage, bad response): keep the old state
        if (currentIds.Count == 0) {
            return;
        }

        var seen = seenStore.GetSeen(url);
        var newItems = feed.Items.Where(i => !seen.Contains(i.Id)).ToList();

        if (newItems.Count > 0) {
            await writer.SaveDataAsync(feed with { Items = newItems }, source.Name, ct);
            logger.NewEntriesFound(source.Name, newItems.Count);
        }

        await seenStore.SetSeenAsync(url, currentIds, ct);
    }
}
