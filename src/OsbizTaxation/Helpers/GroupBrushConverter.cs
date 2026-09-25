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

    static GroupBrushConverter()
    {
        foreach (var brush in Palette)
        {
            if (brush is SolidColorBrush solid)
                solid.Freeze();
        }
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var groupe = value is int g ? g : 0;
        if (groupe <= 0)
            return Palette[0];

        return Palette[((groupe - 1) % (Palette.Length - 1)) + 1];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
