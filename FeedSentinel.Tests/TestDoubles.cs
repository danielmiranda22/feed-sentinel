using System.Net;
using System.Text;

namespace FeedSentinel.Tests;

/// <summary>Builds a small Atom feed with the given entry ids.</summary>
public static class AtomFeed
{
    public static string With(params string[] ids)
    {
        var sb = new StringBuilder("""<?xml version="1.0" encoding="UTF-8"?><feed xmlns="http://www.w3.org/2005/Atom"><id>tag:test</id><title>Test releases</title><updated>2026-01-01T00:00:00Z</updated>""");
        foreach (var id in ids)
            sb.Append($"""<entry><id>{id}</id><title>Release {id}</title><updated>2026-01-01T00:00:00Z</updated><link href="https://example.test/{id}"/><content type="html">Notes for {id}</content></entry>""");
        return sb.Append("</feed>").ToString();
    }
}

/// <summary>Answers each URL with a fixed status and body, without any network.</summary>
public sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _responses = [];

    public StubHttpHandler Returns(string url, string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _responses[url] = (status, body);
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var (status, body) = _responses[request.RequestUri!.AbsoluteUri];
        return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}

/// <summary>A temporary folder deleted at the end of each test.</summary>
public sealed class TempDir : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "feedsentinel-tests-" + Guid.NewGuid().ToString("N"));

    public TempDir() => Directory.CreateDirectory(Path);

    public void Dispose() => Directory.Delete(Path, recursive: true);
}