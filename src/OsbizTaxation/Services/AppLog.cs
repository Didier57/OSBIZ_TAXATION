using System.IO;
using System.Text;

namespace OsbizTaxation.Services;

/// <summary>Journal d'application (fichier <c>osbiz.log</c> a cote de l'executable).</summary>
public static class AppLog
{
    public static string LogPath => Path.Combine(AppPaths.AppDirectory, "osbiz.log");

    public static void Write(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            File.AppendAllText(LogPath, line, Encoding.UTF8);
        }
        catch
        {
            // Le journal ne doit jamais faire echouer l'application.
        }
    }

    public static void WriteException(string context, Exception ex)
    {
        var sb = new StringBuilder();
        sb.AppendLine(context);
        var current = ex;
        var depth = 0;
        while (current is not null)
        {
            sb.AppendLine($"    [{depth}] {current.GetType().FullName} : {current.Message}");
            if (!string.IsNullOrWhiteSpace(current.StackTrace))
                sb.AppendLine("        " + current.StackTrace.Replace("\n", "\n        "));
            current = current.InnerException;
            depth++;
        }

        Write(sb.ToString());
    }

    /// <summary>Concatene les messages d'une exception et de ses exceptions internes.</summary>
    public static string Describe(Exception ex)
    {
        var sb = new StringBuilder();
        var current = ex;
        var depth = 0;
        while (current is not null)
        {
            if (depth > 0)
                sb.Append(" -> ");
            sb.Append(current.Message);
            current = current.InnerException;
            depth++;
        }

        return sb.ToString();
    }
}
