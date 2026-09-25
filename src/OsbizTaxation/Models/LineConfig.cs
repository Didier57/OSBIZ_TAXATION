namespace OsbizTaxation.Models;

public sealed class LineConfig
{
    public int NumDebut { get; set; }

    public int NumFin { get; set; }

    public string Site { get; set; } = string.Empty;

    public string NomLigne { get; set; } = string.Empty;

    public bool Contient(int numero) => numero >= NumDebut && numero <= NumFin;
}
