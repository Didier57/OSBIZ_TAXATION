using System.Collections.ObjectModel;
using OsbizTaxation.Models;
using OsbizTaxation.Services;

namespace OsbizTaxation.ViewModels;

public sealed class ConfigViewModel : ObservableObject
{
    private bool _checkUpdatesOnStartup;
    private bool _autoTransferEnabled;
    private string _autoTransferIntervalMinutes = "60";

    public ConfigViewModel(AppConfig config)
    {
        Sites = new ObservableCollection<SiteConfig>(config.Sites);
        Lignes = new ObservableCollection<LineConfig>(config.Lignes);
        _checkUpdatesOnStartup = config.CheckUpdatesOnStartup;
        _autoTransferEnabled = config.AutoTransferEnabled;
        _autoTransferIntervalMinutes = config.AutoTransferIntervalMinutes.ToString();
        DernierDossier = config.DernierDossier;
    }

    public ObservableCollection<SiteConfig> Sites { get; }

    public ObservableCollection<LineConfig> Lignes { get; }

    public IReadOnlyList<Country> PaysList => Countries.All;

    public string DernierDossier { get; set; }

    public bool CheckUpdatesOnStartup
    {
        get => _checkUpdatesOnStartup;
        set => SetProperty(ref _checkUpdatesOnStartup, value);
    }

    public bool AutoTransferEnabled
    {
        get => _autoTransferEnabled;
        set => SetProperty(ref _autoTransferEnabled, value);
    }

    public string AutoTransferIntervalMinutes
    {
        get => _autoTransferIntervalMinutes;
        set => SetProperty(ref _autoTransferIntervalMinutes, value);
    }

    public AppConfig ToConfig()
    {
        int.TryParse(AutoTransferIntervalMinutes, out var interval);
        return new AppConfig
        {
            Sites = Sites.ToList(),
            Lignes = Lignes.ToList(),
            CheckUpdatesOnStartup = CheckUpdatesOnStartup,
            AutoTransferEnabled = AutoTransferEnabled,
            AutoTransferIntervalMinutes = interval >= 1 ? interval : 60,
            DernierDossier = DernierDossier
        };
    }
}
