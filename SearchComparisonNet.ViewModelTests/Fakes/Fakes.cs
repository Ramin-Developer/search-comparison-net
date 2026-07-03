namespace SearchComparisonNet.ViewModelTests.Fakes;

// A fixed search result. NoOfIterations feeds the average computation in the view model,
// so tests can assert the exact averages a run produces.
public sealed class FakeSearchItem : ISearchItem
{
    public int? TargetIndex { get; init; }

    public int TargetValue { get; init; }

    public int NoOfIterations { get; init; }
}

// Deterministic ISearch: FindItem always returns the same configured item. This removes
// randomness and timing from the simulation loop so the view model's command can be exercised
// quickly and repeatably. Probe generation now lives on ISearchComparison, not the search.
public sealed class FakeSearch : ISearch
{
    private readonly FakeSearchItem _item;

    public FakeSearch(int? targetIndex = 0, int targetValue = 0, int noOfIterations = 1)
    {
        _item = new FakeSearchItem
        {
            TargetIndex = targetIndex,
            TargetValue = targetValue,
            NoOfIterations = noOfIterations
        };
    }

    public int NoOfEntries { get; set; }

    public int this[int index] => 0;

    // Record the last value searched so target-lookup tests can assert what the view model passed in.
    public int? LastSearchedValue { get; private set; }

    // Optional hook invoked on every FindItem call. Cancellation tests use this to request
    // cancellation from inside the running simulation loop, making the cancel path deterministic.
    public Action? OnFindItem { get; set; }

    public ISearchItem FindItem(int value)
    {
        LastSearchedValue = value;
        OnFindItem?.Invoke();
        return _item;
    }
}

// Pairs a linear and a binary fake over a "shared" dataset, matching ISearchComparison.
// NextRandomNo returns a constant so the simulation loop stays deterministic.
public sealed class FakeSearchComparison : ISearchComparison
{
    private readonly int _nextValue;

    public FakeSearchComparison(ISearch linearSearch, ISearch binarySearch, int nextValue = 0)
    {
        LinearSearch = linearSearch;
        BinarySearch = binarySearch;
        _nextValue = nextValue;
    }

    public ISearch LinearSearch { get; }

    public ISearch BinarySearch { get; }

    public Func<int> NextRandomNo => () => _nextValue;
}

// Deterministic factory: returns a preconfigured comparison and records the noOfEntries it was
// asked to build, so simulation tests can assert the view model forwarded NoOfEntries correctly.
public sealed class FakeSearchComparisonFactory : ISearchComparisonFactory
{
    private readonly ISearchComparison _comparison;

    public FakeSearchComparisonFactory(ISearchComparison? comparison = null) =>
        _comparison = comparison ?? new FakeSearchComparison(new FakeSearch(), new FakeSearch());

    public int CreateCallCount { get; private set; }

    public int? LastNoOfEntries { get; private set; }

    public ISearchComparison Create(int noOfEntries)
    {
        CreateCallCount++;
        LastNoOfEntries = noOfEntries;
        return _comparison;
    }
}
