
using System.Net;
using System.Text;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Utils;

public class CameraAlertService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _senderEmail;
    private readonly string _recipientEmail;
    private readonly string _provider;

    public CameraAlertService()
    {
        _provider = EmailSettings.Provider;
        _smtpHost = EmailSettings.SmtpHost;
        _smtpPort = EmailSettings.SmtpPort;
        _senderEmail = EmailSettings.SenderEmail;
        _recipientEmail = EmailSettings.RecipientEmail;

        _ = MailboxAddress.Parse(_senderEmail);
        _ = MailboxAddress.Parse(_recipientEmail);
    }

    public async Task SendMotionBatchAsync(
        IReadOnlyList<MotionCapture> captures,
        CancellationToken cancellationToken = default)
    {
        if (captures.Count == 0)
            return;

        string templatePath = Path.Combine(
            AppContext.BaseDirectory,
            "Templates",
            "alert-template.html");

        string html = await File.ReadAllTextAsync(
            templatePath,
            cancellationToken);

        var bodyBuilder = new BodyBuilder();
        var imageBlocks = new StringBuilder();
        var textBody = new StringBuilder();

        textBody.AppendLine("Bewegung erkannt!");
        textBody.AppendLine(
            $"Anzahl Aufnahmen: {captures.Count}");
        textBody.AppendLine();

        // Embed every image into the email
        foreach (var capture in captures)
        {
            if (!File.Exists(capture.ImagePath))
            {
                throw new FileNotFoundException(
                    "Kamerabild nicht gefunden.",
                    capture.ImagePath);
            }

            var image = bodyBuilder.LinkedResources.Add(
                capture.ImagePath);

            image.ContentId = MimeUtils.GenerateMessageId();

            string cameraName =
                WebUtility.HtmlEncode(capture.CameraName);

            string cameraId =
                WebUtility.HtmlEncode(capture.CameraId);

            string timestamp =
                capture.Timestamp.ToString(
                    "dd.MM.yyyy HH:mm:ss");

            imageBlocks.AppendLine("<div style=\"margin-bottom:24px;\">");
            imageBlocks.AppendLine(
                $"<p><strong>Kamera:</strong> {cameraName}</p>");
            imageBlocks.AppendLine(
                $"<p><strong>Kamera-ID:</strong> {cameraId}</p>");
            imageBlocks.AppendLine(
                $"<p><strong>Uhrzeit:</strong> {timestamp}</p>");
            imageBlocks.AppendLine(
                $"<img src=\"cid:{image.ContentId}\" " +
                "alt=\"Kameraaufnahme\" " +
                "style=\"max-width:100%;height:auto;\">");
            imageBlocks.AppendLine("</div><hr>");

            textBody.AppendLine(
                $"Kamera: {capture.CameraName}");
            textBody.AppendLine(
                $"Kamera-ID: {capture.CameraId}");
            textBody.AppendLine(
                $"Uhrzeit: {timestamp}");
            textBody.AppendLine();
        }

        // Insert all collected images
        html = html
            .Replace(
                "{{ImageCount}}",
                captures.Count.ToString())
            .Replace(
                "{{ImagesHtml}}",
                imageBlocks.ToString());

        bodyBuilder.HtmlBody = html;
        bodyBuilder.TextBody = textBody.ToString();

        // Create email
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(
            "Kameraüberwachung",
            _senderEmail));

        message.To.Add(
            MailboxAddress.Parse(_recipientEmail));

        message.Subject =
            $"[ALARM] Bewegung erkannt - {captures.Count} Bilder";

        message.Body = bodyBuilder.ToMessageBody();

        // Connect to SMTP server
        using var client = new SmtpClient();

        await client.ConnectAsync(
            _smtpHost,
            _smtpPort,
            SecureSocketOptions.StartTls,
            cancellationToken);

        switch (_provider)
        {
            case "Gmail":
            {
                string password =
                    Environment.GetEnvironmentVariable(
                        "CAMERA_SMTP_PASSWORD")
                    ?? throw new InvalidOperationException(
                        "Gmail-App-Passwort fehlt.");

                await client.AuthenticateAsync(
                    _senderEmail,
                    password,
                    cancellationToken);

                break;
            }

            case "Outlook":
            {
                string accessToken =
                    Environment.GetEnvironmentVariable(
                        "CAMERA_OUTLOOK_ACCESS_TOKEN")
                    ?? throw new InvalidOperationException(
                        "Outlook-OAuth2-Token fehlt.");

                var oauth2 = new SaslMechanismOAuth2(
                    _senderEmail,
                    accessToken);

                await client.AuthenticateAsync(
                    oauth2,
                    cancellationToken);

                break;
            }

            default:
                throw new InvalidOperationException(
                    "Unbekannter E-Mail-Anbieter.");
        }

        // Send email containing all images
        await client.SendAsync(
            message,
            cancellationToken);

        await client.DisconnectAsync(
            true,
            cancellationToken);
    }
}
