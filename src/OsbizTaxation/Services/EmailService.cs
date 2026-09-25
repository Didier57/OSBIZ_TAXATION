using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using OsbizTaxation.Models;

namespace OsbizTaxation.Services;

/// <summary>
/// Envoi d'emails via un serveur SMTP configure.
/// Gere le SSL implicite (port 465) et le STARTTLS (ports 587/25), ce que
/// <see cref="System.Net.Mail.SmtpClient"/> ne sait pas faire pour le port 465.
/// </summary>
public static class EmailService
{
    public static async Task EnvoyerAsync(
        EmailConfig config,
        string destinataire,
        string sujet,
        string corps,
        CancellationToken ct = default)
    {
        AppLog.Write("=== Envoi email : debut ===");

        if (config is null)
            throw Fail("Aucune configuration email.");

        if (string.IsNullOrWhiteSpace(config.Hote))
            throw Fail("Le serveur SMTP (hote) n'est pas configure.");

        if (string.IsNullOrWhiteSpace(config.Expediteur))
            throw Fail("L'adresse de l'expediteur n'est pas configuree.");

        if (string.IsNullOrWhiteSpace(destinataire))
            throw Fail("Aucun destinataire.");

        var port = config.Port is >= 1 and <= 65535 ? config.Port : 587;
        var implicitSsl = port == 465;

        AppLog.Write($"Parametres : hote='{config.Hote}' port={port} ssl={config.UseSsl} " +
                     $"login='{config.Login}' motDePasse={(string.IsNullOrEmpty(config.MotDePasse) ? "vide" : "renseigne")} " +
                     $"mode={(implicitSsl ? "SSL implicite" : config.UseSsl ? "STARTTLS" : "aucun")}");
        AppLog.Write($"Expediteur='{config.Expediteur}' nomAffiche='{config.NomAffiche}' destinataire='{destinataire}'");

        try
        {
            AppLog.Write($"Resolution DNS de '{config.Hote}'...");
            var addresses = await Dns.GetHostAddressesAsync(config.Hote, ct);
            AppLog.Write("DNS OK : " + string.Join(", ", addresses.Select(a => a.ToString())));
        }
        catch (Exception ex)
        {
            AppLog.WriteException($"Echec de resolution DNS de '{config.Hote}'", ex);
            throw;
        }

        try
        {
            await SendAsync(config, port, implicitSsl, destinataire, sujet, corps, ct);
            AppLog.Write("Envoi reussi.");
        }
        catch (Exception ex)
        {
            AppLog.WriteException($"Echec de l'envoi SMTP vers {config.Hote}:{port}", ex);
            throw;
        }
        finally
        {
            AppLog.Write("=== Envoi email : fin ===");
        }
    }

