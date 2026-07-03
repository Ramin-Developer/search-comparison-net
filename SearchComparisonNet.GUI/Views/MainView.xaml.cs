namespace SearchComparisonNet.GUI.Views;

public partial class MainView : Window
{
    public MainView(MainViewModel mainViewModel)
    {
        InitializeComponent();
        MainViewModel = mainViewModel;
        DataContext = mainViewModel;
    }

    public MainViewModel MainViewModel { get; }
}
