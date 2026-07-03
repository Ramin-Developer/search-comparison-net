namespace SearchComparisonNet.Benchmarks;

// Hypothesis under test: the ObservableCollection<int> indexer dominates the linear-search
// inner loop. Both variants run the same algorithm and allocate one SearchItem per query; the
// only difference is OC indexer vs raw int[] access, so the timing delta isolates the indexer
// cost while the allocation diagnoser confirms SearchItem allocations are identical.
[SimpleJob(warmupCount: 3, iterationCount: 5)]
[CPUUsageDiagnoser]
public class LinearSearchBenchmarks
{
    private LinearSearch _linear = null!;
    private int[] _data = null!;
    private int[] _queries = null!;

    [Params(10_000, 100_000, 500_000)]
    public int NoOfEntries { get; set; }

    private const int NoOfSearches = 1_000;

    [GlobalSetup]
    public void Setup()
    {
        var workload = SearchWorkload.Build(NoOfEntries, NoOfSearches);
        _linear = new LinearSearch(workload.Generator);
        _data = workload.SortedArray;
        _queries = workload.Queries;
    }

    [Benchmark(Baseline = true)]
    public long ObservableCollectionIndexer()
    {
        long iterations = 0;
        foreach (var value in _queries)
        {
            iterations += _linear.FindItem(value).NoOfIterations;
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
            int? targetIndex = null;
            var noOfIterations = 0;
            for (var i = 0; i < data.Length; i++)
            {
                noOfIterations++;
                if (data[i] < value)
                {
                    continue;
                }

                if (data[i] > value)
                {
                    break;
                }

                targetIndex = i;
                break;
            }

            var item = new SearchItem
            {
                TargetIndex = targetIndex,
                TargetValue = value,
                NoOfIterations = noOfIterations
            };
            iterations += item.NoOfIterations;
        }

        return iterations;
    }
}
