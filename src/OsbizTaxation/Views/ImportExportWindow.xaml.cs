using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Win32;
using OsbizTaxation.Services;
using OsbizTaxation.ViewModels;

namespace OsbizTaxation.Views;

public partial class ImportExportWindow : Window
{
    private readonly MainViewModel _main;
    private readonly StringBuilder _journal = new();

    public ImportExportWindow(MainViewModel main)
    {
        InitializeComponent();
        _main = main;

        CmdSite.Items.Add("(Deduire du nom du fichier)");
        foreach (var site in main.Config.Sites)
            CmdSite.Items.Add(site.Nom);
        CmdSite.SelectedIndex = 0;

        RefreshCount();
        Log($"Dossier raw : {AppPaths.RawDirectory}");
    }

    private void RefreshCount()
        => TxtCount.Text = $"{_main.Repository.Count()} appel(s) en base.";

    private void OnSupprimerToutClick(object sender, RoutedEventArgs e)
    {
        var total = _main.Repository.Count();
        if (total == 0)
        {
            Warn("La base de donnees est deja vide.");
            return;
        }

        var answer = MessageBox.Show(
            $"Supprimer les {total} appel(s) de la base de donnees ?\n\nCette action est irreversible.",
            "Suppression", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
            return;

        _main.Repository.DeleteAll();
        _main.ReloadRecords();
        RefreshCount();
        Log($"Base de donnees videe ({total} appel(s) supprime(s)).");
    }

    private void OnSupprimerDoublonsClick(object sender, RoutedEventArgs e)
    {
        var removed = _main.Repository.DeduplicateRawLines();
        _main.ReloadRecords();
        RefreshCount();
        Log($"{removed} doublon(s) supprime(s).");
    }

    private void OnImporterFichierClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Selectionnez un fichier raw",
            Filter = "Fichiers CDR (*.txt)|*.txt|Tous les fichiers (*.*)|*.*",
            InitialDirectory = AppPaths.RawDirectory
        };

        if (dialog.ShowDialog(this) != true)
            return;

        var inserted = ImportFiles(new[] { dialog.FileName });
        _main.ReloadRecords();
        RefreshCount();
        Log($"Import termine : {inserted} appel(s) ajoute(s).");
    }

    private void OnImporterDossierClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Selectionnez un dossier de fichiers raw",
            InitialDirectory = AppPaths.RawDirectory
        };

        if (dialog.ShowDialog(this) != true)
            return;

        var files = Directory.GetFiles(dialog.FolderName, "*.txt", SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
        {
            Warn("Aucun fichier .txt dans ce dossier.");
            return;
        }

        var inserted = ImportFiles(files);
        _main.ReloadRecords();
        RefreshCount();
        Log($"{files.Length} fichier(s) traite(s) : {inserted} appel(s) ajoute(s).");
    }

    private int ImportFiles(IEnumerable<string> files)
    {
        var total = 0;
        var horodatage = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        foreach (var file in files)
        {
            try
            {
                var content = File.ReadAllText(file);
                var (site, pays) = ResolveSite(file);
                var records = CdrParser.Parse(content, site, pays, Path.GetFileName(file), _main.Config.Lignes);
                foreach (var record in records)
                    record.DateTransfert = horodatage;

                var inserted = _main.Repository.Insert(records);
                total += inserted;
                Log($"{Path.GetFileName(file)} : {inserted}/{records.Count} appel(s) importe(s).");
            }
            catch (Exception ex)
            {
                Log($"{Path.GetFileName(file)} : erreur - {ex.Message}");
            }
        }

        var groupes = _main.Repository.RebuildGroups();
        if (groupes > 0)
            Log($"{groupes} enregistrement(s) relie(s) a un meme appel.");

        return total;
    }

    /// <summary>Determine le site (et son pays) d'un fichier raw, depuis la liste ou le nom du fichier.</summary>
    private (string Site, string Pays) ResolveSite(string file)
    {
        if (CmdSite.SelectedIndex > 0 && CmdSite.SelectedItem is string nomForce)
        {
            var cfgForce = _main.Config.Sites.FirstOrDefault(s => s.Nom == nomForce);
            if (cfgForce != null)
                return (cfgForce.Nom, Countries.NameFor(cfgForce.PaysCode));
        }

        var nom = Path.GetFileNameWithoutExtension(file);
        var match = Regex.Match(nom, @"^(?<site>.+)_\d{8}_\d{6}$");
        var siteNom = match.Success ? match.Groups["site"].Value : nom;

        var cfg = _main.Config.Sites.FirstOrDefault(
            s => string.Equals(s.Nom, siteNom, StringComparison.OrdinalIgnoreCase));

        return cfg is null ? (siteNom, string.Empty) : (cfg.Nom, Countries.NameFor(cfg.PaysCode));
    }

    private void OnFermerClick(object sender, RoutedEventArgs e) => Close();

    private void Log(string message)
    {
        _journal.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        TxtJournal.Text = _journal.ToString();
    }

    private static void Warn(string message)
        => MessageBox.Show(message, "Import / Export", MessageBoxButton.OK, MessageBoxImage.Warning);
}
