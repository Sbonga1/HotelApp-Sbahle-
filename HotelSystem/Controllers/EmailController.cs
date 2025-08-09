

using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Mvc;

namespace HotelSystem.Controllers
{
    public class EmailController : Controller
    {
        private readonly string smtpHost = WebConfigurationManager.AppSettings["BrevoSmtpHost"];
        private readonly int smtpPort = Convert.ToInt32(WebConfigurationManager.AppSettings["BrevoSmtpPort"]);
        private readonly string smtpUsername = WebConfigurationManager.AppSettings["BrevoSmtpUsername"];
        private readonly string smtpPassword = WebConfigurationManager.AppSettings["BrevoSmtpPassword"];
        private readonly string senderEmail = WebConfigurationManager.AppSettings["BrevoSenderEmail"];
        private readonly string senderName = "Hotel";

        public async Task SendEmailWithInlineImageAsync(string toEmail, string subject, string bodyHtml, string[] attachments, string imagePath, string contentId)
        {
            var message = new MailMessage
            {
                Subject = subject,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);
            message.From = new MailAddress(senderEmail, senderName); // Ensure From is set

            var inlineImage = new LinkedResource(imagePath)
            {
                ContentId = contentId,
                ContentType = new ContentType("image/png"),
                TransferEncoding = TransferEncoding.Base64
            };

            var htmlView = AlternateView.CreateAlternateViewFromString(bodyHtml, null, "text/html");
            htmlView.LinkedResources.Add(inlineImage);
            message.AlternateViews.Add(htmlView);

            if (attachments != null)
            {
                foreach (var file in attachments)
                {
                    if (System.IO.File.Exists(file))
                        message.Attachments.Add(new Attachment(file));
                }
            }

            using (var smtp = new SmtpClient(smtpHost, smtpPort))
            {
                smtp.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtp.EnableSsl = true;

                await smtp.SendMailAsync(message);
            }
        }

        public async Task SendEmailAsync(string recipientEmail, string subject, string body, string[] attachmentPaths = null)
        {
            try
            {
                using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, senderName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                    };

                    mailMessage.To.Add(recipientEmail);

                    if (attachmentPaths != null)
                    {
                        foreach (var filePath in attachmentPaths)
                        {
                            if (System.IO.File.Exists(filePath))
                            {
                                mailMessage.Attachments.Add(new Attachment(filePath));
                            }
                        }
                    }
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.TargetName = "STARTTLS/smtp.yourdomain.com"; // Optional
                    smtpClient.Timeout = 10000;

                    try
                    {
                        await smtpClient.SendMailAsync(mailMessage);
                        Console.WriteLine("✅ SMTP accepted the email.");
                    }
                    catch (SmtpException smtpEx)
                    {
                        Console.WriteLine($"❌ SMTP error: {smtpEx.StatusCode} - {smtpEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ General error: {ex.Message}");
                    }

                    Console.WriteLine("Email sent successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }
    }
}
