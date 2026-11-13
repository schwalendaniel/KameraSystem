
public static class EmailSettings
{
    // Email provider
    public static string Provider = "Gmail";

    // Sender and recipient
    public static string SenderEmail =
        "marek.morsch@gmail.com";

    public static string RecipientEmail =
        "marek.morsch@gmail.com";

    // SMTP server
    public static string SmtpHost => Provider switch
    {
        "Gmail" => "smtp.gmail.com",
        "Outlook" => "smtp-mail.outlook.com",
        _ => throw new Exception("Unbekannter E-Mail-Anbieter")
    };

    // SMTP port
    public static int SmtpPort => 587;
}
