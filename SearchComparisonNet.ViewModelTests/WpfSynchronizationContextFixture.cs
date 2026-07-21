namespace SearchComparisonNet.ViewModelTests;

// Sets a DispatcherSynchronizationContext for the duration of a test class so that
// MainViewModel's property-change notifications can be asserted on the calling context.
// Used via [Collection("WPF")] on any test class that needs dispatcher marshaling.
public sealed class WpfSynchronizationContextFixture : IDisposable
{
    private readonly SynchronizationContext? _previous;

    public WpfSynchronizationContextFixture()
    {
        _previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(
            new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
    }

    public void Dispose() =>
        SynchronizationContext.SetSynchronizationContext(_previous);
}

[CollectionDefinition("WPF")]
public class WpfFixtureGroup : ICollectionFixture<WpfSynchronizationContextFixture> { }
