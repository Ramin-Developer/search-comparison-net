namespace SearchComparisonNet.Benchmarks;

// Builds a single shared dataset (via the production DataGenerator) plus a raw int[] copy of
// the same sorted data and a deterministic batch of query values, so every benchmark variant
// searches identical inputs and the only difference under test is the data access path.
internal static class SearchWorkload
{
    public const int QuerySeed = 12345;

    public static (DataGenerator Generator, int[] SortedArray, int[] Queries) Build(int noOfEntries, int noOfSearches)
    {
        var dataParams = new DataParameters(noOfEntries);
        var generator = new DataGenerator(dataParams);   // sorted int[] dataset
        var sortedArray = generator.Data.ToArray();      // identical data as a raw int[]

        var rng = new Random(QuerySeed);
        var queries = new int[noOfSearches];
        for (var i = 0; i < noOfSearches; i++)
        {
            queries[i] = rng.Next(dataParams.MinEntryValue, dataParams.MaxEntryValue);
        }

        return (generator, sortedArray, queries);
    }
}
