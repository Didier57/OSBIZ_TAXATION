using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OsbizTaxation.Helpers;

/// <summary>
/// Convertit un numero de groupe d'appel en couleur de fond : les enregistrements
/// d'un meme appel partagent une meme teinte, 0 (aucun groupe) reste transparent.
/// </summary>
public sealed class GroupBrushConverter : IValueConverter
{
    private static readonly Brush[] Palette =
    {
        Brushes.Transparent,
        new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xCD)),
        new SolidColorBrush(Color.FromRgb(0xD9, 0xED, 0xFF)),
        new SolidColorBrush(Color.FromRgb(0xDF, 0xF5, 0xE3)),
        new SolidColorBrush(Color.FromRgb(0xFB, 0xE1, 0xE8)),
        new SolidColorBrush(Color.FromRgb(0xEC, 0xE0, 0xF5)),
        new SolidColorBrush(Color.FromRgb(0xFF, 0xE8, 0xD8)),
        new SolidColorBrush(Color.FromRgb(0xE0, 0xF7, 0xF5)),
        new SolidColorBrush(Color.FromRgb(0xF0, 0xEF, 0xC8))
    };

    // Teintes plus sombres pour le theme sombre (texte clair lisible).
    private static readonly Brush[] DarkPalette =
    {
        Brushes.Transparent,
        new SolidColorBrush(Color.FromRgb(0x3A, 0x34, 0x21)),
        new SolidColorBrush(Color.FromRgb(0x1F, 0x37, 0x4A)),
        new SolidColorBrush(Color.FromRgb(0x1E, 0x3D, 0x2A)),
        new SolidColorBrush(Color.FromRgb(0x44, 0x23, 0x2E)),
        new SolidColorBrush(Color.FromRgb(0x36, 0x2A, 0x44)),
        new SolidColorBrush(Color.FromRgb(0x4A, 0x33, 0x20)),
        new SolidColorBrush(Color.FromRgb(0x1E, 0x3A, 0x38)),
        new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x1E))
    };

    /// <summary>Vrai lorsque le theme sombre est actif (palette de fond assombrie).</summary>
    public static bool DarkMode { get; set; }

    static GroupBrushConverter()
    {
        foreach (var brush in Palette.Concat(DarkPalette))
        {
            if (brush is SolidColorBrush solid)
                solid.Freeze();
        }
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var palette = DarkMode ? DarkPalette : Palette;
        var groupe = value is int g ? g : 0;
        if (groupe <= 0)
            return palette[0];

        return palette[((groupe - 1) % (palette.Length - 1)) + 1];
    }

    /// <summary>Couleur RGB (6 chiffres hexa) du fond pour un groupe, ou null si aucun groupe.</summary>
    public static string? HexFor(int groupe)
    {
        if (groupe <= 0)
            return null;
        var brush = Palette[((groupe - 1) % (Palette.Length - 1)) + 1];
        return brush is SolidColorBrush solid
            ? $"{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}"
            : null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
