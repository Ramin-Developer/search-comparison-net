namespace SearchComparisonNet.Benchmarks;

// Hypothesis under test: MainViewModel.SimulateAsync runs the two searches sequentially, so a
// multicore machine sits near-idle. Compares the sequential linear-then-binary batch (current
// behavior) against running the two independent batches concurrently. Both searches only READ
// the shared sorted dataset, so concurrent reads are safe.
[SimpleJob(warmupCount: 3, iterationCount: 5)]
[CPUUsageDiagnoser]
public class SearchParallelismBenchmarks
{
    private LinearSearch _linear = null!;
    private BinarySearch _binary = null!;
    private int[] _queries = null!;

    // Focus on the typical run scale, where the sequential bottleneck is most visible.
    [Params(500_000)]
    public int NoOfEntries { get; set; }

    private const int NoOfSearches = 1_000;

    [GlobalSetup]
    public void Setup()
    {
        var workload = SearchWorkload.Build(NoOfEntries, NoOfSearches);
        // A single DataGenerator is shared by both strategies, preserving the comparison invariant.
        _linear = new LinearSearch(workload.Generator);
        _binary = new BinarySearch(workload.Generator);
        _queries = workload.Queries;
    }

    [Benchmark(Baseline = true)]
    public long Sequential()
    {
        return RunLinear() + RunBinary();
    }

    [Benchmark]
    public long Concurrent()
    {
        long linear = 0;
        long binary = 0;
        System.Threading.Tasks.Parallel.Invoke(
            () => linear = RunLinear(),
            () => binary = RunBinary());

        return linear + binary;
    }

    private long RunLinear()
    {
        long iterations = 0;
        foreach (var value in _queries)
        {
            iterations += _linear.FindItem(value).NoOfIterations;
        }

        return iterations;
    }

    private long RunBinary()
    {
        long iterations = 0;
        foreach (var value in _queries)
        {
            iterations += _binary.FindItem(value).NoOfIterations;
        }

        return iterations;
    }
}
