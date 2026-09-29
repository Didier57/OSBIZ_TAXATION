# OSBIZ TAXATION

OSBIZ TAXATION est une application Windows qui **centralise et analyse les appels téléphoniques** d'un ou plusieurs autocommutateurs (centrales) OpenScape Business.

Elle récupère automatiquement les journaux d'appels (CDR) de chaque site, les enregistre dans une base locale, puis offre une large gamme d'outils pour les consulter, les analyser, les rapporter et les partager — en local comme sur le réseau.

---

## Récupération et stockage des appels

- Récupération des journaux d'appels (CDR) sur chaque site configuré, en **mode manuel** ou **automatique** (intervalle configurable).
- Prise en charge de **plusieurs sites** (centrales) simultanément.
- Conservation du fichier brut d'origine dans un dossier **par site**, et archivage par site.
- Suppression optionnelle des données sur la centrale après transfert.
- Import de fichiers bruts existants pour reconstituer l'historique.
- **Journal des transferts** : déroulement détaillé site par site, **erreurs explicites** (connexion impossible, réponse vide, réponse inattendue) et récapitulatif en fin de transfert.
- **État du dernier transfert par site** affiché sur l'écran d'accueil (réussite ou échec).
- **Alerte e-mail en cas d'échec** de récupération, envoyée une seule fois par jour tant que le problème persiste.
- **Regroupement des appels** : les lignes d'un même appel vues sur plusieurs postes ou lignes de la centrale (transferts, suites de conversation) sont rattachées entre elles, colorées et comptées comme un seul appel — y compris lorsque les heures de début diffèrent de quelques secondes.

## Gestion des sites et des lignes

- Configuration de chaque site : nom, adresse (IP / domaine), identifiants, pays.
- Déclaration des **plages de numéros internes**, avec **nom de ligne** et **type de ligne** (Externe ou Interne).
- Renommage automatique des lignes sur les appels déjà enregistrés.
- Détection des **nouveaux numéros** rencontrés lors d'un transfert.
- Suppression d'un site : l'application propose de supprimer en même temps **ses appels** (base active et archives) et **ses utilisateurs internes**.

## Utilisateurs internes

- Répertoire des **postes internes** (numéro, site, nom, rôle, code PIN).
- Gestion de l'**historique d'affectation** : un poste peut changer de nom dans le temps ; chaque appel est associé au nom en vigueur **à la date de l'appel**.
- Import automatique des numéros internes déjà présents dans les appels, avec date de début renseignée automatiquement.
- Import des **noms des postes depuis le fichier d'export du central** (fichier XML), pour un site choisi : les postes manquants sont créés, les noms existants mis à jour.
- Bouton **« Supprimer sites inexistants »** pour nettoyer les postes dont le site n'existe plus.
- Rôles **Utilisateur** / **Gestionnaire** / **Administrateur**, avec **périmètre par site** (un gestionnaire ne voit que les sites qui lui sont attribués).
- Sélection multiple dans la liste pour supprimer plusieurs postes d'un coup.

## Consultation des appels

- **Écran d'accueil** : liste des appels récents, état du dernier transfert par site, avec largeurs et disposition des colonnes mémorisés.
- **Recherche** : consultation par période (jour / mois / année), par site, avec déplacement avant/arrière, filtres façon Excel sur chaque colonne, tri, export Excel et bouton de **réinitialisation** des filtres et du tri.
- Les filtres sont automatiquement retirés lorsqu'on change de **période, de date ou de site**.
- **Affichage progressif** avec message de patience pendant la recherche et l'affichage (périodes volumineuses : aucun plafond sur le nombre d'appels).
- Détail de chaque appel : site, date, heures de début et de fin, ligne et nom de ligne, numéro interne et nom de l'utilisateur, **durée de sonnerie**, durée d'appel, numéro externe, **numéro extra**, type d'appel, taxes, compte, groupe, pays.
- **Type d'appel détaillé** : entrant / sortant / routé / conférence / déviation, avec les précisions transmises par la centrale (établissement, suite, transit, DISA) et les enregistrements d'information d'appel.

## Statistiques et graphiques

