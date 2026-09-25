using System.IO;
using System.Text;

namespace OsbizTaxation.Services;

public static class AppPaths
{
    public static string AppDirectory { get; } = ResolveBaseDirectory();

    public static string RawDirectory => Path.Combine(AppDirectory, "raw");

    public static string DatabasePath => Path.Combine(AppDirectory, "cdr.db");

    public static string ConfigPath => Path.Combine(AppDirectory, "config.json");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(RawDirectory);
    }

    private static string ResolveBaseDirectory()
    {
        var dir = AppContext.BaseDirectory;
        try
        {
            var probe = Path.Combine(dir, ".write_probe");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return dir;
        }
        catch
        {
            var fallback = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OsbizTaxation");
            Directory.CreateDirectory(fallback);
            return fallback;
        }
    }
}

public static class SecretProtector
{
    public static string Protect(string clearText)
    {
        if (string.IsNullOrEmpty(clearText))
            return string.Empty;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(clearText);
            var protectedBytes = System.Security.Cryptography.ProtectedData.Protect(
                bytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }
        catch
        {
            return clearText;
        }
    }

    public static string Unprotect(string protectedText)
    {
        if (string.IsNullOrEmpty(protectedText))
            return string.Empty;

        try
        {
            var bytes = Convert.FromBase64String(protectedText);
            var clearBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                bytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(clearBytes);
        }
        catch
        {
            return protectedText;
        }
    }

    public static bool IsProtected(string value)
    {
        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
