using OsbizTaxation.Helpers;

namespace OsbizTaxation.Models;

/// <summary>Duree cumulee des appels d'un poste, pour le TOP Duree.</summary>
public sealed record TopDuration(string NumeroInterne, long Secondes)
{
    /// <summary>Duree mise en forme pour l'affichage dans la table (ex. "2 h 40 m 7 s").</summary>
    public string DureeAffichee => DurationFormat.Hms(Secondes);
}