- **Non décrochés** : onglet dédié aux appels sans réponse — non décrochés, total d'appels, entrants / sortants non répondus, **taux de non-réponse** et sonnerie moyenne, répartition **par heure** et **par jour de la semaine**, carte de chaleur heures × jours, **Top 5 des postes qui ne décrochent pas**, **Top 5 des numéros appelants** sans réponse, et liste détaillée des appels par un simple clic.
- Nombre d'appels **jour / mois / année** (graphique empilé entrants / sortants) avec totaux.
- Nombre d'appels **par heure**.
- **Heures de pointe** : carte de chaleur heures × jours de la semaine, répartition par jour et **taux de réponse**.
- **Top durées** d'appel.
- Répartition **par pays**.
- **Tableau croisé dynamique** (TCD) exportable.
- **Graphiques interactifs** : un clic sur une barre, une part ou une case affiche la liste des appels correspondants.
- Impression de chaque analyse en **PDF** avec site, période et totaux.

## Rapports

- Création de **rapports personnalisés** sur de multiples critères (sites, numéros internes, numéro externe, noms de ligne, pays, comptes, noms d'utilisateurs, types d'appel, durée, période ou plage de dates).
- Contenu au choix : **détail des appels + totaux**, avec **choix des colonnes à afficher**.
- **Classement et séparation par N° Interne** (option) : titre et **sous-total par poste**, avec **saut de page par poste** dans le PDF.
- **Planification** : exécution toutes les heures / jours / semaines / mois / ans.
- **Envoi automatique par e-mail** (plusieurs destinataires + copies), au choix : rapport **en pièce jointe** ou **directement dans le corps de l'e-mail** (tableau HTML), ou envoi **immédiat** depuis le formulaire.
- **Modèles d'e-mails personnalisables** par rapport : objet et texte du message, avec variables (`{Nom}`, `{Periode}`, `{Nombre}`, `{Date}`, `{Site}`).
- Export au format **PDF** ou **Excel**.

## Templates d'en-tête personnalisés

- Création de plusieurs modèles d'en-tête : **logo de société, nom, adresse, e-mail, téléphone**.
- Réutilisation d'un modèle sur les rapports PDF et Excel pour un rendu personnalisé à l'en-tête de l'entreprise.

## Import / Export

- Import de fichiers bruts.
- Export des résultats de recherche vers **Excel**.
- Génération des rapports en **PDF** ou **Excel**.

## Service Windows (traitement en arrière-plan)

- Installation du traitement comme **service Windows** : récupération des appels, planification et envoi des rapports, serveur web — sans nécessiter l'ouverture de l'application.
- Pilotage depuis l'application : ajout, démarrage, arrêt et suppression du service.
- Intervalle de récupération configurable.

## Serveur web intégré

- Accès aux appels depuis un navigateur, sur le réseau local.
- **Connexion par numéro interne et code PIN**. Chaque utilisateur ne voit que ses propres appels ; les **gestionnaires** voient les appels des sites qui leur sont attribués ; les **administrateurs** voient tous les appels.
- Affichage du mois en cours par défaut, avec filtres par colonne, tri, recherche globale et export CSV.
- Possibilité pour chaque utilisateur de **changer son code PIN**.
- **HTTPS** disponible avec un **certificat auto-signé** (validité 10 ans, régénérable depuis l'application).

## Journal d'audit

- Enregistrement des **connexions** (réussies et refusées), **déconnexions** et **changements de code PIN** sur le serveur web.
- Enregistrement des **modifications faites dans l'application** : configuration, sites, utilisateurs internes, rapports, templates, exécutions.
- Consultation avec filtres par source, action, plage de dates et recherche libre, export Excel et purge par ancienneté.

## Sauvegarde et restauration

- **Sauvegarde automatique** de la base de données à intervalle configurable, en local et éventuellement vers un **dossier partagé (réseau)**.
- La base d'**archive est sauvegardée et restaurée** en même temps que la base principale.
- Nombre de sauvegardes conservées configurable (rotation automatique).
- Sauvegarde **manuelle** et **restauration** d'une sauvegarde depuis l'application.

## Archivage des appels

- Déplacement automatique des appels anciens (au-delà d'un nombre de mois configurable) vers une **base d'archive**.
- La base d'archive reste **entièrement consultable** dans les statistiques, la recherche et les rapports.
- Archivage **manuel** également disponible.

## Maintenance

- Purge automatique des journaux techniques de plus de 31 jours.

## Mises à jour

- Vérification des mises à jour depuis les GitHub Releases, au démarrage ou manuellement.
- Installation de la mise à jour et redémarrage automatique (en tenant compte du service Windows).
