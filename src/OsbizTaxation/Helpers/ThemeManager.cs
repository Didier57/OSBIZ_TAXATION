using System.Windows;

namespace OsbizTaxation.Helpers;

/// <summary>Applique le theme clair ou sombre a l'ensemble de l'application.</summary>
public static class ThemeManager
{
    public const string Light = "Clair";
    public const string Dark = "Sombre";

    private static ResourceDictionary? _current;

    /// <summary>Theme actuellement applique.</summary>
    public static string Current { get; private set; } = Light;

    public static bool IsDark => Current == Dark;

    /// <summary>Remplace le dictionnaire de couleurs par celui du theme demande.</summary>
    public static void Apply(string? theme)
    {
        if (theme != Dark)
            theme = Light;

        var app = Application.Current;
        if (app == null)
            return;

        var uri = new Uri(
            theme == Dark ? "pack://application:,,,/Themes/Dark.xaml" : "pack://application:,,,/Themes/Light.xaml",
            UriKind.Absolute);

        var dictionary = new ResourceDictionary { Source = uri };

        if (_current != null)
            app.Resources.MergedDictionaries.Remove(_current);

        app.Resources.MergedDictionaries.Insert(0, dictionary);
        _current = dictionary;
        Current = theme;
        GroupBrushConverter.DarkMode = theme == Dark;
    }
}
