namespace SearchComparisonNet.Tests.Common;

// Deterministic ISearch test double for exercising SimulationRunner without WPF. FindItem returns
// a fixed NoOfIterations, records how many times it was called, and invokes an optional hook so a
// test can request cancellation from inside the loop and assert the token is honored.
internal sealed class FakeSearch : ISearch
{
    private readonly int _noOfIterations;

    public FakeSearch(int noOfEntries = 100, int noOfIterations = 1)
    {
        NoOfEntries = noOfEntries;
        _noOfIterations = noOfIterations;
    }

    public int NoOfEntries { get; }

    // Records the number of FindItem calls so tests can assert exactly how many iterations ran
    // (e.g. the loop stopped early on cancellation).
    public int FindItemCallCount { get; private set; }

    // Optional hook invoked on every FindItem call; cancellation tests use it to cancel mid-run.
    public Action? OnFindItem { get; set; }

    public int this[int index] => index;

    public ISearchItem FindItem(int value)
    {
        FindItemCallCount++;
        OnFindItem?.Invoke();
        return new FakeSearchItem { NoOfIterations = _noOfIterations };
    }
}

// A fixed search result; only NoOfIterations matters for SimulationRunner's iteration total.
internal sealed class FakeSearchItem : ISearchItem
{
    public int? TargetIndex { get; init; }

    public int TargetValue { get; init; }

    public int NoOfIterations { get; init; }
}
