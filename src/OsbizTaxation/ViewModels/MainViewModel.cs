using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using OsbizTaxation.Models;
using OsbizTaxation.Services;

namespace OsbizTaxation.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly ConfigService _configService = new();
    private readonly DataFetcher _fetcher = new();
    private readonly UpdateService _updateService = new();
    private readonly DispatcherTimer _autoFetchTimer = new();

    private string _sourceUrl = string.Empty;
    private string _outputFile = string.Empty;
    private bool _autoFetchEnabled;
    private string _autoFetchIntervalMinutes = "60";
    private bool _checkUpdatesOnStartup = true;
    private bool _isBusy;
    private string _status = "Pret.";
    private string _lastFetch = "Jamais";

    public MainViewModel()
    {
        var config = _configService.Load();
        _sourceUrl = config.SourceUrl;
        _outputFile = config.OutputFile;
        _autoFetchEnabled = config.AutoFetchEnabled;
        _autoFetchIntervalMinutes = config.AutoFetchIntervalMinutes.ToString();
        _checkUpdatesOnStartup = config.CheckUpdatesOnStartup;

        FetchNowCommand = new AsyncRelayCommand(_ => FetchAsync(manual: true), _ => !IsBusy);
        SaveConfigCommand = new RelayCommand(_ => SaveConfig());
        BrowseOutputFileCommand = new RelayCommand(_ => BrowseOutputFile());
        CheckUpdatesCommand = new AsyncRelayCommand(_ => CheckUpdatesAsync(interactive: true), _ => !IsBusy);
        OpenOutputFolderCommand = new RelayCommand(_ => OpenOutputFolder());
        ClearLogCommand = new RelayCommand(_ => Logs.Clear());

        _autoFetchTimer.Tick += async (_, _) => await FetchAsync(manual: false);
        ConfigureAutoFetchTimer();

        AppendLog($"Application demarree (version {_updateService.CurrentVersion}).");
        AppendLog($"Fichier de configuration : {_configService.ConfigPath}");
    }

    public ObservableCollection<string> Logs { get; } = new();

    public string SourceUrl
    {
        get => _sourceUrl;
        set => SetProperty(ref _sourceUrl, value);
    }

    public string OutputFile
    {
        get => _outputFile;
        set => SetProperty(ref _outputFile, value);
    }

    public bool AutoFetchEnabled
    {
        get => _autoFetchEnabled;
        set
        {
            if (SetProperty(ref _autoFetchEnabled, value))
            {
                ConfigureAutoFetchTimer();
                AppendLog(value
                    ? $"Recuperation automatique activee (toutes les {AutoFetchIntervalMinutes} min)."
                    : "Recuperation automatique desactivee.");
            }
        }
    }

    public string AutoFetchIntervalMinutes
    {
        get => _autoFetchIntervalMinutes;
        set
        {
            if (SetProperty(ref _autoFetchIntervalMinutes, value))
                ConfigureAutoFetchTimer();
        }
    }

    public bool CheckUpdatesOnStartup
    {
        get => _checkUpdatesOnStartup;
        set => SetProperty(ref _checkUpdatesOnStartup, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string LastFetch
    {
        get => _lastFetch;
        private set => SetProperty(ref _lastFetch, value);
    }

    public string Version => _updateService.CurrentVersion.ToString();

    public AsyncRelayCommand FetchNowCommand { get; }

    public RelayCommand SaveConfigCommand { get; }

    public RelayCommand BrowseOutputFileCommand { get; }

    public AsyncRelayCommand CheckUpdatesCommand { get; }

    public RelayCommand OpenOutputFolderCommand { get; }

    public RelayCommand ClearLogCommand { get; }

    public async Task CheckUpdatesOnStartupAsync()
    {
        if (!CheckUpdatesOnStartup)
            return;

        await CheckUpdatesAsync(interactive: false);
    }

    private async Task FetchAsync(bool manual)
    {
        IsBusy = true;
        Status = "Recuperation en cours...";

        try
        {
            var data = await _fetcher.FetchAsync(SourceUrl);
            TextFileWriter.Write(OutputFile, data);

            LastFetch = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            Status = "Recuperation terminee.";
            AppendLog($"{(manual ? "Manuelle" : "Automatique")} : {data.Length} caracteres recuperes et ecrits dans '{OutputFile}'.");
        }
        catch (Exception ex)
        {
            Status = "Echec de la recuperation.";
            AppendLog($"Erreur : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CheckUpdatesAsync(bool interactive)
    {
        IsBusy = true;
        Status = "Verification des mises a jour...";

        try
        {
            var update = await _updateService.CheckForUpdateAsync();
            if (update is null)
            {
                Status = "Aucune mise a jour disponible.";
                AppendLog($"Aucune mise a jour (version courante {Version}).");
                if (interactive)
                    MessageBox.Show(
                        $"L'application est a jour (version {Version}).",
                        "Mises a jour",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                return;
            }

            AppendLog($"Nouvelle version disponible : {update.Version}.");
            var answer = MessageBox.Show(
                $"Une nouvelle version ({update.Version}) est disponible.\n\nTelecharger et installer maintenant ?",
                "Mise a jour",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
            {
                Status = "Mise a jour ignoree.";
                return;
            }

            Status = "Telechargement de la mise a jour...";
            var downloaded = await _updateService.DownloadAsync(update);
            AppendLog($"Mise a jour telechargee : {downloaded}");

            var restart = MessageBox.Show(
                "La mise a jour va etre installee. L'application va redemarrer.\n\nContinuer ?",
                "Mise a jour",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (restart == MessageBoxResult.Yes)
            {
                AppendLog("Application de la mise a jour et redemarrage...");
                _updateService.ApplyUpdateAndRestart(downloaded);
                Application.Current.Shutdown();
            }
        }
        catch (Exception ex)
        {
            Status = "Echec de la mise a jour.";
            AppendLog($"Erreur de mise a jour : {ex.Message}");
            if (interactive)
                MessageBox.Show(ex.Message, "Mises a jour", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SaveConfig()
    {
        if (!int.TryParse(AutoFetchIntervalMinutes, out var interval) || interval < 1)
        {
            MessageBox.Show(
                "L'intervalle doit etre un nombre entier de minutes superieur ou egal a 1.",
                "Configuration",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var config = new AppConfig
        {
            SourceUrl = SourceUrl.Trim(),
            OutputFile = OutputFile.Trim(),
            AutoFetchEnabled = AutoFetchEnabled,
            AutoFetchIntervalMinutes = interval,
            CheckUpdatesOnStartup = CheckUpdatesOnStartup,
        };

        try
        {
            _configService.Save(config);
            AppendLog("Configuration enregistree.");
            Status = "Configuration enregistree.";
        }
        catch (Exception ex)
        {
            AppendLog($"Erreur d'enregistrement de la configuration : {ex.Message}");
        }
    }

    private void BrowseOutputFile()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Fichier de sortie",
            Filter = "Fichier texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*",
            FileName = string.IsNullOrWhiteSpace(OutputFile) ? "osbiz_export.txt" : Path.GetFileName(OutputFile),
        };

        if (!string.IsNullOrWhiteSpace(OutputFile))
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(OutputFile));
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;
        }

        if (dialog.ShowDialog() == true)
            OutputFile = dialog.FileName;
    }

    private void OpenOutputFolder()
    {
        try
        {
            var target = OutputFile;
            var directory = string.IsNullOrWhiteSpace(target)
                ? _configService.ConfigDirectory
                : Path.GetDirectoryName(Path.GetFullPath(target));

            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                directory = _configService.ConfigDirectory;

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(directory)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            AppendLog($"Impossible d'ouvrir le dossier : {ex.Message}");
        }
    }

    private void ConfigureAutoFetchTimer()
    {
        if (int.TryParse(AutoFetchIntervalMinutes, out var minutes) && minutes >= 1)
            _autoFetchTimer.Interval = TimeSpan.FromMinutes(minutes);
        else
            _autoFetchTimer.Interval = TimeSpan.FromMinutes(60);

        _autoFetchTimer.IsEnabled = AutoFetchEnabled;
    }

    private void AppendLog(string message)
        => Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");

    public void Dispose()
    {
        _autoFetchTimer.Stop();
        _fetcher.Dispose();
        _updateService.Dispose();
    }
}
