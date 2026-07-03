namespace SearchComparisonNet.Kernel.Models;

public static class ProblemConstants
{
    // No of Entries
    // NOTE: value preserved exactly (10,000). Flagged in review (K-3) as a possible typo for 100_000 — left unchanged per decision.
    public const int MinNoOfEntries = 10_000;

    public const int InitialNoOfEntries = 500_000;

    public const int MaxNoOfEntries = 50_000_000;

    // Entry Values
    public const int MinEntryValue = 0;

    // No of Searches
    public const int MinNoOfSearches = 1_000;

    public const int InitialNoOfSearches = 5_000;

    public const int MaxNoOfSearches = 500_000;

    public const string NullOrEmptyNoOfEntriesMsg = "NoOfEntriesText is a required field.";

    public const string NullOrEmptyNoOfSearchesMsg = "NoOfSearchesText is a required field.";

    public const string InvalidNoOfEntriesMsg = "NoOfEntriesText must be a valid integer.";

    public const string InvalidNoOfSearchesMsg = "NoOfSearchesText must be a valid integer.";

    public static readonly string OutOfRangeNoOfEntriesMsg =
        $"NoOfEntriesText must be an integer in the interval [{MinNoOfEntries}, {MaxNoOfEntries}].";

    public static readonly string OutOfRangeNoOfSearchesMsg =
        $"NoOfSearchesText must be an integer in the interval [{MinNoOfSearches}, {MaxNoOfSearches}].";
}
