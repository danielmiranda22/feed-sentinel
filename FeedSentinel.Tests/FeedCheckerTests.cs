using System.Net;
using System.Text.Json;
using FeedSentinel.Worker;
using FeedSentinel.Worker.Config;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace FeedSentinel.Tests;

/// <summary>
/// End to end: real fetcher, parser, store and writer on a temp folder.
/// Only the network is fake.
/// </summary>
public class FeedCheckerTests : IDisposable
{
    private const string Runtime = "https://example.test/runtime/releases.atom";
    private const string AspNet = "https://example.test/aspnetcore/releases.atom";

    private readonly TempDir _dir = new();
    private readonly StubHttpHandler _http = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 3, 14, 30, 0, TimeSpan.Zero));

    public void Dispose() => _dir.Dispose();

    [Fact]
    public async Task Saves_only_the_entries_not_seen_before()
    {
        var checker = CreateChecker();

        _http.Returns(Runtime, AtomFeed.With("v1"));
        await CheckAsync(checker, Source("Runtime", Runtime));

        _http.Returns(Runtime, AtomFeed.With("v1", "v2"));
        await CheckAsync(checker, Source("Runtime", Runtime));

        Assert.Collection(SavedFeeds(),
            first => Assert.Equal(["v1"], Ids(first)),
            second => Assert.Equal(["v2"], Ids(second)));
    }

    [Fact]
    public async Task Does_not_repeat_entries_after_a_restart()
    {
        _http.Returns(Runtime, AtomFeed.With("v1", "v2"));

        await CheckAsync(CreateChecker(), Source("Runtime", Runtime));
        await CheckAsync(CreateChecker(), Source("Runtime", Runtime));   // new store = service restarted

        Assert.Single(SavedFeeds());
    }

    // An outage that returns an empty feed must not make every release look new next time.
    [Fact]
    public async Task An_empty_feed_does_not_make_old_entries_look_new()
    {
        var checker = CreateChecker();

        _http.Returns(Runtime, AtomFeed.With("v1", "v2"));
        await CheckAsync(checker, Source("Runtime", Runtime));

        _http.Returns(Runtime, AtomFeed.With());
        await CheckAsync(checker, Source("Runtime", Runtime));

        _http.Returns(Runtime, AtomFeed.With("v1", "v2"));
        await CheckAsync(checker, Source("Runtime", Runtime));

        Assert.Single(SavedFeeds());
    }

    [Fact]
    public async Task One_failing_source_does_not_stop_the_others()
    {
        _http.Returns(Runtime, "", HttpStatusCode.InternalServerError);
        _http.Returns(AspNet, AtomFeed.With("v1"));

        await CheckAsync(CreateChecker(), Source("Runtime", Runtime), Source("AspNet", AspNet));

        var file = Assert.Single(Directory.GetFiles(_dir.Path, "*-*.json"));
        Assert.StartsWith("AspNet-", Path.GetFileName(file));
    }

    private FeedChecker CreateChecker()
    {
        var options = Options.Create(new WatcherOptions { OutputPath = _dir.Path, MaxConcurrency = 4 });

        return new FeedChecker(
            NullLogger<FeedChecker>.Instance,
            options,
            new FeedFetcher(new HttpClient(_http)),
            new FeedParser(),
            new JsonSeenStore(options, NullLogger<JsonSeenStore>.Instance),
            new FeedWriter(options, NullLogger<FeedWriter>.Instance, _time));
    }

    private async Task CheckAsync(FeedChecker checker, params WatcherSource[] sources)
    {
        await checker.CheckAllFeedsAsync(sources, CancellationToken.None);

        // Files are named by the minute and second: move the clock so the next check gets its own file.
        _time.Advance(TimeSpan.FromMinutes(15));
    }

    // Every saved file except state.json, oldest first.
    private List<Feed> SavedFeeds() =>
        Directory.GetFiles(_dir.Path, "*-*.json")
            .Order()
            .Select(f => JsonSerializer.Deserialize<Feed>(File.ReadAllText(f))!)
            .ToList();

    private static IEnumerable<string> Ids(Feed feed) => feed.Items.Select(i => i.Id);

    private static WatcherSource Source(string name, string url) => new() { Name = name, Url = new Uri(url) };
}