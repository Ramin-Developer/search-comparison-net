namespace SearchComparisonNet.Kernel.Models;

// Central home for tuning constants that shape how a search simulation runs and how its results
// are presented. Keeping them together (rather than scattered across the view-model) makes the
// simulation's behaviour easy to discover and adjust in one place.
public static class SimulationConstants
{
    // Number of dataset values surfaced in each part of the results preview. The preview shows this
    // many values from the start, the middle, and the end of the sorted collection, so the default is 3.
    public const int DataSampleValueCount = 3;

    // Default minimum gap between progress reports, in milliseconds. Reporting on every iteration
    // floods the UI-thread dispatcher; this bounds reports to roughly five per second.
    public const int ProgressReportIntervalMs = 200;
}
