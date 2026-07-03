namespace SearchComparisonNet.Benchmarks;
[SimpleJob(warmupCount: 3, iterationCount: 5)]
[MemoryDiagnoser]
[CPUUsageDiagnoser]
public class DataGenerationBenchmarks
{
    // Typical run uses 500_000 entries; smaller sizes show how generation scales.
    [Params(10_000, 100_000, 500_000)]
    public int NoOfEntries { get; set; }

    // Constructing a DataGenerator eagerly produces one dataset in its constructor
    // (Data = GenerateData()), so each op measures a fresh generation. GenerateData is now a
    // private implementation detail (C-2), so the benchmark drives it via construction instead.
    [Benchmark]
    public int[] GenerateData() => new DataGenerator(new DataParameters(NoOfEntries)).Data;
}