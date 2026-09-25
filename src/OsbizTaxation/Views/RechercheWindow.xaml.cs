using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using OsbizTaxation.Helpers;
using OsbizTaxation.Models;
using OsbizTaxation.ViewModels;

namespace OsbizTaxation.Views;

public partial class RechercheWindow : Window
{
    private const string TousLesSites = "(tous les sites)";
    private readonly MainViewModel _main;

    public RechercheWindow(MainViewModel main)
    {
        InitializeComponent();
        _main = main;

        var sites = new List<string> { TousLesSites };
        sites.AddRange(main.Config.Sites.Select(s => s.Nom));
        CmdSite.ItemsSource = sites;
        CmdSite.SelectedIndex = 0;

        GridResultats.ItemsSource = Resultats;
        ExcelFilter.Attach(GridResultats);
    }

    public ObservableCollection<CdrRecord> Resultats { get; } = new();

    private void OnRechercherClick(object sender, RoutedEventArgs e)
    {
        var site = CmdSite.SelectedItem as string;
        if (site == TousLesSites)
            site = null;

        var results = _main.Repository.Search(
            site,
            TxtNumero.Text,
            TxtNomLigne.Text,
            DpDebut.SelectedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DpFin.SelectedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        Resultats.Clear();
        foreach (var record in results)
            Resultats.Add(record);

        TxtResultat.Text = $"{Resultats.Count} resultat(s).";
    }

    private void OnReinitialiserClick(object sender, RoutedEventArgs e)
    {
        CmdSite.SelectedIndex = 0;
        TxtNumero.Text = string.Empty;
        TxtNomLigne.Text = string.Empty;
        DpDebut.SelectedDate = null;
        DpFin.SelectedDate = null;
        Resultats.Clear();
        TxtResultat.Text = "Aucune recherche.";
    }

    private void OnFermerClick(object sender, RoutedEventArgs e) => Close();
}
