using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;

    public EmailService(SmtpSettings settings)
    {
        _settings = settings;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
            message.To.Add(to);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = false;

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                EnableSsl = _settings.EnableSsl
            };

            await client.SendMailAsync(message);
        }
        catch (SmtpException ex)
        {
            Console.WriteLine($"Ошибка отправки письма на {to}: {ex.Message}");
        }
    }
}
