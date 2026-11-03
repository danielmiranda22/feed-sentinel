using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using FeedSentinel.Worker;
using FeedSentinel.Worker.Config;
using Microsoft.Extensions.Logging.Abstractions;

namespace FeedSentinel.Benchmarks;

/// <summary>
/// One full check over N sources, each answering after a simulated network latency.
/// </summary>
[MemoryDiagnoser]
// Each operation takes seconds: run it a few times instead of BenchmarkDotNet's
// default (many short iterations), which would take far too long.
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 1, iterationCount: 5)]
public class CheckAllFeedsBenchmark
{
    private static readonly TimeSpan Latency = TimeSpan.FromMilliseconds(200);

    [Params(10, 50)]
    public int Sources;

    private FeedChecker _checker = null!;
    private List<WatcherSource> _sources = null!;

    [GlobalSetup]
    public void Setup()
    {
        var httpClient = new HttpClient(new FakeFeedHandler(Latency));

        _checker = new FeedChecker(
            NullLogger<FeedChecker>.Instance,
            new FeedFetcher(httpClient),
            new FeedParser(),
            new NoOpSeenStore(),
            new NoOpWriter());

        _sources = Enumerable.Range(0, Sources)
            .Select(i => new WatcherSource {
                Url = new Uri($"https://example.test/repo-{i}/releases.atom"),
                Name = $"Source {i}"
            })
            .ToList();
    }

    [Benchmark]
    public Task CheckAllFeeds() => _checker.CheckAllFeedsAsync(_sources, CancellationToken.None);
}