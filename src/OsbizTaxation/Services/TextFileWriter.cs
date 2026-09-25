using System.IO;

namespace OsbizTaxation.Services;

public static class TextFileWriter
{
    // TODO: adapter le format d'ecriture une fois la structure du fichier texte connue.
    public static void Write(string path, string content)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("Le fichier de sortie n'est pas configure.");

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(fullPath, content);
    }
}
