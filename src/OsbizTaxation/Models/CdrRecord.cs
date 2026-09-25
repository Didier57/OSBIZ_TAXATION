namespace OsbizTaxation.Models;

public sealed class CdrRecord
{
    public long Id { get; set; }

    public string Site { get; set; } = string.Empty;

    public string Date { get; set; } = string.Empty;

    /// <summary>Date au format ISO (yyyy-MM-dd) pour les tris et filtres.</summary>
    public string DateIso { get; set; } = string.Empty;

    public string HeureDebut { get; set; } = string.Empty;

    public string HeureFin { get; set; } = string.Empty;

    public string Ligne { get; set; } = string.Empty;

    public string NomLigne { get; set; } = string.Empty;

    public string NumeroInterne { get; set; } = string.Empty;

    public string DureeSonnerie { get; set; } = string.Empty;

    public string DureeAppel { get; set; } = string.Empty;

    public string NumeroExterne { get; set; } = string.Empty;

    public string Information { get; set; } = string.Empty;

    public int InfoCode { get; set; }

    public string NumeroExtra { get; set; } = string.Empty;

    public int DureeAppelSecondes { get; set; }

    public string RawLine { get; set; } = string.Empty;

    public string SourceFile { get; set; } = string.Empty;

    public string DateTransfert { get; set; } = string.Empty;
}
