using FeedSentinel.Worker.Config;
using FeedSentinel.Worker.Logging;

namespace FeedSentinel.Worker;

public class FeedChecker(
    ILogger<FeedChecker> logger,
    IFeedFetcher fetcher,
    IParser parser,
    ISeenStore seenStore,
    IWriter writer) : IFeedChecker
{

    public async Task CheckAllFeedsAsync(List<WatcherSource> sources, CancellationToken ct)
    {
        foreach (var source in sources) {
            try {
                await CheckSourceAsync(source, ct);
            } catch (Exception ex) when (!ct.IsCancellationRequested) {
                logger.FetchFailed(source.Url!, ex);
            }
        }
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

public interface IFeedChecker
{
    Task CheckAllFeedsAsync(List<WatcherSource> sources, CancellationToken ct);
}