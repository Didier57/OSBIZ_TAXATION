using Microsoft.Data.Sqlite;
using OsbizTaxation.Models;

namespace OsbizTaxation.Services;

/// <summary>Stockage local des CDR dans une base SQLite (fichier cdr.db).</summary>
public sealed class CdrRepository : IDisposable
{
    private readonly SqliteConnection _connection;

    public CdrRepository()
    {
        AppPaths.EnsureCreated();
        _connection = new SqliteConnection($"Data Source={AppPaths.DatabasePath}");
        _connection.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Cdr (
    Id                 INTEGER PRIMARY KEY AUTOINCREMENT,
    Site               TEXT,
    Date               TEXT,
    DateIso            TEXT,
    HeureDebut         TEXT,
    HeureFin           TEXT,
    Ligne              TEXT,
    NomLigne           TEXT,
    NumeroInterne      TEXT,
    DureeSonnerie      TEXT,
    DureeAppel         TEXT,
    NumeroExterne      TEXT,
    Information        TEXT,
    InfoCode           INTEGER,
    NumeroExtra        TEXT,
    DureeAppelSecondes INTEGER,
    RawLine            TEXT,
    SourceFile         TEXT,
    DateTransfert      TEXT
);
CREATE INDEX IF NOT EXISTS IX_Cdr_Date ON Cdr(DateIso);
CREATE INDEX IF NOT EXISTS IX_Cdr_Site ON Cdr(Site);
CREATE INDEX IF NOT EXISTS IX_Cdr_NumeroExterne ON Cdr(NumeroExterne);";
        cmd.ExecuteNonQuery();
    }

    public int Insert(IEnumerable<CdrRecord> records)
    {
        var list = records as ICollection<CdrRecord> ?? records.ToList();
        if (list.Count == 0)
            return 0;

        using var tx = _connection.BeginTransaction();
        using var cmd = _connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
INSERT INTO Cdr (Site, Date, DateIso, HeureDebut, HeureFin, Ligne, NomLigne, NumeroInterne,
                 DureeSonnerie, DureeAppel, NumeroExterne, Information, InfoCode, NumeroExtra,
                 DureeAppelSecondes, RawLine, SourceFile, DateTransfert)
VALUES ($site, $date, $dateIso, $debut, $fin, $ligne, $nomLigne, $interne,
        $sonnerie, $duree, $externe, $info, $infoCode, $extra,
        $secondes, $raw, $source, $transfert);";

        var pSite = cmd.Parameters.Add("$site", SqliteType.Text);
        var pDate = cmd.Parameters.Add("$date", SqliteType.Text);
        var pDateIso = cmd.Parameters.Add("$dateIso", SqliteType.Text);
        var pDebut = cmd.Parameters.Add("$debut", SqliteType.Text);
        var pFin = cmd.Parameters.Add("$fin", SqliteType.Text);
        var pLigne = cmd.Parameters.Add("$ligne", SqliteType.Text);
        var pNomLigne = cmd.Parameters.Add("$nomLigne", SqliteType.Text);
        var pInterne = cmd.Parameters.Add("$interne", SqliteType.Text);
        var pSonnerie = cmd.Parameters.Add("$sonnerie", SqliteType.Text);
        var pDuree = cmd.Parameters.Add("$duree", SqliteType.Text);
        var pExterne = cmd.Parameters.Add("$externe", SqliteType.Text);
        var pInfo = cmd.Parameters.Add("$info", SqliteType.Text);
        var pInfoCode = cmd.Parameters.Add("$infoCode", SqliteType.Integer);
        var pExtra = cmd.Parameters.Add("$extra", SqliteType.Text);
        var pSecondes = cmd.Parameters.Add("$secondes", SqliteType.Integer);
        var pRaw = cmd.Parameters.Add("$raw", SqliteType.Text);
        var pSource = cmd.Parameters.Add("$source", SqliteType.Text);
        var pTransfert = cmd.Parameters.Add("$transfert", SqliteType.Text);

        var count = 0;
        foreach (var r in list)
        {
            pSite.Value = r.Site;
            pDate.Value = r.Date;
            pDateIso.Value = r.DateIso;
            pDebut.Value = r.HeureDebut;
            pFin.Value = r.HeureFin;
            pLigne.Value = r.Ligne;
            pNomLigne.Value = r.NomLigne;
            pInterne.Value = r.NumeroInterne;
            pSonnerie.Value = r.DureeSonnerie;
            pDuree.Value = r.DureeAppel;
            pExterne.Value = r.NumeroExterne;
            pInfo.Value = r.Information;
            pInfoCode.Value = r.InfoCode;
            pExtra.Value = r.NumeroExtra;
            pSecondes.Value = r.DureeAppelSecondes;
            pRaw.Value = r.RawLine;
            pSource.Value = r.SourceFile;
            pTransfert.Value = r.DateTransfert;
            count += cmd.ExecuteNonQuery();
        }

        tx.Commit();
        return count;
    }

