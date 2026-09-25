using System.Windows;
using OsbizTaxation.ViewModels;

namespace OsbizTaxation;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
        => await _viewModel.CheckUpdatesOnStartupAsync();

    private void OnClosed(object? sender, EventArgs e)
        => _viewModel.Dispose();
}
