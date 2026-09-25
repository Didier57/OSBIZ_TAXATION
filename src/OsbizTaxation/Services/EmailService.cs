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
        if (config is null)
            throw new InvalidOperationException("Aucune configuration email.");

        if (string.IsNullOrWhiteSpace(config.Hote))
            throw new InvalidOperationException("Le serveur SMTP (hote) n'est pas configure.");

        if (string.IsNullOrWhiteSpace(config.Expediteur))
            throw new InvalidOperationException("L'adresse de l'expediteur n'est pas configuree.");

        if (string.IsNullOrWhiteSpace(destinataire))
            throw new InvalidOperationException("Aucun destinataire.");

        using var message = new MailMessage();
        message.From = string.IsNullOrWhiteSpace(config.NomAffiche)
            ? new MailAddress(config.Expediteur)
            : new MailAddress(config.Expediteur, config.NomAffiche);
        message.To.Add(destinataire);
        message.Subject = sujet;
        message.Body = corps;
        message.IsBodyHtml = false;

        var port = config.Port is >= 1 and <= 65535 ? config.Port : 587;

        using var client = new SmtpClient(config.Hote, port)
        {
            EnableSsl = config.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(config.Login))
            client.Credentials = new NetworkCredential(config.Login, config.MotDePasse);
        else
            client.UseDefaultCredentials = false;

        await client.SendMailAsync(message, ct);
    }
}
