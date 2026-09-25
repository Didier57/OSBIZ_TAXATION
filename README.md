# OSBIZ TAXATION

Application Windows (WPF, .NET 8) distribuee sous forme d'un **executable unique sans installation**.

## Fonctionnalites

- Recuperation de donnees via **HTTPS**, en mode **manuel** ou **automatique** (intervalle configurable).
- Ecriture du resultat dans un **fichier texte**.
- **Verification et installation des mises a jour** depuis les GitHub Releases du depot.
- Aucun droit administrateur requis.

## Prerequis (developpement)

- .NET SDK 8.0
- Windows 10/11

## Build et publication

```powershell
dotnet publish src/OsbizTaxation/OsbizTaxation.csproj -c Release
```

Le resultat est un fichier unique `OsbizTaxation.exe` dans
`src/OsbizTaxation/bin/Release/net8.0-windows/win-x64/publish/`.

## Configuration

La configuration est stockee dans :

```
%APPDATA%\OsbizTaxation\config.json
```

Parametres : URL source, fichier de sortie, recuperation automatique, intervalle (minutes),
verification des mises a jour au demarrage.

## Mises a jour

L'application interroge `https://api.github.com/repos/Didier57/OSBIZ_TAXATION/releases/latest`.
Pour publier une nouvelle version :

1. Incrementer `<Version>` dans `OsbizTaxation.csproj`.
2. Creer une GitHub Release avec un tag de version (ex. `1.0.1`).
3. Joindre l'executable `OsbizTaxation.exe` en tant qu'asset de la release.

## Structure

```
OsbizTaxation.sln
src/OsbizTaxation/
  Models/        Modeles de donnees
  Services/      Recuperation, ecriture, configuration, mises a jour
  ViewModels/    Logique de presentation (MVVM)
  MainWindow.xaml
```
