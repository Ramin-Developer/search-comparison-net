namespace SearchComparisonNet.Kernel.Interfaces;

public interface ISearchItem
{
    int? TargetIndex { get; init; }

    int TargetValue { get; init; }

    int NoOfIterations { get; init; }
}
