namespace OsbizTaxation.Models;

/// <summary>Nombre d'appels entrants/sortants pour une heure donnee (0 a 23).</summary>
public sealed record HourCount(int Hour, int Entrant, int Sortant);
