namespace SearchComparisonNet.Benchmarks;
// Hypothesis under test: in the real GUI flow (MainViewModel.SimulateLinearSearchAsync), the
// inner search loop calls IProgress<double>.Report(...) on EVERY iteration. This benchmark
// isolates that reporting overhead by running the identical, deterministic search loop four ways:
//   * NoReporting          - baseline: search loop only, no progress callbacks.
//   * ReportEveryIteration - original production: Report(...) once per query.
//   * ReportThrottled      - Report(...) only when the integer percent changes (<=100 calls).
//   * ReportTimeThrottled  - production fix: Report(...) at most once per 200 ms (time-based).
// All variants iterate the same prepared _queries array, so the timing delta isolates the cost
// of progress reporting and how throttling its frequency affects the search loop.
[SimpleJob(warmupCount: 3, iterationCount: 5)]
[CPUUsageDiagnoser]
public class ProgressReportingBenchmarks
{
    private LinearSearch _linear = null!;
    private int[] _queries = null!;
    private IProgress<double> _progress = null!;
    [Params(10_000, 100_000, 500_000)]
    public int NoOfEntries { get; set; }

    private const int NoOfSearches = 5_000;
    [GlobalSetup]
    public void Setup()
    {
        var workload = SearchWorkload.Build(NoOfEntries, NoOfSearches);
        _linear = new LinearSearch(workload.Generator);
        _queries = workload.Queries;
        // A no-op sink. We measure the Report(...) dispatch cost, not UI marshaling (a headless
        // harness has no UI SynchronizationContext), so this captures the per-call overhead and
        // the effect of call frequency on the hot loop.
        _progress = new Progress<double>(_ =>
        {
        });
    }

    [Benchmark(Baseline = true)]
    public long NoReporting()
    {
        long iterations = 0;
        foreach (var value in _queries)
        {
            iterations += _linear.FindItem(value).NoOfIterations;
        }

        return iterations;
    }

    [Benchmark]
    public long ReportEveryIteration()
    {
        long iterations = 0;
        for (var i = 0; i < _queries.Length; i++)
        {
            iterations += _linear.FindItem(_queries[i]).NoOfIterations;
            _progress.Report((i + 1) * 100.0 / _queries.Length);
        }

        return iterations;
    }

    [Benchmark]
    public long ReportThrottled()
    {
        long iterations = 0;
        var lastPercent = -1;
        for (var i = 0; i < _queries.Length; i++)
        {
            iterations += _linear.FindItem(_queries[i]).NoOfIterations;
            var percent = (i + 1) * 100 / _queries.Length;
            if (percent != lastPercent)
            {
                lastPercent = percent;
                _progress.Report(percent);
            }
        }

        return iterations;
    }

    [Benchmark]
    public long ReportTimeThrottled()
    {
        long iterations = 0;
        var stopwatch = Stopwatch.StartNew();
        var lastReportMs = -1L;
        const int minReportIntervalMs = 200;
        for (var i = 0; i < _queries.Length; i++)
        {
            iterations += _linear.FindItem(_queries[i]).NoOfIterations;
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            if (i == _queries.Length - 1 || elapsedMs - lastReportMs >= minReportIntervalMs)
            {
                lastReportMs = elapsedMs;
                _progress.Report((i + 1) * 100.0 / _queries.Length);
            }
        }

        return iterations;
    }
}