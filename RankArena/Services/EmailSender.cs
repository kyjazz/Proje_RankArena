using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace RankArena.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly SmtpSettings _smtp;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IOptions<SmtpSettings> smtpOptions, ILogger<EmailSender> logger)
        {
            _smtp = smtpOptions.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var mail = new MailMessage
                {
                    From = new MailAddress(_smtp.FromEmail, _smtp.FromName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mail.To.Add(email);

                using var client = new SmtpClient(_smtp.Host, _smtp.Port)
                {
                    Credentials = new NetworkCredential(_smtp.UserName, _smtp.Password),
                    EnableSsl = _smtp.EnableSsl
                };

                await client.SendMailAsync(mail);
                _logger.LogInformation("[EmailSender] Mail gönderildi → {Email} | Konu: {Subject}", email, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailSender] Mail gönderilemedi → {Email} | Konu: {Subject}", email, subject);
                throw;
            }
        }
    }
}