    public List<CdrRecord> GetAll()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = SelectColumns + " ORDER BY DateIso DESC, HeureFin DESC, Id DESC;";
        return Read(cmd);
    }

    public List<CdrRecord> Search(
        string? site,
        string? numero,
        string? nomLigne,
        string? dateIsoDebut,
        string? dateIsoFin,
        int limit = 5000)
    {
        using var cmd = _connection.CreateCommand();
        var where = new List<string>();

        if (!string.IsNullOrWhiteSpace(site))
        {
            where.Add("Site = $site");
            cmd.Parameters.AddWithValue("$site", site.Trim());
        }

        if (!string.IsNullOrWhiteSpace(numero))
        {
            where.Add("(NumeroExterne LIKE $numero OR NumeroInterne LIKE $numero OR NumeroExtra LIKE $numero)");
            cmd.Parameters.AddWithValue("$numero", "%" + numero.Trim() + "%");
        }

        if (!string.IsNullOrWhiteSpace(nomLigne))
        {
            where.Add("NomLigne LIKE $nomLigne");
            cmd.Parameters.AddWithValue("$nomLigne", "%" + nomLigne.Trim() + "%");
        }

        if (!string.IsNullOrWhiteSpace(dateIsoDebut))
        {
            where.Add("DateIso >= $debut");
            cmd.Parameters.AddWithValue("$debut", dateIsoDebut.Trim());
        }

        if (!string.IsNullOrWhiteSpace(dateIsoFin))
        {
            where.Add("DateIso <= $fin");
            cmd.Parameters.AddWithValue("$fin", dateIsoFin.Trim());
        }

        var clause = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : string.Empty;
        cmd.CommandText = SelectColumns + clause + " ORDER BY DateIso DESC, HeureFin DESC, Id DESC LIMIT " + limit + ";";
        return Read(cmd);
    }

    public int ReassignLineNames(IReadOnlyList<LineConfig> lignes)
    {
        using var tx = _connection.BeginTransaction();

        using (var reset = _connection.CreateCommand())
        {
            reset.Transaction = tx;
            reset.CommandText = "UPDATE Cdr SET NomLigne = '';";
            reset.ExecuteNonQuery();
        }

        var total = 0;
        foreach (var ligne in lignes)
        {
            using var cmd = _connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
UPDATE Cdr SET NomLigne = $nom
WHERE Site = $site
  AND CAST(Ligne AS INTEGER) BETWEEN $debut AND $fin;";
            cmd.Parameters.AddWithValue("$nom", ligne.NomLigne);
            cmd.Parameters.AddWithValue("$site", ligne.Site);
            cmd.Parameters.AddWithValue("$debut", ligne.NumDebut);
            cmd.Parameters.AddWithValue("$fin", ligne.NumFin);
            total += cmd.ExecuteNonQuery();
        }

        tx.Commit();
        return total;
    }

    public int Count()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Cdr;";
        return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
    }

    public void DeleteAll()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Cdr;";
        cmd.ExecuteNonQuery();
    }

    private const string SelectColumns = @"
SELECT Id, Site, Date, DateIso, HeureDebut, HeureFin, Ligne, NomLigne, NumeroInterne,
       DureeSonnerie, DureeAppel, NumeroExterne, Information, InfoCode, NumeroExtra,
       DureeAppelSecondes, RawLine, SourceFile, DateTransfert
FROM Cdr";

    private static List<CdrRecord> Read(SqliteCommand cmd)
    {
        var list = new List<CdrRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new CdrRecord
            {
                Id = reader.GetInt64(0),
                Site = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Date = reader.IsDBNull(2) ? "" : reader.GetString(2),
                DateIso = reader.IsDBNull(3) ? "" : reader.GetString(3),
                HeureDebut = reader.IsDBNull(4) ? "" : reader.GetString(4),
                HeureFin = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Ligne = reader.IsDBNull(6) ? "" : reader.GetString(6),
                NomLigne = reader.IsDBNull(7) ? "" : reader.GetString(7),
                NumeroInterne = reader.IsDBNull(8) ? "" : reader.GetString(8),
                DureeSonnerie = reader.IsDBNull(9) ? "" : reader.GetString(9),
                DureeAppel = reader.IsDBNull(10) ? "" : reader.GetString(10),
                NumeroExterne = reader.IsDBNull(11) ? "" : reader.GetString(11),
                Information = reader.IsDBNull(12) ? "" : reader.GetString(12),
                InfoCode = reader.IsDBNull(13) ? 0 : reader.GetInt32(13),
                NumeroExtra = reader.IsDBNull(14) ? "" : reader.GetString(14),
                DureeAppelSecondes = reader.IsDBNull(15) ? 0 : reader.GetInt32(15),
                RawLine = reader.IsDBNull(16) ? "" : reader.GetString(16),
                SourceFile = reader.IsDBNull(17) ? "" : reader.GetString(17),
                DateTransfert = reader.IsDBNull(18) ? "" : reader.GetString(18)
            });
        }

        return list;
    }

    public void Dispose() => _connection.Dispose();
}
