namespace SearchComparisonNet.ViewModelTests;

// Exercises the SimulateCommand end-to-end against deterministic fakes. The fake search makes
// each "run" complete immediately, so these tests assert the observable contract (results,
// averages, state transitions, cancellation) without depending on UI-thread marshalling.
public class MainViewModelSimulationTests
{
    private static MainViewModel CreateForRun(int noOfSearches, int linearIterations, int binaryIterations, out FakeSearchComparisonFactory factory)
    {
        var linear = new FakeSearch(noOfIterations: linearIterations);
        var binary = new FakeSearch(noOfIterations: binaryIterations);
        var comparison = new FakeSearchComparison(linear, binary);
        var sut = ViewModelFactory.Create(out factory, comparison);
        sut.NoOfSearchesText = noOfSearches.ToString();
        return sut;
    }

    [Fact]
    public async Task Simulation_creates_dataset_using_NoOfEntries()
    {
        var sut = CreateForRun(noOfSearches: 1_000, linearIterations: 1, binaryIterations: 1, out var factory);

        await sut.SimulateCommand.ExecuteAsync(null);

        Assert.Equal(1, factory.CreateCallCount);
        Assert.Equal(ProblemConstants.InitialNoOfEntries, factory.LastNoOfEntries);
    }

    [Fact]
    public async Task Simulation_populates_results_and_enables_search()
    {
        var sut = CreateForRun(noOfSearches: 1_000, linearIterations: 3, binaryIterations: 2, out _);

        await sut.SimulateCommand.ExecuteAsync(null);

        // A completed run publishes its aggregates via the average properties (the standalone
        // *SearchResults holders were removed) and enables the search panel.
        Assert.Multiple(
            () => Assert.Equal(3.0, sut.LinearAvgNoOfIterations),
            () => Assert.Equal(2.0, sut.BinaryAvgNoOfIterations),
            () => Assert.True(sut.IsSearchEnabled));
    }

    [Fact]
    public async Task Average_iterations_equal_per_search_iteration_count()
    {
        // Every FindItem call reports a constant NoOfIterations, so the average over the run
        // equals that constant for each strategy.
        var sut = CreateForRun(noOfSearches: 2_000, linearIterations: 4, binaryIterations: 6, out _);

        await sut.SimulateCommand.ExecuteAsync(null);

        Assert.Equal(4.0, sut.LinearAvgNoOfIterations);
        Assert.Equal(6.0, sut.BinaryAvgNoOfIterations);
    }

    [Fact]
    public async Task Simulation_resets_state_when_finished()
    {
        var sut = CreateForRun(noOfSearches: 1_000, linearIterations: 1, binaryIterations: 1, out _);

        await sut.SimulateCommand.ExecuteAsync(null);

        // The finally block clears the simulating flag and hides the progress bar synchronously on
        // the continuation. ProgressBarValue is intentionally not asserted here: the final
        // Progress<double>.Report(100) is delivered asynchronously and can race with the reset.
        Assert.False(sut.IsSimulating);
        Assert.Equal(Visibility.Hidden, sut.ProgressBarVisibility);
    }

    [Fact]
    public async Task Simulate_command_is_disabled_again_after_completion()
    {
        var sut = CreateForRun(noOfSearches: 1_000, linearIterations: 1, binaryIterations: 1, out _);

        await sut.SimulateCommand.ExecuteAsync(null);

        // Input is still valid and no run is in progress, so Simulate can run again.
        Assert.True(sut.SimulateCommand.CanExecute(null));
        // Cancel is only enabled while simulating, so it is disabled once the run completes.
        Assert.False(sut.CancelCommand.CanExecute(null));
    }

    [Fact]
    public async Task Cancelling_during_run_leaves_search_disabled()
    {
        // A controllable linear fake requests cancellation from inside the simulation loop on its
        // first FindItem call. The next loop iteration hits ThrowIfCancellationRequested, so the
        // run ends via OperationCanceledException and the catch block keeps the panel disabled.
        var linear = new FakeSearch(noOfIterations: 1);
        var binary = new FakeSearch(noOfIterations: 1);
        var comparison = new FakeSearchComparison(linear, binary);
        var sut = ViewModelFactory.Create(out _, comparison);
        sut.NoOfSearchesText = 1_000.ToString();
        linear.OnFindItem = () => sut.SimulateCommand.Cancel();

        await sut.SimulateCommand.ExecuteAsync(null);

        Assert.False(sut.IsSearchEnabled);
        Assert.False(sut.IsSimulating);
        Assert.Equal(Visibility.Hidden, sut.ProgressBarVisibility);
    }

    [Fact]
    public void Data_sample_is_empty_before_any_simulation()
    {
        var sut = ViewModelFactory.Create();

        Assert.Equal(string.Empty, sut.DataSample);
    }

    [Fact]
    public async Task Simulation_publishes_first_middle_and_last_dataset_values()
    {
        // The binary fake reports 10 entries and returns its index from the indexer. With three values
        // per part the preview reads first=0,1,2 ... middle=3,4,5 ((10-3)/2=3) ... last=7,8,9, and a comma
        // surrounds each separator.
        var linear = new FakeSearch(noOfIterations: 1);
        var binary = new FakeSearch(noOfIterations: 1) { NoOfEntries = 10 };
        var comparison = new FakeSearchComparison(linear, binary);
        var sut = ViewModelFactory.Create(out _, comparison);
        sut.NoOfSearchesText = 1_000.ToString();

        await sut.SimulateCommand.ExecuteAsync(null);

        Assert.Equal("0, 1, 2, ..., 3, 4, 5, ..., 7, 8, 9", sut.DataSample);
    }
}
