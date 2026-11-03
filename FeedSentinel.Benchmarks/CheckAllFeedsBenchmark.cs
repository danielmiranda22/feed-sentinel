using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using FeedSentinel.Worker;
using FeedSentinel.Worker.Config;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FeedSentinel.Benchmarks;

/// <summary>
/// One full check over N sources, each answering after a simulated network latency.
/// Compares checking sources one at a time (baseline) with checking them concurrently.
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

    // Only used by Concurrent(); Sequential() always uses 1.
    [Params(4, 8)]
    public int MaxConcurrency;

    private FeedChecker _sequential = null!;
    private FeedChecker _concurrent = null!;
    private List<WatcherSource> _sources = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Same production code for both benchmarks: only MaxConcurrency changes.
        _sequential = CreateChecker(maxConcurrency: 1);
        _concurrent = CreateChecker(MaxConcurrency);

        _sources = Enumerable.Range(0, Sources)
            .Select(i => new WatcherSource {
                Url = new Uri($"https://example.test/repo-{i}/releases.atom"),
                Name = $"Source {i}"
            })
            .ToList();
    }

    [Benchmark(Baseline = true)]
    public Task Sequential() =>
        _sequential.CheckAllFeedsAsync(_sources, CancellationToken.None);

    [Benchmark]
    public Task Concurrent() =>
        _concurrent.CheckAllFeedsAsync(_sources, CancellationToken.None);

    private static FeedChecker CreateChecker(int maxConcurrency) =>
        new(
            NullLogger<FeedChecker>.Instance,
            Options.Create(new WatcherOptions { MaxConcurrency = maxConcurrency }),
            new FeedFetcher(new HttpClient(new FakeFeedHandler(Latency))),
            new FeedParser(),
            new NoOpSeenStore(),
            new NoOpWriter());
}
