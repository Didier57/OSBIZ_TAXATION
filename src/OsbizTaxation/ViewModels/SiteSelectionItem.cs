using OsbizTaxation.Models;

namespace OsbizTaxation.ViewModels;

public sealed class SiteSelectionItem : ObservableObject
{
    private bool _isSelected = true;

    public SiteSelectionItem(SiteConfig site) => Site = site;

    public SiteConfig Site { get; }

    public string Nom => Site.Nom;

    public string Adresse => Site.Adresse;

    public string Pays => Services.Countries.NameFor(Site.PaysCode);

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
