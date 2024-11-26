using Common.Communication.Model;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Common.Communication
{
    public class MailHelper
    {
        private readonly MailConfig _mailConfig;

        public MailHelper(MailConfig mailConfig)
        {
            _mailConfig = mailConfig;
        }

        public async Task SendMessageAsync(MimeMessage message)
        {
            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                await client.ConnectAsync(_mailConfig.Host, _mailConfig.Port, _mailConfig.UseSsl);

                if (_mailConfig.IsAuthenticationRequired)
                    await client.AuthenticateAsync(_mailConfig.UserId, _mailConfig.Password);

                await client.SendAsync(message);

                await client.DisconnectAsync(true);
            }
        }

        public void SendMessageAsync(string toEmailId, string subject, string body) 
        {
            var imgList = new Dictionary<string, string>();

            string regexImgSrc = @"<img[^>]*?src\s*=\s*[""']data:image?([^'"" >]+?)[ '""][^>]*?>";
            MatchCollection matchesImgSrc = Regex.Matches(body, regexImgSrc, RegexOptions.IgnoreCase | RegexOptions.Singleline);


            foreach (Match m in matchesImgSrc)
            {
                var contentId = Guid.NewGuid().ToString();
                var sIndex = m.ToString().IndexOf("data:image");
                var eIndex = m.ToString().IndexOf("\" style", sIndex);

                if (eIndex == -1)
                    eIndex = m.ToString().IndexOf("' style", sIndex);

                var imgData = m.ToString().Substring(sIndex, eIndex - sIndex);

                body = body.Replace($"'{imgData}'", $"cid:{contentId}");
                body = body.Replace($"\"{imgData}\"", $"cid:{contentId}");

                imgList.Add(contentId, imgData);
            }

            AlternateView htmlView = AlternateView.CreateAlternateViewFromString(body, Encoding.UTF8, MediaTypeNames.Text.Html);
            //AlternateView plainView = AlternateView.CreateAlternateViewFromString(Regex.Replace(body, "<[^>]+?>", string.Empty), Encoding.UTF8, MediaTypeNames.Text.Plain);

            foreach (var image in imgList)
            {
                var sIndex = image.Value.IndexOf("base64,") + 7;
                var base64Image = image.Value.Substring(sIndex);

                LinkedResource linkedImg = new LinkedResource(GetImageContentStream(base64Image));

                linkedImg.ContentId = image.Key;
                linkedImg.ContentType.MediaType = MediaTypeNames.Image.Jpeg;
                linkedImg.TransferEncoding = TransferEncoding.Base64;
                linkedImg.ContentType.Name = linkedImg.ContentId;
                linkedImg.ContentLink = new Uri("cid:" + linkedImg.ContentId);

                htmlView.LinkedResources.Add(linkedImg);
            }

            MailAddress to = new MailAddress(toEmailId, toEmailId);
            var from = new MailAddress(_mailConfig.SenderMailAddress, _mailConfig.SenderName);

            MailMessage objMailMsg = new MailMessage(from, to)
            {
                BodyEncoding = Encoding.UTF8,
                Subject = subject,
                Body = body,
                Priority = MailPriority.Normal,
                IsBodyHtml = true
            };

            //objMailMsg.AlternateViews.Add(plainView);
            objMailMsg.AlternateViews.Add(htmlView);

            var objSMTPClient = new System.Net.Mail.SmtpClient
            {
                Host = _mailConfig.Host,
                Port = _mailConfig.Port,
                EnableSsl = _mailConfig.UseSsl
            };

            if (_mailConfig.IsAuthenticationRequired == true)
                objSMTPClient.Credentials = new NetworkCredential(_mailConfig.UserId, _mailConfig.Password);

            objSMTPClient.Send(objMailMsg);

        }

        //public async Task SendMessagesAsync(IEnumerable<MimeMessage> messages)
        //{
        //    using (var client = new DSNSmtpClient())
        //    {
        //        await client.ConnectAsync(_mailConfig.Host, _mailConfig.Port, _mailConfig.UseSsl);

        //        if (_mailConfig.IsAuthenticationRequired)
        //            await client.AuthenticateAsync(_mailConfig.UserId, _mailConfig.Password);

        //        Parallel.ForEach(messages, async (message) => {

        //            message.MessageId = MimeUtils.GenerateMessageId();

        //            await client.SendAsync(message);
        //        });

        //        await client.DisconnectAsync(true);
        //    }
        //}

        private static Stream GetImageContentStream(string base64EncodedString) 
        {
            var bytes = Convert.FromBase64String(base64EncodedString);
            return new MemoryStream(bytes);
        }
    }
}