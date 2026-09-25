using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using OsbizTaxation.Models;
using OsbizTaxation.Services;

namespace OsbizTaxation.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly UpdateService _updateService = new();
    private readonly CdrTransferService _transferService = new();
    private readonly DispatcherTimer _autoTimer = new();

    private const int RecentDays = 31;

    private string _status = "Pret.";
    private string _lastTransfer = "Jamais";
    private bool _isBusy;

    public MainViewModel()
    {
        Config = ConfigService.Load();
        Repository = new CdrRepository();
        ReloadRecords();

        _autoTimer.Tick += async (_, _) => await RunAutoTransferAsync();
        ConfigureAutoTimer();

        AppendLog($"Application demarree (version {Version}).");
        AppendLog($"Dossier de travail : {AppPaths.AppDirectory}");
        AppendLog($"{Config.Sites.Count} site(s) configure(s).");
    }

    public AppConfig Config { get; private set; }

    public CdrRepository Repository { get; }

    public ObservableCollection<CdrRecord> Records { get; } = new();

    public ObservableCollection<string> Logs { get; } = new();

    public string Version => _updateService.CurrentVersion.ToString();

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string LastTransfer
    {
        get => _lastTransfer;
        private set => SetProperty(ref _lastTransfer, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public void ApplyConfig(AppConfig config)
    {
        Config = config;
        ConfigureAutoTimer();
        AppendLog("Configuration mise a jour.");
    }

    public void ReloadRecords()
    {
        Records.Clear();
        foreach (var record in Repository.GetRecent(RecentDays))
            Records.Add(record);

        Status = $"{Records.Count} appel(s) — {RecentDays} derniers jours.";
    }

    public async Task<int> TransferSitesAsync(
        IEnumerable<SiteConfig> sites,
        IProgress<string> log,
        CancellationToken ct = default)
    {
        IsBusy = true;
        var total = 0;

        try
        {
            foreach (var site in sites)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var result = await _transferService.TransferAsync(site, Config, Repository, log, ct);
                    total += result.Appels;
                }
                catch (Exception ex)
                {
                    log.Report($"[{site.Nom}] Erreur : {ex.Message}");
                }
            }
        }
        finally
        {
            IsBusy = false;
        }

        ReloadRecords();
        LastTransfer = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        return total;
    }

    public async Task CheckUpdatesOnStartupAsync()
    {
        if (!Config.CheckUpdatesOnStartup)
            return;

        await CheckForUpdatesAsync(interactive: false);
    }

    public async Task CheckForUpdatesAsync(bool interactive)
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
                        "Mises a jour", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AppendLog($"Nouvelle version disponible : {update.Version}.");
            var answer = MessageBox.Show(
                $"Une nouvelle version ({update.Version}) est disponible.\n\nTelecharger et installer maintenant ?",
                "Mise a jour", MessageBoxButton.YesNo, MessageBoxImage.Question);

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
                "Mise a jour", MessageBoxButton.YesNo, MessageBoxImage.Question);

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

    private async Task RunAutoTransferAsync()
    {
        if (IsBusy || Config.Sites.Count == 0)
            return;

        AppendLog("Transfert automatique demarre.");
        var progress = new Progress<string>(AppendLog);
        var total = await TransferSitesAsync(Config.Sites, progress);
        AppendLog($"Transfert automatique termine : {total} appel(s).");
    }

    private void ConfigureAutoTimer()
    {
        var minutes = Config.AutoTransferIntervalMinutes >= 1 ? Config.AutoTransferIntervalMinutes : 60;
        _autoTimer.Interval = TimeSpan.FromMinutes(minutes);
        _autoTimer.IsEnabled = Config.AutoTransferEnabled;
    }

    public void AppendLog(string message)
        => Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");

    public void Dispose()
    {
        _autoTimer.Stop();
        Repository.Dispose();
        _updateService.Dispose();
    }
}
