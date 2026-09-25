using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using OsbizTaxation.Helpers;
using OsbizTaxation.Services;
using OsbizTaxation.ViewModels;
using OsbizTaxation.Views;
using WinForms = System.Windows.Forms;

namespace OsbizTaxation;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private readonly WinForms.NotifyIcon _tray = new();
    private System.Drawing.Icon? _trayIcon;
    private bool _startupUpdateChecked;
    private bool _restoring;

    public MainWindow()
    {
        InitializeComponent();
        Icon = GetExecutableIcon() ?? WindowIcons.Create(WindowIcons.Headset);
        DataContext = _viewModel;
        Title = $"Taxation OSBIZ  ({_viewModel.Version})";
        UpdateThemeUi();
        GridColumnWidths.Apply(GridCdr, WidthsFor("GridCdr"));
        SourceInitialized += (_, _) => TitleBarTheme.Apply(this);
        Loaded += OnLoaded;
        StateChanged += OnStateChanged;
        Closed += OnClosed;
        InitTrayIcon();
    }

    // ---------------------------------------------------------------------
    // Icone (celle de l'executable) et icone de la zone de notification
    // ---------------------------------------------------------------------

    private static System.Windows.Media.ImageSource? GetExecutableIcon()
    {
        try
        {
            var path = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(path))
            {
                using var ico = System.Drawing.Icon.ExtractAssociatedIcon(path);
                if (ico != null)
                {
                    return Imaging.CreateBitmapSourceFromHIcon(
                        ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private void InitTrayIcon()
    {
        try
        {
            var path = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(path))
                _trayIcon = System.Drawing.Icon.ExtractAssociatedIcon(path);

            _tray.Icon = _trayIcon ?? System.Drawing.SystemIcons.Application;
            _tray.Text = $"Taxation OSBIZ ({_viewModel.Version})";
            _tray.Visible = false;

            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("Ouvrir", null, (_, _) => RestoreFromTray());
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add("Quitter", null, (_, _) => QuitApplication());
            _tray.ContextMenuStrip = menu;
            _tray.DoubleClick += (_, _) => RestoreFromTray();
        }
        catch
        {
        }
    }

    /// <summary>Reduit la fenetre dans la zone de notification au lieu de la barre des taches.</summary>
    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (_restoring)
            return;

        if (WindowState == WindowState.Minimized && IsVisible)
        {
            ShowInTaskbar = false;
            Hide();
            _tray.Visible = true;
        }
    }

    /// <summary>Restaure et affiche la fenetre depuis l'icone de la zone de notification.</summary>
    public void RestoreFromTray()
    {
        _restoring = true;
        try
        {
            _tray.Visible = false;
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
        ShowInTaskbar = false;
        _tray.Visible = true;
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
        _tray.Visible = false;
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

        _tray.Visible = false;
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
