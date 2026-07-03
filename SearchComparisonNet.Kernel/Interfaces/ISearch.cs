namespace SearchComparisonNet.Kernel.Interfaces;

public interface ISearch
{
    int NoOfEntries { get; }

    int this[int index] { get; }

    ISearchItem FindItem(int value);
}
