using System.Windows;
using OsbizTaxation.Helpers;
using OsbizTaxation.Services;
using OsbizTaxation.ViewModels;
using OsbizTaxation.Views;

namespace OsbizTaxation;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        Icon = WindowIcons.Create(WindowIcons.Headset);
        DataContext = _viewModel;
        Title = $"Taxation OSBIZ  ({_viewModel.Version})";
        UpdateThemeUi();
        SourceInitialized += (_, _) => TitleBarTheme.Apply(this);
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void UpdateThemeUi()
    {
        var dark = ThemeManager.IsDark;
        TxtThemeIcon.Text = dark ? "\uE706" : "\uE708";
        BtnTheme.ToolTip = dark ? "Passer en mode clair" : "Passer en mode sombre";
    }

    private void ApplyTheme(string theme)
    {
        ThemeManager.Apply(theme);
        TitleBarTheme.ApplyToAllWindows();
        _viewModel.Config.Theme = theme;
        ConfigService.Save(_viewModel.Config);
        UpdateThemeUi();
        _viewModel.ReloadRecords();
    }

    private void OnThemeClick(object sender, RoutedEventArgs e)
        => ApplyTheme(ThemeManager.IsDark ? ThemeManager.Light : ThemeManager.Dark);

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
        var window = new StatistiquesWindow(_viewModel) { Owner = this };
        window.ShowDialog();
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
