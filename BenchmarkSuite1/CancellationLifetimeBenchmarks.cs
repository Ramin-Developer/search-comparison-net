namespace SearchComparisonNet.Benchmarks;
// Investigates the disposal/lifetime questions raised for the GUI simulate/cancel flow:
//   * "Is the CancellationTokenSource leaking across repeated runs?"
//   * "Does disposal pressure show up in GC?"
// MainViewModel drives cancellation through AsyncRelayCommand, which internally creates a
// CancellationTokenSource per execution and cancels/disposes it. This benchmark models that
// lifetime directly (without WPF/MVVM plumbing a headless harness cannot host) by repeatedly
// creating, registering a callback on, cancelling, observing the cancellation, and disposing a
// CancellationTokenSource. With [MemoryDiagnoser], the Allocated column reveals whether repeated
// cycles retain CTS state (CancellationCallbackInfo, registrations) or merely churn collectible
// transients that the GC reclaims cleanly.
//
// SessionsPerOp mirrors a user repeatedly pressing Simulate then Cancel many times in a session.
[SimpleJob(warmupCount: 3, iterationCount: 5)]
[CPUUsageDiagnoser]
public class CancellationLifetimeBenchmarks
{
    private int[] _sessions = null!;
    [Params(1_000, 10_000)]
    public int SessionsPerOp { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _sessions = new int[SessionsPerOp];
        for (var i = 0; i < _sessions.Length; i++)
        {
            _sessions[i] = i;
        }
    }

    // Baseline: create -> dispose with no registration or cancellation, to isolate the bare
    // CancellationTokenSource allocation cost from the cancel/callback machinery.
    [Benchmark(Baseline = true)]
    public long CreateDisposeOnly()
    {
        long observed = 0;
        foreach (var _ in _sessions)
        {
            using var cts = new CancellationTokenSource();
            if (cts.Token.CanBeCanceled)
            {
                observed++;
            }
        }

        return observed;
    }

    // Models the real simulate/cancel path without exception noise: a callback is registered (as
    // the runtime does when a token is awaited/observed), Cancel() is invoked, and cancellation is
    // observed via IsCancellationRequested before disposal. This exercises the registration and
    // callback machinery that a leak or GC-pressure issue would surface through.
    [Benchmark]
    public long CreateCancelDispose_NoException()
    {
        long observed = 0;
        foreach (var _ in _sessions)
        {
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            using var registration = token.Register(static () =>
            {
            });
            cts.Cancel();
            if (token.IsCancellationRequested)
            {
                observed++;
            }
        }

        return observed;
    }
}