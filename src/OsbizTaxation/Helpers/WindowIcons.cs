using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OsbizTaxation.Helpers;

/// <summary>
/// Construit les icones de fenetre a partir de glyphes de la police
/// "Segoe MDL2 Assets" (disponible sur Windows 10/11).
/// </summary>
public static class WindowIcons
{
    // Glyphes Segoe MDL2 Assets.
    public const string Headset = "\uE95B";       // ecouteur telephonique
    public const string Gear = "\uE713";          // configuration (engrenage)
    public const string Search = "\uE721";        // loupe
    public const string Download = "\uE896";      // transfert
    public const string ImportExport = "\uE8B5";  // import / export
    public const string Chart = "\uE908";         // statistiques (barres)
    public const string Money = "\uE8C7";         // taxation (billet)

    private static readonly Color DefaultBackground = Color.FromRgb(0x0B, 0x66, 0xC3);

    /// <summary>Cree une icone (cercle colore + glyphe blanc) utilisable comme Window.Icon.</summary>
    public static ImageSource Create(string glyph, Color? background = null, int size = 32)
    {
        var bg = background ?? DefaultBackground;
        var typeface = new Typeface(new FontFamily("Segoe MDL2 Assets"),
            FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var center = new Point(size / 2.0, size / 2.0);
            dc.DrawEllipse(new SolidColorBrush(bg), null, center, size / 2.0, size / 2.0);

            var text = new FormattedText(
                glyph,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                size * 0.58,
                Brushes.White,
                1.0);

            dc.DrawText(text, new Point((size - text.Width) / 2.0, (size - text.Height) / 2.0));
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}
