using System.IO;
using System.Text.Json;
using OsbizTaxation.Models;

namespace OsbizTaxation.Services;

/// <summary>Charge et enregistre la configuration (config.json). Les mots de passe sont chiffres (DPAPI).</summary>
public static class ConfigService
{
    public static string LastError { get; private set; } = string.Empty;

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AppConfig Load()
    {
        LastError = string.Empty;
        try
        {
            if (!File.Exists(AppPaths.ConfigPath))
                return new AppConfig();

            var json = File.ReadAllText(AppPaths.ConfigPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, Options) ?? new AppConfig();

            foreach (var site in config.Sites)
                site.MotDePasse = SecretProtector.Unprotect(site.MotDePasse);

            config.Email ??= new EmailConfig();
            config.Email.MotDePasse = SecretProtector.Unprotect(config.Email.MotDePasse);

            return config;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return new AppConfig();
        }
    }

    public static bool Save(AppConfig config)
    {
        LastError = string.Empty;
        try
        {
            var clone = new AppConfig
            {
                CheckUpdatesOnStartup = config.CheckUpdatesOnStartup,
                AutoTransferEnabled = config.AutoTransferEnabled,
                AutoTransferIntervalMinutes = config.AutoTransferIntervalMinutes,
                StartWithWindows = config.StartWithWindows,
                StartMinimized = config.StartMinimized,
                DernierDossier = config.DernierDossier,
                Theme = config.Theme,
                ColumnWidths = config.ColumnWidths.ToDictionary(
                    pair => pair.Key,
                    pair => new Dictionary<string, double>(pair.Value)),
                ColumnOrders = config.ColumnOrders.ToDictionary(
                    pair => pair.Key,
                    pair => new List<string>(pair.Value)),
                HiddenColumns = config.HiddenColumns.ToDictionary(
                    pair => pair.Key,
                    pair => new List<string>(pair.Value)),
                Sites = config.Sites.Select(s => new SiteConfig
                {
                    Nom = s.Nom,
                    Adresse = s.Adresse,
                    Utilisateur = s.Utilisateur,
                    MotDePasse = SecretProtector.Protect(s.MotDePasse),
                    PaysCode = s.PaysCode,
                    SupprimerApresTransfert = s.SupprimerApresTransfert
                }).ToList(),
                Lignes = config.Lignes.Select(l => new LineConfig
                {
                    NumDebut = l.NumDebut,
                    NumFin = l.NumFin,
                    Site = l.Site,
                    NomLigne = l.NomLigne
                }).ToList(),
                Email = new EmailConfig
                {
                    Hote = config.Email?.Hote ?? string.Empty,
                    Port = config.Email?.Port ?? 587,
                    UseSsl = config.Email?.UseSsl ?? true,
                    Login = config.Email?.Login ?? string.Empty,
                    MotDePasse = SecretProtector.Protect(config.Email?.MotDePasse ?? string.Empty),
                    Expediteur = config.Email?.Expediteur ?? string.Empty,
                    NomAffiche = config.Email?.NomAffiche ?? string.Empty
                }
            };

            Directory.CreateDirectory(AppPaths.AppDirectory);
            File.WriteAllText(AppPaths.ConfigPath, JsonSerializer.Serialize(clone, Options));
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }
}
