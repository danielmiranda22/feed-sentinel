using System.Net;
using System.Text;
using FeedSentinel.Worker;

namespace FeedSentinel.Benchmarks;

/// <summary>
/// Answers every request with the same Atom feed after a fixed delay, to simulate network latency
/// without hitting a real server.
/// </summary>
public sealed class FakeFeedHandler(TimeSpan latency) : HttpMessageHandler
{
    private static readonly byte[] Feed = Encoding.UTF8.GetBytes(BuildFeed(entries: 10));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        await Task.Delay(latency, ct);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Feed) };
    }

    private static string BuildFeed(int entries)
    {
        var sb = new StringBuilder("""<?xml version="1.0" encoding="UTF-8"?><feed xmlns="http://www.w3.org/2005/Atom"><id>tag:test</id><title>Fake releases</title><updated>2026-01-01T00:00:00Z</updated>""");
        for (var i = 0; i < entries; i++) {
            sb.Append($"""<entry><id>tag:test/v1.0.{i}</id><title>v1.0.{i}</title><updated>2026-01-01T00:00:00Z</updated><link href="https://example.test/v1.0.{i}"/><content type="html">Release notes {i}</content></entry>""");
        }

        return sb.Append("</feed>").ToString();
    }
}

/// <summary>Never remembers anything: every entry is always new.</summary>
public sealed class NoOpSeenStore : ISeenStore
{
    public IReadOnlySet<string> GetSeen(Uri source) => new HashSet<string>();
    public Task SetSeenAsync(Uri source, IReadOnlySet<string> ids, CancellationToken ct) => Task.CompletedTask;
}

/// <summary>Discards everything: the benchmark measures fetching, not disk.</summary>
public sealed class NoOpWriter : IWriter
{
    public Task SaveDataAsync(Feed feed, string name, CancellationToken ct) => Task.CompletedTask;
}