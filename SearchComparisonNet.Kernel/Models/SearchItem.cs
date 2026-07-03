namespace SearchComparisonNet.Kernel.Models;

public class SearchItem : ISearchItem
{
    public int? TargetIndex { get; init; }

    public int TargetValue { get; init; }

    public int NoOfIterations { get; init; }
}
