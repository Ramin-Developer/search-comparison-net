namespace SearchComparisonNet.Kernel.Models;

public class DataGenerator : IDataGenerator
{
    public DataGenerator(DataParameters dataParams)
    {
        Random = new Random();
        NoOfEntries = dataParams.NoOfEntries;
        MinValue = dataParams.MinEntryValue;
        MaxValue = dataParams.MaxEntryValue;
        Data = GenerateData();
    }

    public int[] Data { get; }

    public int NoOfEntries { get; }

    public int MinValue { get; }

    public int MaxValue { get; }

    public int NextRandomNo() => Random.Next(MinValue, MaxValue);

    private Random Random { get; }

    private int[] GenerateData()
    {
        // Track membership in a compact bitset over the sampling domain [MinValue, MaxValue)
        // instead of a HashSet<int>. This avoids the HashSet Entry[] LOH allocations and lets
        // us collect the values in ascending order directly, so no separate sort is needed.
        var domainSize = MaxValue - MinValue;
        var seen = new BitArray(domainSize);

        var result = new int[NoOfEntries];
        var count = 0;
        while (count < NoOfEntries)
        {
            var offset = Random.Next(MinValue, MaxValue) - MinValue;
            if (seen[offset])
            { continue; }

            seen[offset] = true;
            count++;
        }

        var index = 0;
        for (var offset = 0; offset < domainSize && index < NoOfEntries; offset++)
        {
            if (seen[offset])
            { result[index++] = offset + MinValue; }
        }

        return result;
    }
}
