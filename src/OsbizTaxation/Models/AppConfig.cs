namespace OsbizTaxation.Models;

public sealed class AppConfig
{
    public List<SiteConfig> Sites { get; set; } = new();

    public List<LineConfig> Lignes { get; set; } = new();

    public bool CheckUpdatesOnStartup { get; set; } = true;

    public bool AutoTransferEnabled { get; set; }

    public int AutoTransferIntervalMinutes { get; set; } = 60;

    /// <summary>Demarrer l'application automatiquement avec Windows (reduite).</summary>
    public bool StartWithWindows { get; set; }

    /// <summary>Demarrer l'application reduite dans la zone de notification.</summary>
    public bool StartMinimized { get; set; }

    /// <summary>Dernier chemin de fichier utilise pour un import/export.</summary>
    public string DernierDossier { get; set; } = string.Empty;

    /// <summary>Theme de l'interface : "Clair" ou "Sombre".</summary>
    public string Theme { get; set; } = "Clair";

    /// <summary>Largeur memorisee des colonnes, par nom de table puis par nom de colonne.</summary>
    public Dictionary<string, Dictionary<string, double>> ColumnWidths { get; set; } = new();

    /// <summary>Ordre memorise des colonnes, par nom de table.</summary>
    public Dictionary<string, List<string>> ColumnOrders { get; set; } = new();

    /// <summary>Colonnes masquees, par nom de table.</summary>
    public Dictionary<string, List<string>> HiddenColumns { get; set; } = new();

    /// <summary>Parametres du serveur d'envoi d'emails (SMTP).</summary>
    public EmailConfig Email { get; set; } = new();
}