    private static async Task SendAsync(
        EmailConfig config,
        int port,
        bool implicitSsl,
        string destinataire,
        string sujet,
        string corps,
        CancellationToken ct)
    {
        using var tcp = new TcpClient();
        tcp.ReceiveTimeout = 30000;
        tcp.SendTimeout = 30000;

        AppLog.Write($"Connexion TCP a {config.Hote}:{port}...");
        await tcp.ConnectAsync(config.Hote, port, ct);
        AppLog.Write("Connexion TCP etablie.");

        Stream stream = tcp.GetStream();
        if (implicitSsl)
        {
            AppLog.Write("Negociation SSL implicite...");
            stream = await UpgradeToSslAsync(stream, config.Hote, ct);
            AppLog.Write("SSL implicite actif.");
        }

        var reader = new StreamReader(stream, Encoding.ASCII, false, 4096);
        var writer = new StreamWriter(stream, Encoding.ASCII, 4096, false)
        {
            AutoFlush = true,
            NewLine = "\r\n"
        };

        var greeting = await ReadResponseAsync(reader, ct);
        AppLog.Write("Serveur : " + greeting.Summary);
        if (greeting.Code != 220)
            throw new InvalidOperationException($"Accueil SMTP inattendu ({greeting.Code}).");

        var ehlo = await CommandAsync(writer, reader, "EHLO " + LocalHostName(), ct);
        AppLog.Write("EHLO : " + ehlo.FirstLine);

        if (!implicitSsl && config.UseSsl)
        {
            if (!ehlo.Text.Contains("STARTTLS", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Le serveur annonce ne pas supporter STARTTLS.");

            var startTls = await CommandAsync(writer, reader, "STARTTLS", ct);
            if (startTls.Code != 220)
                throw new InvalidOperationException($"STARTTLS refuse ({startTls.Code}).");

            AppLog.Write("Negociation STARTTLS...");
            stream = await UpgradeToSslAsync(stream, config.Hote, ct);
            reader = new StreamReader(stream, Encoding.ASCII, false, 4096);
            writer = new StreamWriter(stream, Encoding.ASCII, 4096, false)
            {
                AutoFlush = true,
                NewLine = "\r\n"
            };

            ehlo = await CommandAsync(writer, reader, "EHLO " + LocalHostName(), ct);
            AppLog.Write("EHLO (apres TLS) : " + ehlo.FirstLine);
        }

        if (!string.IsNullOrWhiteSpace(config.Login))
        {
            await AuthenticateAsync(writer, reader, config, ehlo, ct);
            AppLog.Write("Authentification reussie.");
        }
        else
        {
            AppLog.Write("Authentification : aucune (non demandee).");
        }

        var mailFrom = await CommandAsync(writer, reader, $"MAIL FROM:<{config.Expediteur}>", ct);
        if (mailFrom.Code is not (250 or 251))
            throw new InvalidOperationException($"MAIL FROM refuse ({mailFrom.Code}) : {mailFrom.FirstLine}");

        var rcptTo = await CommandAsync(writer, reader, $"RCPT TO:<{destinataire}>", ct);
        if (rcptTo.Code is not (250 or 251))
            throw new InvalidOperationException($"RCPT TO refuse ({rcptTo.Code}) : {rcptTo.FirstLine}");

        var data = await CommandAsync(writer, reader, "DATA", ct);
        if (data.Code != 354)
            throw new InvalidOperationException($"DATA refuse ({data.Code}) : {data.FirstLine}");

        var body = ComposeMessage(config, destinataire, sujet, corps);
        await writer.WriteAsync(body.AsMemory(), ct);
        await writer.WriteAsync(".\r\n".AsMemory(), ct);

        var afterData = await ReadResponseAsync(reader, ct);
        AppLog.Write("Reponse apres donnees : " + afterData.Summary);
        if (afterData.Code != 250)
            throw new InvalidOperationException($"Envoi refuse ({afterData.Code}) : {afterData.FirstLine}");

        try
        {
            await writer.WriteAsync("QUIT\r\n".AsMemory(), ct);
            await reader.ReadLineAsync(ct);
        }
        catch
        {
            // La deconnexion n'a aucune importance.
        }
    }

    private static async Task AuthenticateAsync(
        StreamWriter writer,
        StreamReader reader,
        EmailConfig config,
        SmtpResponse ehlo,
        CancellationToken ct)
    {
        var mechanisms = ParseAuthMechanisms(ehlo.Text);
        AppLog.Write("Mecanismes AUTH annonces : " + (mechanisms.Count > 0 ? string.Join(", ", mechanisms) : "aucun"));

        if (mechanisms.Count == 0 || mechanisms.Contains("PLAIN"))
        {
            var payload = Convert.ToBase64String(
                Encoding.UTF8.GetBytes("\0" + config.Login + "\0" + config.MotDePasse));
            var auth = await CommandAsync(writer, reader, "AUTH PLAIN " + payload, ct);
            if (auth.Code == 235)
                return;

            if (mechanisms.Count == 0)
                throw new InvalidOperationException($"Authentification refusee ({auth.Code}) : {auth.FirstLine}");
        }

        if (mechanisms.Contains("LOGIN"))
        {
            var step1 = await CommandAsync(writer, reader, "AUTH LOGIN", ct);
            if (step1.Code != 334)
                throw new InvalidOperationException($"AUTH LOGIN refuse ({step1.Code}) : {step1.FirstLine}");

            var step2 = await CommandAsync(writer, reader, Convert.ToBase64String(Encoding.UTF8.GetBytes(config.Login)), ct);
            if (step2.Code != 334)
                throw new InvalidOperationException($"Nom d'utilisateur refuse ({step2.Code}) : {step2.FirstLine}");

            var step3 = await CommandAsync(writer, reader, Convert.ToBase64String(Encoding.UTF8.GetBytes(config.MotDePasse)), ct);
            if (step3.Code != 235)
                throw new InvalidOperationException($"Mot de passe refuse ({step3.Code}) : {step3.FirstLine}");

            return;
        }

        throw new InvalidOperationException("Aucun moyen d'authentification supporte par le serveur.");
    }

    private static List<string> ParseAuthMechanisms(string ehloText)
    {
        var result = new List<string>();
        foreach (var rawLine in ehloText.Split('\n'))
        {
            var line = rawLine.Trim();
            var normalized = line;
            if (normalized.StartsWith("250-", StringComparison.Ordinal))
                normalized = normalized[4..];
            else if (normalized.StartsWith("250 ", StringComparison.Ordinal))
                normalized = normalized[3..];

            if (!normalized.StartsWith("AUTH", StringComparison.OrdinalIgnoreCase))
                continue;

            var rest = normalized[4..].Trim().Replace("=", " ");
            foreach (var token in rest.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                result.Add(token.ToUpperInvariant());
        }

        return result;
    }

    private static string ComposeMessage(EmailConfig config, string destinataire, string sujet, string corps)
    {
        var sb = new StringBuilder();
        sb.Append("From: ").Append(FormatAddress(config.Expediteur, config.NomAffiche)).Append("\r\n");
        sb.Append("To: <").Append(destinataire.Replace("\r", "").Replace("\n", "")).Append(">\r\n");
        sb.Append("Subject: ").Append(EncodeHeader(sujet)).Append("\r\n");
        sb.Append("Date: ").Append(DateTime.Now.ToString("r", CultureInfo.InvariantCulture)).Append("\r\n");
        sb.Append("MIME-Version: 1.0\r\n");
        sb.Append("Content-Type: text/plain; charset=utf-8\r\n");
        sb.Append("Content-Transfer-Encoding: base64\r\n");
        sb.Append("\r\n");
        sb.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(corps), Base64FormattingOptions.InsertLineBreaks));
        sb.Append("\r\n");
        return sb.ToString();
    }

    private static string FormatAddress(string address, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return "<" + address + ">";

        var name = displayName.Replace("\r", "").Replace("\n", "");
        var encoded = EncodeHeader(name);
        return encoded + " <" + address + ">";
    }

    private static string EncodeHeader(string value)
    {
        var safe = value.Replace("\r", "").Replace("\n", "");
        if (safe.All(c => c < 128))
            return safe;

        return "=?UTF-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(safe)) + "?=";
    }

    private static async Task<Stream> UpgradeToSslAsync(Stream stream, string host, CancellationToken ct)
    {
        var ssl = new SslStream(stream, false, (_, _, _, _) => true);
        var options = new SslClientAuthenticationOptions
        {
            TargetHost = host,
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        };
        await ssl.AuthenticateAsClientAsync(options, ct);
        return ssl;
    }

    private static async Task<SmtpResponse> CommandAsync(
        StreamWriter writer,
        StreamReader reader,
        string command,
        CancellationToken ct)
    {
        var label = command.StartsWith("AUTH", StringComparison.OrdinalIgnoreCase)
            ? "AUTH <masque>"
            : command;
        AppLog.Write("C> " + label);
        await writer.WriteAsync((command + "\r\n").AsMemory(), ct);
        var response = await ReadResponseAsync(reader, ct);
        AppLog.Write("S> " + response.Summary);
        return response;
    }

    private static async Task<SmtpResponse> ReadResponseAsync(StreamReader reader, CancellationToken ct)
    {
        var lines = new List<string>();
        while (true)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null)
                throw new IOException("Le serveur SMTP a ferme la connexion.");

            lines.Add(line);
            if (line.Length >= 4 && line[3] == ' ')
                break;
            if (line.Length < 4)
                break;
        }

        var text = string.Join("\n", lines);
        var code = lines.Count > 0 && lines[0].Length >= 3 && int.TryParse(lines[0][..3], out var parsed)
            ? parsed
            : 0;

        return new SmtpResponse(code, text);
    }

    private static string LocalHostName()
    {
        try
        {
            return Dns.GetHostName();
        }
        catch
        {
            return "localhost";
        }
    }

    private static InvalidOperationException Fail(string message)
    {
        AppLog.Write("Echec : " + message);
        return new InvalidOperationException(message);
    }

    private sealed record SmtpResponse(int Code, string Text)
    {
        public string FirstLine => Text.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

        public string Summary
        {
            get
            {
                var lines = Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                return lines.Length <= 1 ? FirstLine : string.Join(" | ", lines);
            }
        }
    }
}
