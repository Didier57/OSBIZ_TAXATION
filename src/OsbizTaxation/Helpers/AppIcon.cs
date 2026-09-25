using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OsbizTaxation.Services;

namespace OsbizTaxation.Helpers;

/// <summary>
/// Fournit l'icone de l'application (celle integree a l'exe) pour la fenetre et la zone de notification.
/// </summary>
public static class AppIcon
{
    private static readonly Uri PackUri = new("pack://application:,,,/Assets/app.ico", UriKind.Absolute);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

    /// <summary>
    /// Donne une identite explicite a l'application afin que Windows associe et mette en
    /// cache la bonne icone dans la barre des taches.
    /// </summary>
    public static void SetAppUserModelId(string appId)
    {
        try
        {
            SetCurrentProcessExplicitAppUserModelID(appId);
        }
        catch (Exception ex)
        {
            AppLog.WriteException("AppUserModelID", ex);
        }
    }

    /// <summary>Icone pour la barre de titre / barre des taches.</summary>
    public static ImageSource? WindowIcon()
    {
        // On passe par System.Drawing (comme la zone de notification, qui fonctionne) :
        // la conversion BitmapImage (PackUri) -> HICON faite par WPF peut donner une icone generique.
        try
        {
            var info = Application.GetResourceStream(PackUri);
            if (info?.Stream is { } stream)
            {
                using (stream)
                {
                    System.Drawing.Icon? ico = null;
                    try
                    {
                        ico = new System.Drawing.Icon(stream, new System.Drawing.Size(256, 256));
                    }
                    catch
                    {
                        stream.Position = 0;
                        ico = new System.Drawing.Icon(stream);
                    }

                    if (ico != null)
                    {
                        using (ico)
                        {
                            var source = Imaging.CreateBitmapSourceFromHIcon(
                                ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            source.Freeze();
                            return source;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.WriteException("Icone de la fenetre", ex);
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
        catch (Exception ex)
        {
            AppLog.WriteException("Icone de la zone de notification", ex);
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
        catch (Exception ex)
        {
            AppLog.WriteException("Icone de l'executable", ex);
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
        catch (Exception ex)
        {
            AppLog.WriteException("Icone de l'executable (tray)", ex);
        }

        return null;
    }
}
