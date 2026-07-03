namespace SearchComparisonNet.GUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ISearchComparisonFactory _searchComparisonFactory;

    // Minimum wall-clock interval between progress reports. Report(...) marshals to the UI thread,
    // so this bounds progress-bar updates to ~5 per second regardless of NoOfSearches.
    private const int ProgressReportIntervalMs = ProgressReportPolicy.DefaultIntervalMs;

    public MainViewModel(ISearchComparisonFactory searchComparisonFactory)
    {
        _searchComparisonFactory = searchComparisonFactory;

        InputValidation = new InputValidation() { ClassLevelCascadeMode = CascadeMode.Stop };
        NoOfEntriesText = ProblemConstants.InitialNoOfEntries.ToString(CultureInfo.InvariantCulture);
        NoOfSearchesText = ProblemConstants.InitialNoOfSearches.ToString(CultureInfo.InvariantCulture);

        IsSimulating = false;
        IsSearchEnabled = false;
        ProgressBarVisibility = Visibility.Hidden;
    }

    /************************************ Public Attributes ************************************/
    public InputValidation InputValidation { get; }

    public string ProgressBarLabel { get; private set; } = string.Empty;

    [ObservableProperty]
    private bool _isSearchEnabled;

    [ObservableProperty]
    private Visibility _progressBarVisibility;

    [ObservableProperty]
    private int _noOfEntries;

    [ObservableProperty]
    private int _noOfSearches;

    [ObservableProperty]
    private int? _targetIndex;

    [ObservableProperty]
    private double _binaryAvgNoOfIterations;

    [ObservableProperty]
    private double _binaryAvgElapsedTime;

    [ObservableProperty]
    private double _linearAvgNoOfIterations;

    [ObservableProperty]
    private double _linearAvgElapsedTime;

    [ObservableProperty]
    private bool _isSimulating;

    [ObservableProperty]
    private double _progressBarValue;

    [ObservableProperty]
    private string _noOfEntriesText = string.Empty;

    [ObservableProperty]
    private string _noOfSearchesText = string.Empty;

    [ObservableProperty]
    private int? _targetValue;

    // True when the most recent search resolved but the value was absent from the dataset.
    // Drives the "-1" convention and the "Not Found" tooltip on the Target Index box.
    [ObservableProperty]
    private bool _targetIndexNotFound;

    // Tooltip for the Target Index box: explains the "-1" shown for a not-found value. Returns null
    // when there is nothing to show so WPF suppresses the tooltip popup entirely.
    public string? TargetIndexTooltip => TargetIndexNotFound ? "Not Found" : null;

    partial void OnTargetIndexNotFoundChanged(bool value) => OnPropertyChanged(nameof(TargetIndexTooltip));

    // A compact preview of the generated (sorted) dataset: first, middle, and last values,
    // separated by dots. Populated once a simulation has produced the data.
    [ObservableProperty]
    private string _dataSample = string.Empty;

    partial void OnIsSimulatingChanged(bool value) => UpdateButtonFunctionality();

    partial void OnProgressBarValueChanged(double value)
    {
        ProgressBarLabel = Math.Round(value, 0) + "%";
        OnPropertyChanged(nameof(ProgressBarLabel));
    }

    partial void OnNoOfEntriesTextChanged(string value) =>
        IsNoOfEntriesValid = ValidateAndParse(value, nameof(NoOfEntriesText), parsed => NoOfEntries = parsed);

    partial void OnNoOfSearchesTextChanged(string value) =>
        IsNoOfSearchesValid = ValidateAndParse(value, nameof(NoOfSearchesText), parsed => NoOfSearches = parsed);

    // Validates a single text property, refreshes command/button state, and on success parses the
    // value into the matching integer property. Returns whether the text property is valid.
    private bool ValidateAndParse(string value, string propertyName, Action<int> setParsedValue)
    {
        var isValid = InputValidation.Validate(this, context => context.IncludeProperties(propertyName)).IsValid;
        OnPropertyChanged(nameof(IsInputValid));
        UpdateButtonFunctionality();

        if (!isValid)
        {
            IsSimulating = false;
            return false;
        }

        _ = int.TryParse(value, out var parsed);
        setParsedValue(parsed);
        return true;
    }

    // The lookup is no longer implicit: it runs on demand via SearchCommand (button or Enter).
    // Changing the value only refreshes the command's enabled state and clears any stale
    // "not found" feedback from a previous search.
    partial void OnTargetValueChanged(int? value)
    {
        TargetIndexNotFound = false;
        SearchCommand.NotifyCanExecuteChanged();
    }

    private void UpdateButtonFunctionality()
    {
        SimulateCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }

    /***************************************** Private Methods *****************************************/
    private bool CanSimulate() => !IsSimulating && IsInputValid;

    private bool CanCancel() => IsSimulating;

    // The generated SimulateCommand is an IAsyncRelayCommand; Cancel forwards to its Cancel()
    // because this toolkit version exposes cancellation as a method, not a separate command.
    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => SimulateCommand.Cancel();

    // Enabled whenever the Target Value is a valid integer. WPF sets the bound int? to null when
    // the entered text is not a valid integer, so a non-null value implies valid input.
    private bool CanSearch() => TargetValue is not null;

    // Runs a single binary-search lookup for the current Target Value. BinarySearch is only
    // assigned once a simulation has run, so before then this is a safe no-op. A value that is
    // absent from the dataset yields TargetIndex = -1 and a "Not found" tooltip.
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private void Search()
    {
        if (BinarySearch is null || TargetValue is null)
        { return; }

        var searchItem = BinarySearch.FindItem(TargetValue.Value);
        if (searchItem.TargetIndex is null)
        {
            TargetIndexNotFound = true;
            TargetIndex = -1;
        }
        else
        {
            TargetIndexNotFound = false;
            TargetIndex = searchItem.TargetIndex;
        }
    }

    // Formats a preview of the sorted dataset as three groups joined by ", ..., ": the first N values,
    // N values around the middle, and the last N values, where N is SimulationConstants.DataSampleValueCount
    // (default 3). Values within each group are comma-separated, and a comma surrounds each " ... "
    // separator. Uses the ISearch indexer, so it reads the shared data without exposing the underlying array.
    private static string BuildDataSample(ISearch search)
    {
        var count = search.NoOfEntries;
        var perGroup = SimulationConstants.DataSampleValueCount;
        if (count == 0 || perGroup <= 0)
        { return string.Empty; }

        // When the collection is too small to show three distinct groups, just list every value.
        if (count <= perGroup * 3)
        { return FormatRange(search, 0, count); }

        var firstGroup = FormatRange(search, 0, perGroup);
        var middleGroup = FormatRange(search, (count - perGroup) / 2, perGroup);
        var lastGroup = FormatRange(search, count - perGroup, perGroup);

        return string.Join(", ..., ", firstGroup, middleGroup, lastGroup);
    }

    // Joins `length` values starting at `start` as comma-separated numbers using the ISearch indexer.
    private static string FormatRange(ISearch search, int start, int length)
    {
        var values = new string[length];
        for (var i = 0; i < length; i++)
        { values[i] = search[start + i].ToString(CultureInfo.InvariantCulture); }

        return string.Join(", ", values);
    }

    [RelayCommand(CanExecute = nameof(CanSimulate))]
    private async Task SimulateAsync(CancellationToken token)
    {
        IsSearchEnabled = false;
        TargetValue = null;
        TargetIndex = null;
        TargetIndexNotFound = false;
        DataSample = string.Empty;

        var searchComparison = _searchComparisonFactory.Create(NoOfEntries);
        LinearSearch = searchComparison.LinearSearch;
        BinarySearch = searchComparison.BinarySearch;
        DataSample = BuildDataSample(BinarySearch);
        var nextRandomNo = searchComparison.NextRandomNo;

        IsSimulating = true;

        // Progress is reported through IProgress<T>. Because the Progress<T> instance is
        // created here on the UI thread, its callbacks marshal back to the UI thread, so the
        // background simulation never touches UI-bound properties directly.
        var progress = new Progress<double>(value => ProgressBarValue = value);
        ProgressBarVisibility = Visibility.Visible;

        try
        {
            // Linear and binary runs differ only in how aggressively their elapsed time is
            // rounded for display (1 vs 5 fractional digits); the loop itself is identical.
            var linearResults = await RunSimulationAsync(LinearSearch!, nextRandomNo, roundDigits: 1, progress, token);
            var binaryResults = await RunSimulationAsync(BinarySearch!, nextRandomNo, roundDigits: 5, progress, token);

            LinearAvgNoOfIterations = linearResults.AvgNoOfIterations;
            LinearAvgElapsedTime = linearResults.AvgElapsedTime;

            BinaryAvgNoOfIterations = binaryResults.AvgNoOfIterations;
            BinaryAvgElapsedTime = binaryResults.AvgElapsedTime;

            IsSearchEnabled = true;
        }
        catch (OperationCanceledException)
        {
            // Cancellation is a normal outcome, not an error: leave results as-is and keep
            // the search panel disabled since the run did not complete.
            IsSearchEnabled = false;
        }
        finally
        {
            // Always run on the UI thread (continuation after await), so resetting the
            // progress bar here is thread-safe and deterministic.
            ProgressBarValue = 0;
            ProgressBarVisibility = Visibility.Hidden;
            IsSimulating = false;
        }
    }

    private Task<SimulationResults> RunSimulationAsync(ISearch search, Func<int> nextRandomNo, int roundDigits, IProgress<double> progress, CancellationToken token) =>
        Task.Run(() =>
        {
            var result = SimulationRunner.Run(search, nextRandomNo, NoOfSearches, roundDigits, progress, ProgressReportIntervalMs, token);
            return SimulationResults(result.TotalNoOfIterations, result.ElapsedTimeInSec);
        }, token);

    private SimulationResults SimulationResults(double totalNoOfIterations, double totalElapsedTime) =>
        new SimulationResults()
        {
            NoOfEntries = NoOfEntries,
            NoOfSearches = NoOfSearches,
            AvgNoOfIterations = totalNoOfIterations / NoOfSearches,
            AvgElapsedTime = totalElapsedTime / NoOfSearches
        };

    private ISearch? LinearSearch { get; set; }

    private ISearch? BinarySearch { get; set; }

    [ObservableProperty]
    private bool _isNoOfEntriesValid;

    [ObservableProperty]
    private bool _isNoOfSearchesValid;

    private bool IsInputValid => IsNoOfEntriesValid && IsNoOfSearchesValid;
}
