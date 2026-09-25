namespace OsbizTaxation.Models;

/// <summary>nombre d'appels pour un site, un type d'appel et une date donnes (pour le TCD).</summary>
public sealed record PivotCall(string Site, string Information, string DateIso, int Count);
