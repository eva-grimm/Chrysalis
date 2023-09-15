using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using Chrysalis.Models;

namespace Chrysalis.Services
{
    public class EmailService : IEmailSender
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                // get email sender settings
                var emailAddress = _emailSettings.EmailAddress
                    ?? Environment.GetEnvironmentVariable("EmailAddress");
                var emailPassword = _emailSettings.EmailPassword
                    ?? Environment.GetEnvironmentVariable("EmailPassword");
                var emailHost = _emailSettings.EmailHost
                    ?? Environment.GetEnvironmentVariable("EmailHost");
                var emailPort = _emailSettings.EmailPort != 0
                    ? _emailSettings.EmailPort
                    : int.Parse(Environment.GetEnvironmentVariable("EmailPort")!);

                // set email body
                BodyBuilder emailBody = new()
                {
                    HtmlBody = htmlMessage
                };

                MimeMessage newEmail = new()
                {
                    Sender = MailboxAddress.Parse(emailAddress),
                    Subject = subject,
                    Body = emailBody.ToMessageBody(),
                };

                foreach (string address in email.Split(";"))
                {
                    //newEmail.To.Add(MailboxAddress.Parse(address));
                    newEmail.Bcc.Add(MailboxAddress.Parse(address));
                }

                try
                {
                    using SmtpClient smtpClient = new();
                    await smtpClient.ConnectAsync(emailHost, emailPort, SecureSocketOptions.StartTls);
                    await smtpClient.AuthenticateAsync(emailAddress, emailPassword);
                    await smtpClient.SendAsync(newEmail);
                    await smtpClient.DisconnectAsync(true);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}