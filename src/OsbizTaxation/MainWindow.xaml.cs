using System.Windows;
using OsbizTaxation.ViewModels;
using OsbizTaxation.Views;

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

    private void OnQuitClick(object sender, RoutedEventArgs e) => Close();

    private void OnConfigurationClick(object sender, RoutedEventArgs e)
    {
        var window = new ConfigurationWindow(_viewModel) { Owner = this };
        window.ShowDialog();
        _viewModel.ReloadRecords();
    }

    private void OnTransfertClick(object sender, RoutedEventArgs e)
    {
        var window = new TransfertWindow(_viewModel) { Owner = this };
        window.ShowDialog();
        _viewModel.ReloadRecords();
    }

    private void OnRechercheClick(object sender, RoutedEventArgs e)
    {
        var window = new RechercheWindow(_viewModel) { Owner = this };
        window.ShowDialog();
    }

    private void OnStatistiquesClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Le module de statistiques sera disponible dans une prochaine version.",
            "Statistiques", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnImportExportClick(object sender, RoutedEventArgs e)
    {
        var window = new ImportExportWindow(_viewModel) { Owner = this };
        window.ShowDialog();
        _viewModel.ReloadRecords();
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            $"Taxation OSBIZ\nVersion {_viewModel.Version}\n\nDossier de travail :\n{OsbizTaxation.Services.AppPaths.AppDirectory}",
            "A propos", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
