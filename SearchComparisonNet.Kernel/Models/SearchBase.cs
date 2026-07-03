namespace SearchComparisonNet.Kernel.Models;

public abstract class SearchBase(IDataGenerator dataGen) : ISearch
{
    // Derived from the actual dataset so it can never desync from Data. The generator produces
    // Data with exactly NoOfEntries elements, so this preserves the existing value while removing
    // the previously public setter (K-2).
    public int NoOfEntries => Data.Length;

    // Read-only: callers (the tests) only translate a known index to the value stored there so the
    // search can be asserted. The previously public setter was dead surface and was removed (C-1).
    public int this[int index] => Data[index];

    public abstract ISearchItem FindItem(int value);

    protected int[] Data { get; } = dataGen.Data;
}
