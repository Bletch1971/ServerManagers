using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace ServerManagerTool.Common.Utils
{
    public class EmailUtil
    {
        public EmailUtil()
        {
            Credentials = null;
            DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
            EnableSsl = false;
            MailServer = string.Empty;
            Port = 25;
            Timeout = 100000;
            UseDefaultCredentials = false;
        }


        public ICredentialsByHost Credentials
        {
            get;
            set;
        }

        public DeliveryNotificationOptions DeliveryNotificationOptions
        {
            get;
            set;
        }

        public bool EnableSsl
        {
            get;
            set;
        }

        public string MailServer
        {
            get;
            set;
        }

        public int Port
        {
            get;
            set;
        }

        public int Timeout
        {
            get;
            set;
        }

        public bool UseDefaultCredentials
        {
            get;
            set;
        }


        public void SendEmail(string fromAddress, string toAddress, string subject, string body, bool isBodyHtml)
        {
            SendEmailImplementation(fromAddress, new[] { toAddress }, subject, body, isBodyHtml, null);
        }

        public void SendEmail(string fromAddress, string toAddress, string subject, string body, bool isBodyHtml, Attachment[] mailAttachments)
        {
            SendEmailImplementation(fromAddress, new[] { toAddress }, subject, body, isBodyHtml, mailAttachments);
        }

        public void SendEmail(string fromAddress, string[] toAddresses, string subject, string body, bool isBodyHtml)
        {
            SendEmailImplementation(fromAddress, toAddresses, subject, body, isBodyHtml, null);
        }

        public void SendEmail(string fromAddress, string[] toAddresses, string subject, string body, bool isBodyHtml, Attachment[] mailAttachments)
        {
            SendEmailImplementation(fromAddress, toAddresses, subject, body, isBodyHtml, mailAttachments);
        }

        private void SendEmailImplementation(string fromAddress, IEnumerable<string> toAddresses, string subject, string body, bool isBodyHtml, IEnumerable<Attachment> mailAttachments)
        {
            // Format mail message
            using (var mailMessage = new MailMessage())
            {
                mailMessage.From = new MailAddress(fromAddress);
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = isBodyHtml;
                mailMessage.DeliveryNotificationOptions = DeliveryNotificationOptions;

                if (mailAttachments != null)
                {
                    foreach (var mailAttachment in mailAttachments)
                    {
                        if (mailAttachment == null)
                            continue;
                        mailMessage.Attachments.Add(mailAttachment);
                    }
                }

                mailMessage.To.Clear();
                foreach (var toAddress in toAddresses)
                {
                    mailMessage.To.Add(new MailAddress(toAddress));
                }

                if (mailMessage.To.Count > 0)
                {
                    using (var smptClient = new SmtpClient())
                    {
                        smptClient.Host = MailServer;
                        smptClient.Port = Port;
                        smptClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smptClient.EnableSsl = EnableSsl;
                        smptClient.Timeout = Timeout;
                        smptClient.UseDefaultCredentials = UseDefaultCredentials;
                        smptClient.Credentials = Credentials;

                        try
                        {
                            smptClient.Send(mailMessage);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("An error occurred trying to send an email.", ex);
                        }
                    }
                }
            }
        }
    }
}
