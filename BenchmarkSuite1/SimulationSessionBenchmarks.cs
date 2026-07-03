namespace SearchComparisonNet.Benchmarks;
// Lifetime/retention check for the repeated-use pattern: every MainViewModel.SimulateAsync run
// builds a brand-new SearchComparison (fresh DataGenerator + dataset) via the factory and runs
// both search batches. This benchmark reproduces ONE such session per op so the MemoryDiagnoser
// can reveal whether repeating sessions leaks/retains memory or simply churns transient,
// GC-reclaimed allocations. The UI-only Progress<T>/Task.Run plumbing is intentionally excluded
// (a headless harness cannot model UI marshaling, and it was already shown immaterial).
[SimpleJob(warmupCount: 3, iterationCount: 5)]
[CPUUsageDiagnoser]
public class SimulationSessionBenchmarks
{
    private const int QuerySeed = 12345;
    private const int NoOfSearches = 5_000;
    private SearchComparisonFactory _factory = null!;
    private int[] _queries = null!;
    [Params(10_000, 100_000, 500_000)]
    public int NoOfEntries { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _factory = new SearchComparisonFactory();
        var dataParams = new DataParameters(NoOfEntries);
        var rng = new Random(QuerySeed);
        _queries = new int[NoOfSearches];
        for (var i = 0; i < NoOfSearches; i++)
        {
            _queries[i] = rng.Next(dataParams.MinEntryValue, dataParams.MaxEntryValue);
        }
    }

    [Benchmark]
    public long RunSession()
    {
        // Mirror one SimulateAsync invocation: new dataset + both searches from the factory.
        var comparison = _factory.Create(NoOfEntries);
        long iterations = 0;
        foreach (var value in _queries)
        {
            iterations += comparison.LinearSearch.FindItem(value).NoOfIterations;
        }

        foreach (var value in _queries)
        {
            iterations += comparison.BinarySearch.FindItem(value).NoOfIterations;
        }

        return iterations;
    }
}