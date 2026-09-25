using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using OsbizTaxation.Helpers;
using OsbizTaxation.Models;
using OsbizTaxation.Services;
using OsbizTaxation.ViewModels;

namespace OsbizTaxation.Views;

public partial class RechercheWindow : Window
{
    private const string FmtIso = "yyyy-MM-dd";
    private readonly MainViewModel _main;
    private bool _initialise = true;

    public RechercheWindow(MainViewModel main)
    {
        InitializeComponent();
        Icon = WindowIcons.Create(WindowIcons.Search);
        _main = main;

        CmdPeriode.SelectedIndex = 0;
        DpDate.SelectedDate = DateTime.Today;

        var sites = main.Config.Sites.Select(s => s.Nom).ToList();
        CmdSite.ItemsSource = sites;
        if (sites.Count > 0)
            CmdSite.SelectedIndex = 0;

        GridResultats.ItemsSource = Resultats;
        ExcelFilter.Attach(GridResultats);

        _initialise = false;
        RunSearch();
    }

    public ObservableCollection<CdrRecord> Resultats { get; } = new();

    private string Periode => (CmdPeriode.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Jour";

    private void OnSiteChanged(object sender, SelectionChangedEventArgs e) => RunSearch();

    private void OnPeriodeChanged(object sender, SelectionChangedEventArgs e) => RunSearch();

    private void OnDateChanged(object sender, SelectionChangedEventArgs e) => RunSearch();

    private void OnPrecedentClick(object sender, RoutedEventArgs e) => Decaler(-1);

    private void OnSuivantClick(object sender, RoutedEventArgs e) => Decaler(1);

    private void OnAujourdhuiClick(object sender, RoutedEventArgs e)
    {
        DpDate.SelectedDate = DateTime.Today;
        RunSearch();
    }

    private void Decaler(int sens)
    {
        var date = DpDate.SelectedDate ?? DateTime.Today;
        DpDate.SelectedDate = Periode switch
        {
            "Mois" => date.AddMonths(sens),
            "Année" => date.AddYears(sens),
            _ => date.AddDays(sens)
        };
        RunSearch();
    }

    private (string? Debut, string? Fin, string Libelle) GetPlage()
    {
        var date = DpDate.SelectedDate ?? DateTime.Today;
        switch (Periode)
        {
            case "Mois":
                var premier = new DateTime(date.Year, date.Month, 1);
                var dernier = premier.AddMonths(1).AddDays(-1);
                return (premier.ToString(FmtIso, CultureInfo.InvariantCulture),
                        dernier.ToString(FmtIso, CultureInfo.InvariantCulture),
                        premier.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("fr-FR")));
            case "Année":
                return (new DateTime(date.Year, 1, 1).ToString(FmtIso, CultureInfo.InvariantCulture),
                        new DateTime(date.Year, 12, 31).ToString(FmtIso, CultureInfo.InvariantCulture),
                        date.Year.ToString(CultureInfo.InvariantCulture));
            default:
                return (date.ToString(FmtIso, CultureInfo.InvariantCulture),
                        date.ToString(FmtIso, CultureInfo.InvariantCulture),
                        date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        }
    }

    private void RunSearch()
    {
        if (_initialise)
            return;

        var site = CmdSite.SelectedItem as string;
        if (string.IsNullOrEmpty(site))
        {
            Resultats.Clear();
            TxtResultat.Text = "Aucun site configuré.";
            return;
        }

        var (debut, fin, libelle) = GetPlage();
        var results = _main.Repository.Search(site, null, null, debut, fin);

        Resultats.Clear();
        foreach (var record in results)
            Resultats.Add(record);

        TxtResultat.Text = $"{site} — {Periode} {libelle} : {Resultats.Count} résultat(s).";
    }

    private void OnSupprimerFiltresClick(object sender, RoutedEventArgs e) => ExcelFilter.Clear(GridResultats);

    private void OnColonnesClick(object sender, RoutedEventArgs e) =>
        ColumnManager.Show(GridResultats, (UIElement)sender);

    private void OnExportExcelClick(object sender, RoutedEventArgs e)
    {
        var columns = GridResultats.Columns.Where(c => c.Visibility == Visibility.Visible).ToList();
        if (columns.Count == 0)
        {
            MessageBox.Show(this, "Aucune colonne à exporter.", "Export Excel", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var paths = columns.Select(ExcelFilter.GetColumnPath).ToList();
        var headers = columns.Select(ExcelFilter.GetColumnTitle).ToList();

        var view = CollectionViewSource.GetDefaultView(GridResultats.ItemsSource);
        var rows = new List<(string[], string?)>();
        foreach (var item in view)
        {
            var cells = paths.Select(p => ExcelFilter.GetCellValue(item, p)).ToArray();
            var fill = item is CdrRecord record ? GroupBrushConverter.HexFor(record.Groupe) : null;
            rows.Add((cells, fill));
        }

        if (rows.Count == 0)
        {
            MessageBox.Show(this, "Aucune ligne à exporter.", "Export Excel", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var site = CmdSite.SelectedItem as string ?? "Site";
        var dialog = new SaveFileDialog
        {
            Filter = "Classeur Excel (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            FileName = $"Recherche_{Sanitize(site)}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            ExcelExporter.Export(dialog.FileName, headers, rows, "Recherche");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Erreur lors de l'export :\n" + ex.Message,
                "Export Excel", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var ouvrir = MessageBox.Show(this,
            $"{rows.Count} ligne(s) exportée(s) vers :\n{dialog.FileName}\n\nVoulez-vous ouvrir le fichier ?",
            "Export Excel", MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (ouvrir == MessageBoxResult.Yes)
            OuvrirFichier(dialog.FileName);
    }

    private void OuvrirFichier(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Impossible d'ouvrir le fichier :\n" + ex.Message,
                "Export Excel", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }

    private void OnFermerClick(object sender, RoutedEventArgs e) => Close();
}
