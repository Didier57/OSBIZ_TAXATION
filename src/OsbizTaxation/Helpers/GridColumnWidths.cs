using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace OsbizTaxation.Helpers;

/// <summary>Memorise et restaure la largeur des colonnes des tables pour l'utilisateur.</summary>
public static class GridColumnWidths
{
    public static void Apply(DataGrid grid, Dictionary<string, double>? widths)
    {
        if (widths is null || widths.Count == 0)
            return;

        foreach (var column in grid.Columns)
        {
            if (widths.TryGetValue(Key(column), out var value) && value >= 1)
                column.Width = new DataGridLength(value);
        }
    }

    public static void Capture(DataGrid grid, Dictionary<string, double> widths)
    {
        foreach (var column in grid.Columns)
        {
            if (column.Visibility != Visibility.Visible)
                continue;

            // Seules les colonnes dont la largeur a ete fixee (par l'utilisateur
            // ou dans le XAML) sont memorisees ; les colonnes "Auto" restent auto.
            if (!column.Width.IsAbsolute)
                continue;

            var value = column.ActualWidth;
            if (value >= 1)
                widths[Key(column)] = Math.Round(value);
        }
    }

    public static string Key(DataGridColumn column)
    {
        if (column is DataGridBoundColumn bound &&
            bound.Binding is Binding binding &&
            !string.IsNullOrEmpty(binding.Path?.Path))
        {
            return binding.Path.Path;
        }

        return column.SortMemberPath is { Length: > 0 } path
            ? path
            : column.Header?.ToString() ?? string.Empty;
    }
}
