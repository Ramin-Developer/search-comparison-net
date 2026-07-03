namespace SearchComparisonNet.Tests;

// Headless tests for the cancellation-aware simulation loop extracted from MainViewModel (Option B).
// These lock in the G-5 cancellation contract - a cancelled token surfaces as
// OperationCanceledException at the loop boundary - and the normal-completion accounting, all
// without a UI or WPF SynchronizationContext.
public sealed class SimulationRunnerTests
{
    [Fact]
    public void Run_completes_all_iterations_and_sums_NoOfIterations()
    {
        var search = new FakeSearch(noOfIterations: 3);

        var result = SimulationRunner.Run(search, () => 0, noOfSearches: 10, roundDigits: 1,
            token: TestContext.Current.CancellationToken);

        Assert.Equal(10, search.FindItemCallCount);        // ran every requested search
        Assert.Equal(30.0, result.TotalNoOfIterations);    // 10 searches * 3 iterations each
        Assert.True(result.ElapsedTimeInSec >= 0.0);
    }

    [Fact]
    public void Run_reports_progress_and_reaches_100_percent()
    {
        var search = new FakeSearch();
        var reported = new List<double>();
        var progress = new SynchronousProgress(reported.Add);

        SimulationRunner.Run(search, () => 0, noOfSearches: 5, roundDigits: 1, progress,
            token: TestContext.Current.CancellationToken);

        Assert.NotEmpty(reported);
        Assert.Equal(100.0, reported[^1]);                 // final iteration always reports 100%
    }

    [Fact]
    public void Run_with_already_cancelled_token_throws_before_searching()
    {
        var search = new FakeSearch();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            SimulationRunner.Run(search, () => 0, noOfSearches: 10, roundDigits: 1, token: cts.Token));

        Assert.Equal(0, search.FindItemCallCount);         // cancellation is checked before FindItem
    }

    [Fact]
    public void Run_honors_cancellation_requested_mid_run()
    {
        using var cts = new CancellationTokenSource();
        // Cancel from inside the loop after the first FindItem call; the next iteration's
        // ThrowIfCancellationRequested() must end the run.
        var search = new FakeSearch { OnFindItem = cts.Cancel };

        Assert.Throws<OperationCanceledException>(() =>
            SimulationRunner.Run(search, () => 0, noOfSearches: 1000, roundDigits: 1, token: cts.Token));

        Assert.Equal(1, search.FindItemCallCount);         // stopped right after the first search
    }

    [Fact]
    public void Run_null_search_throws_ArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            SimulationRunner.Run(null!, () => 0, noOfSearches: 1, roundDigits: 1,
                token: TestContext.Current.CancellationToken));

    [Fact]
    public void Run_null_nextRandomNo_throws_ArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            SimulationRunner.Run(new FakeSearch(), null!, noOfSearches: 1, roundDigits: 1,
                token: TestContext.Current.CancellationToken));

    // Minimal IProgress<double> that invokes the callback inline (no SynchronizationContext), so
    // reported values are captured deterministically on the calling thread.
    private sealed class SynchronousProgress(Action<double> onReport) : IProgress<double>
    {
        public void Report(double value) => onReport(value);
    }
}
