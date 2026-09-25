namespace OsbizTaxation.Models;

public sealed class AppConfig
{
    public List<SiteConfig> Sites { get; set; } = new();

    public List<LineConfig> Lignes { get; set; } = new();

    public bool CheckUpdatesOnStartup { get; set; } = true;

    public bool AutoTransferEnabled { get; set; }

    public int AutoTransferIntervalMinutes { get; set; } = 60;

    /// <summary>Dernier chemin de fichier utilise pour un import/export.</summary>
    public string DernierDossier { get; set; } = string.Empty;

    /// <summary>Theme de l'interface : "Clair" ou "Sombre".</summary>
    public string Theme { get; set; } = "Clair";
}
