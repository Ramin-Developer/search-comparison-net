namespace SearchComparisonNet.Benchmarks;

// Hypothesis under test: the OC indexer also taxes the recursive binary search. The variants run
// the same recursive algorithm and allocate the same SearchItem; only the data access differs
// (OC indexer vs raw int[]), so the timing delta isolates the indexer cost.
[SimpleJob(warmupCount: 3, iterationCount: 5)]
[CPUUsageDiagnoser]
public class BinarySearchBenchmarks
{
    private BinarySearch _binary = null!;
    private int[] _data = null!;
    private int[] _queries = null!;

    [Params(10_000, 100_000, 500_000)]
    public int NoOfEntries { get; set; }

    private const int NoOfSearches = 1_000;

    [GlobalSetup]
    public void Setup()
    {
        var workload = SearchWorkload.Build(NoOfEntries, NoOfSearches);
        _binary = new BinarySearch(workload.Generator);
        _data = workload.SortedArray;
        _queries = workload.Queries;
    }

    [Benchmark(Baseline = true)]
    public long ObservableCollectionIndexer()
    {
        long iterations = 0;
        foreach (var value in _queries)
        {
            iterations += _binary.FindItem(value).NoOfIterations;
        }

        return iterations;
    }

    [Benchmark]
    public long RawArray()
    {
        long iterations = 0;
        var data = _data;
        foreach (var value in _queries)
        {
            iterations += BinaryFind(data, 0, data.Length - 1, value, 0).NoOfIterations;
        }

        return iterations;
    }

    private static SearchItem BinaryFind(int[] data, int low, int high, int value, int noOfIterations)
    {
        noOfIterations++;
        if (low > high)
        {
            return new SearchItem { TargetIndex = null, TargetValue = value, NoOfIterations = noOfIterations };
        }

        var mid = (low + high) / 2;
        if (data[mid] == value)
        {
            return new SearchItem { TargetIndex = mid, TargetValue = value, NoOfIterations = noOfIterations };
        }

        return data[mid] > value
            ? BinaryFind(data, low, mid - 1, value, noOfIterations)
            : BinaryFind(data, mid + 1, high, value, noOfIterations);
    }
}
