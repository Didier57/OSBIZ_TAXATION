namespace OsbizTaxation.Models;

/// <summary>Parametres du serveur SMTP pour l'envoi d'emails.</summary>
public sealed class EmailConfig
{
    public string Hote { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string Login { get; set; } = string.Empty;

    public string MotDePasse { get; set; } = string.Empty;

    public string Expediteur { get; set; } = string.Empty;

    public string NomAffiche { get; set; } = string.Empty;
}
