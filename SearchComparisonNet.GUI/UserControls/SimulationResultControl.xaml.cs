namespace SearchComparisonNet.GUI.UserControls;

public partial class SimulationResultControl : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SimulationResultControl),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty AvgIterationsProperty =
        DependencyProperty.Register(nameof(AvgIterations), typeof(object), typeof(SimulationResultControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty AvgElapsedTimeProperty =
        DependencyProperty.Register(nameof(AvgElapsedTime), typeof(object), typeof(SimulationResultControl),
            new PropertyMetadata(null));

    public SimulationResultControl()
    {
        InitializeComponent();
        ((FrameworkElement)Content).DataContext = this;
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object? AvgIterations
    {
        get => GetValue(AvgIterationsProperty);
        set => SetValue(AvgIterationsProperty, value);
    }

    public object? AvgElapsedTime
    {
        get => GetValue(AvgElapsedTimeProperty);
        set => SetValue(AvgElapsedTimeProperty, value);
    }
}
