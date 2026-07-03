namespace SearchComparisonNet.ViewModelTests;

// These tests pin down the TargetValue -> TargetIndex behavior. The lookup is explicit: it runs
// via SearchCommand (the Search button / Enter key), not implicitly when TargetValue changes.
// The binary search used for the lookup is only assigned while a simulation runs, so tests
// that need a non-null lookup first execute SimulateCommand against a configured fake.
public class MainViewModelTargetValueTests
{
    private static MainViewModel CreateWithBinaryTargetIndex(int? targetIndex)
    {
        var binary = new FakeSearch(targetIndex: targetIndex);
        var comparison = new FakeSearchComparison(new FakeSearch(), binary);
        return ViewModelFactory.Create(out _, comparison);
    }

    [Fact]
    public void Target_index_is_null_before_any_simulation()
    {
        var sut = ViewModelFactory.Create();

        sut.TargetValue = 123;
        sut.SearchCommand.Execute(null);

        // No simulation has run, so BinarySearch is null and the search is a no-op.
        Assert.Null(sut.TargetIndex);
        Assert.False(sut.TargetIndexNotFound);
    }

    [Fact]
    public async Task Searching_after_simulation_resolves_target_index()
    {
        var sut = CreateWithBinaryTargetIndex(targetIndex: 7);
        await sut.SimulateCommand.ExecuteAsync(null);

        sut.TargetValue = 42;
        sut.SearchCommand.Execute(null);

        Assert.Equal(7, sut.TargetIndex);
        Assert.False(sut.TargetIndexNotFound);
    }

    [Fact]
    public async Task Searching_forwards_value_to_binary_search()
    {
        var binary = new FakeSearch(targetIndex: 3);
        var comparison = new FakeSearchComparison(new FakeSearch(), binary);
        var sut = ViewModelFactory.Create(out _, comparison);
        await sut.SimulateCommand.ExecuteAsync(null);

        sut.TargetValue = 99;
        sut.SearchCommand.Execute(null);

        // The exact value set on TargetValue must reach FindItem unchanged (no lossy parse).
        Assert.Equal(99, binary.LastSearchedValue);
    }

    [Fact]
    public async Task Negative_target_value_is_forwarded_unchanged()
    {
        var binary = new FakeSearch(targetIndex: 3);
        var comparison = new FakeSearchComparison(new FakeSearch(), binary);
        var sut = ViewModelFactory.Create(out _, comparison);
        await sut.SimulateCommand.ExecuteAsync(null);

        sut.TargetValue = -250;
        sut.SearchCommand.Execute(null);

        Assert.Equal(-250, binary.LastSearchedValue);
    }

    [Fact]
    public async Task Not_found_value_reports_minus_one_and_not_found_flag()
    {
        // The fake resolves every lookup to TargetIndex = null, i.e. "value absent from the dataset".
        var sut = CreateWithBinaryTargetIndex(targetIndex: null);
        await sut.SimulateCommand.ExecuteAsync(null);

        sut.TargetValue = 12345;
        sut.SearchCommand.Execute(null);

        Assert.Equal(-1, sut.TargetIndex);
        Assert.True(sut.TargetIndexNotFound);
        Assert.Equal("Not Found", sut.TargetIndexTooltip);
    }

    [Fact]
    public async Task Changing_target_value_clears_previous_not_found_state()
    {
        var sut = CreateWithBinaryTargetIndex(targetIndex: null);
        await sut.SimulateCommand.ExecuteAsync(null);
        sut.TargetValue = 12345;
        sut.SearchCommand.Execute(null);
        Assert.True(sut.TargetIndexNotFound);

        // Editing the value must clear the stale "Not found" feedback before the next search.
        sut.TargetValue = 999;

        Assert.False(sut.TargetIndexNotFound);
        Assert.Null(sut.TargetIndexTooltip);
    }

    [Fact]
    public void Search_command_is_disabled_when_target_value_is_not_a_valid_integer()
    {
        var sut = ViewModelFactory.Create();

        // WPF sets the bound int? to null when the entered text is not a valid integer.
        sut.TargetValue = null;
        Assert.False(sut.SearchCommand.CanExecute(null));

        sut.TargetValue = 5;
        Assert.True(sut.SearchCommand.CanExecute(null));
    }
}
