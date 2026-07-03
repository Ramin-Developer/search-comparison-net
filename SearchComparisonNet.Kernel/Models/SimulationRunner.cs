namespace SearchComparisonNet.Kernel.Models;

// The cancellation-aware simulation loop, extracted from MainViewModel so it can be unit-tested
// without a UI. The method is pure and synchronous: the caller owns any Task.Run offloading and
// the CancellationToken, so the G-5 cancellation contract (token honored -> OperationCanceledException)
// can be verified headlessly.
public static class SimulationRunner
{
    // Runs noOfSearches probes against the given search, summing NoOfIterations and measuring
    // elapsed time. Cancellation is checked at the top of every iteration via
    // ThrowIfCancellationRequested(), so a cancelled token surfaces as OperationCanceledException.
    // Progress (optional) is throttled by ProgressReportPolicy and always reaches 100% on the final
    // iteration. elapsedTime is rounded to roundDigits for display parity with the view model.
    public static SimulationRunResult Run(
        ISearch search,
        Func<int> nextRandomNo,
        int noOfSearches,
        int roundDigits,
        IProgress<double>? progress = null,
        int progressIntervalMs = ProgressReportPolicy.DefaultIntervalMs,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(search);
        ArgumentNullException.ThrowIfNull(nextRandomNo);

        var totalNoOfIterations = 0.0;
        var stopwatch = Stopwatch.StartNew();
        var lastReportMs = -1L;
        for (var j = 0; j < noOfSearches; j++)
        {
            token.ThrowIfCancellationRequested();
            var value = nextRandomNo();
            var searchItem = search.FindItem(value);
            totalNoOfIterations += searchItem.NoOfIterations;
            // Throttle progress by elapsed time: Report(...) may marshal to the UI thread, so
            // updating every iteration floods the dispatcher. Reporting at most once per
            // progressIntervalMs keeps UI updates bounded and adapts to the run length;
            // the final iteration always reports so the bar reaches 100%.
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            if (ProgressReportPolicy.ShouldReport(j, noOfSearches, elapsedMs, lastReportMs, progressIntervalMs))
            {
                lastReportMs = elapsedMs;
                progress?.Report((j + 1) * 100.0 / noOfSearches);
            }
        }
        stopwatch.Stop();
        var timeInSec = (double)stopwatch.ElapsedMilliseconds / 1000;
        var elapsedTimeInSec = Math.Round(timeInSec, roundDigits);

        return new SimulationRunResult(totalNoOfIterations, elapsedTimeInSec);
    }
}

// Raw totals produced by a simulation run. The view model divides these by the search count to
// derive the averages it displays; keeping the runner UI-agnostic avoids depending on the
// SimulationResults DTO's NoOfEntries/NoOfSearches presentation fields.
public readonly record struct SimulationRunResult(double TotalNoOfIterations, double ElapsedTimeInSec);
