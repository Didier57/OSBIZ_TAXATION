namespace OsbizTaxation.Models;

/// <summary>Nombre d'appels entrants/sortants pour un jour donne (date ISO).</summary>
public sealed record DailyCount(string DateIso, int Entrant, int Sortant);
