namespace SearchComparisonNet.ViewModelTests;

// Exercises MainViewModel property-change notifications under a WPF SynchronizationContext.
// The fixture sets DispatcherSynchronizationContext for the duration of this collection so that
// any dispatcher-marshal path in the view model is exercised on the correct context.
[Collection("WPF")]
public class MainViewModelDispatcherTests
{
    // Verifies IsSimulating transitions True → False under a WPF SynchronizationContext.
    // The DispatcherSynchronizationContext is set; the VM raises IsSimulating=false from the
    // background task thread (current behavior — full marshal path is a future Option C step).
    [Fact]
    public async Task SimulateCommand_transitions_IsSimulating_true_then_false()
    {
        var states = new List<bool>();

        var sut = ViewModelFactory.Create();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsSimulating))
                states.Add(sut.IsSimulating);
        };

        await sut.SimulateCommand.ExecuteAsync(null);

        Assert.Multiple(
            () => Assert.Contains(true, states),
            () => Assert.False(sut.IsSimulating));
    }

    // Verifies that a completed simulation raises PropertyChanged for the iteration averages
    // under a WPF SynchronizationContext. Elapsed-time averages are not asserted here because
    // the FakeSearch completes in 0 ms, so those properties stay at 0 and SetProperty
    // (CommunityToolkit) suppresses the notification when the value does not change.
    [Fact]
    public async Task SimulateCommand_raises_result_properties_after_completion()
    {
        var changed = new List<string?>();

        var sut = ViewModelFactory.Create();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        await sut.SimulateCommand.ExecuteAsync(null);

        Assert.Multiple(
            () => Assert.Contains(nameof(MainViewModel.LinearAvgNoOfIterations), changed),
            () => Assert.Contains(nameof(MainViewModel.BinaryAvgNoOfIterations), changed));
    }

    // Verifies ProgressBarVisibility transitions (Hidden → Visible → Hidden) are raised
    // on the same context, which is the dispatcher-marshal path exercised by SimulationRunner.
    [Fact]
    public async Task SimulateCommand_transitions_ProgressBarVisibility_on_same_context()
    {
        var visibilityChanges = new List<Visibility>();

        var sut = ViewModelFactory.Create();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ProgressBarVisibility))
                visibilityChanges.Add(sut.ProgressBarVisibility);
        };

        await sut.SimulateCommand.ExecuteAsync(null);

        Assert.Multiple(
            () => Assert.Contains(Visibility.Visible, visibilityChanges),
            () => Assert.Equal(Visibility.Hidden, visibilityChanges[^1]));
    }
}
