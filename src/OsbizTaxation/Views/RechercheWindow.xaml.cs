using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using OsbizTaxation.Helpers;
using OsbizTaxation.Models;
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

    private void OnFermerClick(object sender, RoutedEventArgs e) => Close();
}
