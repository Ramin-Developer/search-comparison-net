namespace SearchComparisonNet.Tests.Common;

// Deterministic IDataGenerator test double over a caller-supplied, already-sorted dataset.
// Lets the search algorithms be exercised against precise, repeatable data (empty, single
// element, boundaries) without the randomness of the production DataGenerator.
internal sealed class FakeDataGenerator : IDataGenerator
{
    private int _nextIndex;

    public FakeDataGenerator(params int[] data)
    {
        Data = data;
        NoOfEntries = data.Length;
        MinValue = data.Length == 0 ? 0 : data[0];
        MaxValue = data.Length == 0 ? 0 : data[^1];
    }

    public int NoOfEntries { get; }

    public int MinValue { get; }

    public int MaxValue { get; }

    public int[] Data { get; }

    // Returns the supplied values in order, cycling, so callers that need a probe value get a
    // deterministic, in-range result. Returns 0 for an empty dataset.
    public int NextRandomNo()
    {
        if (Data.Length == 0)
        { return 0; }

        var value = Data[_nextIndex % Data.Length];
        _nextIndex++;
        return value;
    }
}
