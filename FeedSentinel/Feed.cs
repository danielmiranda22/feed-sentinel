namespace FeedSentinel.Worker;

public record Feed(string Title, string Description, List<FeedItem> Items);
public record FeedItem(string Id, string Title, string Link, string Description, DateTimeOffset LastUpdatedTime);
