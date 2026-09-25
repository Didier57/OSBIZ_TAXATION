using System.Collections.ObjectModel;
using OsbizTaxation.Models;
using OsbizTaxation.Services;

namespace OsbizTaxation.ViewModels;

public sealed class ConfigViewModel : ObservableObject
{
    private readonly AppConfig _original;
    private bool _checkUpdatesOnStartup;
    private bool _autoTransferEnabled;
    private string _autoTransferIntervalMinutes = "60";
    private string _emailHote = string.Empty;
    private string _emailPort = "587";
    private bool _emailSsl = true;
    private string _emailLogin = string.Empty;
    private string _emailMotDePasse = string.Empty;
    private string _emailExpediteur = string.Empty;
    private string _emailNomAffiche = string.Empty;

    public ConfigViewModel(AppConfig config)
    {
        _original = config;
        Sites = new ObservableCollection<SiteConfig>(config.Sites);
        Lignes = new ObservableCollection<LineConfig>(config.Lignes);
        _checkUpdatesOnStartup = config.CheckUpdatesOnStartup;
        _autoTransferEnabled = config.AutoTransferEnabled;
        _autoTransferIntervalMinutes = config.AutoTransferIntervalMinutes.ToString();
        DernierDossier = config.DernierDossier;

        var email = config.Email ?? new EmailConfig();
        _emailHote = email.Hote;
        _emailPort = email.Port.ToString();
        _emailSsl = email.UseSsl;
        _emailLogin = email.Login;
        _emailMotDePasse = email.MotDePasse;
        _emailExpediteur = email.Expediteur;
        _emailNomAffiche = email.NomAffiche;
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

    public string EmailHote
    {
        get => _emailHote;
        set => SetProperty(ref _emailHote, value);
    }

    public string EmailPort
    {
        get => _emailPort;
        set => SetProperty(ref _emailPort, value);
    }

    public bool EmailSsl
    {
        get => _emailSsl;
        set => SetProperty(ref _emailSsl, value);
    }

    public string EmailLogin
    {
        get => _emailLogin;
        set => SetProperty(ref _emailLogin, value);
    }

    public string EmailMotDePasse
    {
        get => _emailMotDePasse;
        set => SetProperty(ref _emailMotDePasse, value);
    }

    public string EmailExpediteur
    {
        get => _emailExpediteur;
        set => SetProperty(ref _emailExpediteur, value);
    }

    public string EmailNomAffiche
    {
        get => _emailNomAffiche;
        set => SetProperty(ref _emailNomAffiche, value);
    }

    public EmailConfig ToEmailConfig()
    {
        if (!int.TryParse(EmailPort, out var port) || port < 1 || port > 65535)
            port = 587;

        return new EmailConfig
        {
            Hote = EmailHote.Trim(),
            Port = port,
            UseSsl = EmailSsl,
            Login = EmailLogin.Trim(),
            MotDePasse = EmailMotDePasse,
            Expediteur = EmailExpediteur.Trim(),
            NomAffiche = EmailNomAffiche.Trim()
        };
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
            DernierDossier = DernierDossier,
            Theme = _original.Theme,
            ColumnWidths = _original.ColumnWidths.ToDictionary(
                pair => pair.Key,
                pair => new Dictionary<string, double>(pair.Value)),
            ColumnOrders = _original.ColumnOrders.ToDictionary(
                pair => pair.Key,
                pair => new List<string>(pair.Value)),
            HiddenColumns = _original.HiddenColumns.ToDictionary(
                pair => pair.Key,
                pair => new List<string>(pair.Value)),
            Email = ToEmailConfig()
        };
    }
}
