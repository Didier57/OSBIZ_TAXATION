namespace OsbizTaxation.Models;

public sealed class SiteConfig
{
    public string Nom { get; set; } = string.Empty;

    public string Adresse { get; set; } = string.Empty;

    public string Utilisateur { get; set; } = string.Empty;

    public string MotDePasse { get; set; } = string.Empty;

    public string PaysCode { get; set; } = string.Empty;

    public bool SupprimerApresTransfert { get; set; } = true;
}
