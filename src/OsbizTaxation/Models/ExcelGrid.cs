namespace OsbizTaxation.Models;

/// <summary>Cellule d'une grille exportee vers Excel (texte ou nombre, style simple).</summary>
public sealed class ExcelGridCell
{
    public string? Text { get; init; }
    public double? Number { get; init; }
    public bool Header { get; init; }
    public bool Bold { get; init; }
    public bool Right { get; init; }

    public static readonly ExcelGridCell Empty = new();
}

/// <summary>Grille generique exportable vers Excel, avec cellules fusionnees et largeurs de colonnes.</summary>
public sealed class ExcelGrid
{
    public int ColumnCount { get; set; }
    public double DefaultColumnWidth { get; set; } = 11;
    public Dictionary<int, double> ColumnWidths { get; } = new();
    public List<List<ExcelGridCell>> Rows { get; } = new();
    public List<(int Row, int Col, int RowSpan, int ColSpan)> Merges { get; } = new();
}
