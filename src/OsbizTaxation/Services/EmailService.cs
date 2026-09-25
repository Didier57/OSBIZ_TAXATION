using System.Net;
using System.Net.Mail;
using OsbizTaxation.Models;

namespace OsbizTaxation.Services;

/// <summary>Envoi d'emails via un serveur SMTP configure.</summary>
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

        AppLog.Write($"Parametres : hote='{config.Hote}' port={port} ssl={config.UseSsl} " +
                     $"login='{config.Login}' motDePasse={(string.IsNullOrEmpty(config.MotDePasse) ? "vide" : "renseigne")}");
        AppLog.Write($"Expediteur='{config.Expediteur}' nomAffiche='{config.NomAffiche}' destinataire='{destinataire}'");

        if (port == 465)
            AppLog.Write("Remarque : le port 465 utilise SSL implicite, non gere par SmtpClient. " +
                         "Preferer le port 587 (STARTTLS) ou 25.");

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

        using var message = new MailMessage();
        message.From = string.IsNullOrWhiteSpace(config.NomAffiche)
            ? new MailAddress(config.Expediteur)
            : new MailAddress(config.Expediteur, config.NomAffiche);
        message.To.Add(destinataire);
        message.Subject = sujet;
        message.Body = corps;
        message.IsBodyHtml = false;

        using var client = new SmtpClient(config.Hote, port)
        {
            EnableSsl = config.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000
        };

        if (!string.IsNullOrWhiteSpace(config.Login))
        {
            client.Credentials = new NetworkCredential(config.Login, config.MotDePasse);
            AppLog.Write("Authentification : identifiants fournis.");
        }
        else
        {
            client.UseDefaultCredentials = false;
            AppLog.Write("Authentification : aucune (UseDefaultCredentials=false).");
        }

        try
        {
            AppLog.Write($"Connexion SMTP a {config.Hote}:{port} (ssl={config.UseSsl}) et envoi...");
            await client.SendMailAsync(message, ct);
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

    private static InvalidOperationException Fail(string message)
    {
        AppLog.Write("Echec : " + message);
        return new InvalidOperationException(message);
    }
}
