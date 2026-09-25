using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OsbizTaxation.Helpers;

/// <summary>
/// Fournit l'icone de l'application (celle integree a l'exe) pour la fenetre et la zone de notification.
/// </summary>
public static class AppIcon
{
    private static readonly Uri PackUri = new("pack://application:,,,/Assets/app.ico", UriKind.Absolute);

    /// <summary>Icone pour la barre de titre / barre des taches.</summary>
    public static ImageSource? WindowIcon()
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = PackUri;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
        }

        return FromExe();
    }

    /// <summary>Icone (System.Drawing) pour la zone de notification.</summary>
    public static System.Drawing.Icon? TrayHandle()
    {
        try
        {
            var info = Application.GetResourceStream(PackUri);
            if (info?.Stream is { } stream)
            {
                using (stream)
                {
                    return new System.Drawing.Icon(stream);
                }
            }
        }
        catch
        {
        }

        return FromExeHandle();
    }

    private static ImageSource? FromExe()
    {
        try
        {
            var path = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(path))
            {
                using var ico = System.Drawing.Icon.ExtractAssociatedIcon(path);
                if (ico != null)
                {
                    var source = Imaging.CreateBitmapSourceFromHIcon(
                        ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    source.Freeze();
                    return source;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static System.Drawing.Icon? FromExeHandle()
    {
        try
        {
            var path = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(path))
                return System.Drawing.Icon.ExtractAssociatedIcon(path);
        }
        catch
        {
        }

        return null;
    }
}
