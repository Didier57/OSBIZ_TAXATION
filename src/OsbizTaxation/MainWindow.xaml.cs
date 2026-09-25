using System;
using System.Windows;
using OsbizTaxation.Helpers;
using OsbizTaxation.Services;
using OsbizTaxation.ViewModels;
using OsbizTaxation.Views;

namespace OsbizTaxation;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private readonly TrayIcon _tray = new();
    private System.Drawing.Icon? _trayIcon;
    private bool _startupUpdateChecked;
    private bool _restoring;
    private bool _balloonShown;

    public MainWindow()
    {
        InitializeComponent();
        Icon = AppIcon.WindowIcon() ?? WindowIcons.Create(WindowIcons.Headset);
        _trayIcon = AppIcon.TrayHandle();
        DataContext = _viewModel;
        Title = $"Taxation OSBIZ  ({_viewModel.Version})";
        UpdateThemeUi();
        GridColumnWidths.Apply(GridCdr, WidthsFor("GridCdr"));
        SourceInitialized += (_, _) => TitleBarTheme.Apply(this);
        Loaded += OnLoaded;
        StateChanged += OnStateChanged;
        Closed += OnClosed;
    }

    // ---------------------------------------------------------------------
    // Icone de la zone de notification
    // ---------------------------------------------------------------------

    private string TrayTooltip => $"Taxation OSBIZ ({_viewModel.Version})";

    private System.Drawing.Icon TrayHandle => _trayIcon ?? System.Drawing.SystemIcons.Application;

    private bool EnsureTrayIcon()
        => _tray.IsVisible || _tray.Show(this, TrayTooltip, TrayHandle, RestoreFromTray, RestoreFromTray, QuitApplication);

    /// <summary>Reduit la fenetre dans la zone de notification au lieu de la barre des taches.</summary>
    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (_restoring)
            return;

        if (WindowState != WindowState.Minimized || !IsVisible)
            return;

        if (!EnsureTrayIcon())
        {
            AppLog.Write("Reduction annulee : impossible de creer l'icone de notification.");
            return;
        }

        ShowInTaskbar = false;
        Hide();

        if (!_balloonShown)
        {
            _balloonShown = true;
            _tray.ShowBalloon("Taxation OSBIZ", "L'application est reduite ici. Double-cliquez pour la rouvrir.");
        }
    }

    /// <summary>Restaure et affiche la fenetre depuis l'icone de la zone de notification.</summary>
    public void RestoreFromTray()
    {
        _restoring = true;
        try
        {
            _tray.Hide();
            ShowInTaskbar = true;
            Show();
            WindowState = WindowState.Maximized;
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }
        finally
        {
            _restoring = false;
        }
    }

    /// <summary>Demarre l'application sans afficher la fenetre (uniquement l'icone de notification).</summary>
    public void StartHiddenToTray()
    {
        if (EnsureTrayIcon())
        {
            ShowInTaskbar = false;
        }
        else
        {
            AppLog.Write("Demarrage reduit impossible : la fenetre est affichee minimisee.");
            Show();
            WindowState = WindowState.Minimized;
        }

        RunStartupUpdateCheck();
    }

    private void RunStartupUpdateCheck()
    {
        if (_startupUpdateChecked)
            return;

        _startupUpdateChecked = true;
        _ = _viewModel.CheckUpdatesOnStartupAsync();
    }

    private void QuitApplication()
    {
        _tray.Hide();
        Close();
    }

    // ---------------------------------------------------------------------
    // Theme / cycle de vie
    // ---------------------------------------------------------------------

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

    private void OnLoaded(object sender, RoutedEventArgs e) => RunStartupUpdateCheck();

    private void OnClosed(object? sender, EventArgs e)
    {
        GridColumnWidths.Capture(GridCdr, WidthsFor("GridCdr"));
        ConfigService.Save(_viewModel.Config);
        _viewModel.Dispose();

        _tray.Dispose();
        _trayIcon?.Dispose();

        Application.Current.Shutdown();
    }

    private Dictionary<string, double> WidthsFor(string grid)
    {
        if (!_viewModel.Config.ColumnWidths.TryGetValue(grid, out var widths))
        {
            widths = new Dictionary<string, double>();
            _viewModel.Config.ColumnWidths[grid] = widths;
        }

        return widths;
    }

    private void OnQuitClick(object sender, RoutedEventArgs e) => QuitApplication();

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
