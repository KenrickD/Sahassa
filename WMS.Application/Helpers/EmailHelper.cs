using System.Net.Mail;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace WMS.Application.Helpers
{

    public class EmailHelper
    {
        //private static readonly ILogger<EmailHelper> _logger;
        private static readonly string _strIVREmail = "IVR@hsc.sg";
        private static readonly string _stro365AppClientID = "10caa0bf-28f1-4e3c-a794-2fedfb979bfe";
        private static readonly string _stro365TenantID = "a0d8d371-f62f-4416-824b-4a668a6d573d";
        private static readonly string _stro365ClientSecret = "LHn8Q~e5lAhdMW2sk19kqQA4.4fmi9a2cCqpucLM";

        public async static System.Threading.Tasks.Task SendEmailAsync(List<string> recipient, string subject, string body, List<string> ccRecipient = null)
        {
            //ILog emailLogger = LogManager.GetLogger("Email");
            //string strIVREmail = "IVR@hsc.sg";
            //string stro365AppClientID = "10caa0bf-28f1-4e3c-a794-2fedfb979bfe";
            //string stro365TenantID = "a0d8d371-f62f-4416-824b-4a668a6d573d";
            //string stro365ClientSecret = "LHn8Q~e5lAhdMW2sk19kqQA4.4fmi9a2cCqpucLM";

            // Using Microsoft.Identity.Client 4.22.0
            var cca = ConfidentialClientApplicationBuilder
                .Create(_stro365AppClientID)
                .WithClientSecret(_stro365ClientSecret)
                .WithTenantId(_stro365TenantID)
                .Build();

            // The permission scope required for EWS access
            var ewsScopes = new string[] { "https://outlook.office365.com/.default" };

            //Make the token request
            var authResult = await cca.AcquireTokenForClient(ewsScopes).ExecuteAsync();

            ExchangeService emailService = new ExchangeService();
            emailService.Url = new Uri("https://outlook.office365.com/EWS/Exchange.asmx");

            emailService.Credentials = new OAuthCredentials(authResult.AccessToken);

            emailService.ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, _strIVREmail);

            try
            {
                EmailMessage emailMessage = new EmailMessage(emailService);
                //subject
                emailMessage.Subject = subject;
                //body
                //emailMessage.Body = new MessageBody(body);
                emailMessage.Body = EmailBody(body);
                //recipient, in list
                emailMessage.ToRecipients.AddRange(recipient);

                if (ccRecipient != null)
                {
                    emailMessage.CcRecipients.AddRange(ccRecipient);
                }

                emailMessage.Send();
            }
            catch (SmtpException smtpEx)
            {
                //emailLogger.Error("sendEmai's error" + smtpEx.Message);
                //_logger.LogError("SendEmail's error" + smtpEx.Message);
            }
        }
        public async static System.Threading.Tasks.Task SendEmailWithAttachmentAsync(List<string> recipient, string subject, string body, string attachmentName, byte[] attachmentBytes, List<string> ccRecipient = null, string? senderEmail = null, string? customSenderInfo = null)
        {
            //ILog emailLogger = LogManager.GetLogger("Email");
            //string strIVREmail = "IVR@hsc.sg";
            //string stro365AppClientID = "10caa0bf-28f1-4e3c-a794-2fedfb979bfe";
            //string stro365TenantID = "a0d8d371-f62f-4416-824b-4a668a6d573d";
            //string stro365ClientSecret = "LHn8Q~e5lAhdMW2sk19kqQA4.4fmi9a2cCqpucLM";

            // Using Microsoft.Identity.Client 4.22.0
            var cca = ConfidentialClientApplicationBuilder
                .Create(_stro365AppClientID)
                .WithClientSecret(_stro365ClientSecret)
                .WithTenantId(_stro365TenantID)
                .Build();

            // The permission scope required for EWS access
            var ewsScopes = new string[] { "https://outlook.office365.com/.default" };

            //Make the token request
            var authResult = await cca.AcquireTokenForClient(ewsScopes).ExecuteAsync();

            ExchangeService emailService = new ExchangeService();
            emailService.Url = new Uri("https://outlook.office365.com/EWS/Exchange.asmx");

            emailService.Credentials = new OAuthCredentials(authResult.AccessToken);

            if (!String.IsNullOrEmpty(senderEmail))
                emailService.ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, senderEmail);
            else
                emailService.ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, _strIVREmail);

            try
            {
                EmailMessage emailMessage = new EmailMessage(emailService);
                //subject
                emailMessage.Subject = subject;
                //body
                emailMessage.Body = EmailBody(body, customSenderInfo);
                //recipient, in list
                emailMessage.ToRecipients.AddRange(recipient);

                if (ccRecipient != null)
                {
                    emailMessage.CcRecipients.AddRange(ccRecipient);
                }
                emailMessage.Attachments.AddFileAttachment(attachmentName, attachmentBytes);
                emailMessage.Send();
            }
            catch (SmtpException smtpEx)
            {
                //emailLogger.Error("sendEmai's error" + smtpEx.Message);
                //_logger.LogError("SendEmail include attachment's error" + smtpEx.Message);
            }
        }
        public async static System.Threading.Tasks.Task SendEmailWithAttachmentsAsync(List<string> recipient, string subject, string body, Dictionary<string, byte[]> attachmentDicts, List<string> ccRecipient = null)
        {
            //ILog emailLogger = LogManager.GetLogger("Email");
            //string strIVREmail = "IVR@hsc.sg";
            //string stro365AppClientID = "10caa0bf-28f1-4e3c-a794-2fedfb979bfe";
            //string stro365TenantID = "a0d8d371-f62f-4416-824b-4a668a6d573d";
            //string stro365ClientSecret = "LHn8Q~e5lAhdMW2sk19kqQA4.4fmi9a2cCqpucLM";

            // Using Microsoft.Identity.Client 4.22.0
            var cca = ConfidentialClientApplicationBuilder
                .Create(_stro365AppClientID)
                .WithClientSecret(_stro365ClientSecret)
                .WithTenantId(_stro365TenantID)
                .Build();
            // The permission scope required for EWS access
            var ewsScopes = new string[] { "https://outlook.office365.com/.default" };

            //Make the token request
            var authResult = await cca.AcquireTokenForClient(ewsScopes).ExecuteAsync();

            ExchangeService emailService = new ExchangeService();
            emailService.Url = new Uri("https://outlook.office365.com/EWS/Exchange.asmx");

            emailService.Credentials = new OAuthCredentials(authResult.AccessToken);

            emailService.ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, _strIVREmail);

            try
            {
                EmailMessage emailMessage = new EmailMessage(emailService);
                //subject
                emailMessage.Subject = subject;
                //body
                emailMessage.Body = EmailBody(body);
                //recipient, in list
                emailMessage.ToRecipients.AddRange(recipient);

                if (ccRecipient != null)
                {
                    emailMessage.CcRecipients.AddRange(ccRecipient);
                }
                foreach (var dict in attachmentDicts)
                {
                    emailMessage.Attachments.AddFileAttachment(dict.Key, dict.Value);
                }
                emailMessage.Send();
            }
            catch (SmtpException smtpEx)
            {
                //emailLogger.Error("sendEmai's error" + smtpEx.Message);
                //_logger.LogError("SendEmail include attachment's error" + smtpEx.Message);
            }
        }
        public async static Task<string> MoveEmailToFolderAsync(string targetFolderName, string emailId, ILogger logger)
        {
            string errMsg = string.Empty;
            try
            {
                // Using Microsoft.Identity.Client 4.22.0
                var cca = ConfidentialClientApplicationBuilder
                    .Create(_stro365AppClientID)
                    .WithClientSecret(_stro365ClientSecret)
                    .WithTenantId(_stro365TenantID)
                    .Build();
                var ewsScopes = new string[] { "https://outlook.office365.com/.default" };
                var authResult = await cca.AcquireTokenForClient(ewsScopes).ExecuteAsync();

                ExchangeService emailService = new ExchangeService();
                emailService.Url = new Uri("https://outlook.office365.com/EWS/Exchange.asmx");
                emailService.Credentials = new OAuthCredentials(authResult.AccessToken);
                emailService.ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, _strIVREmail);

                // Load the email by its ID
                EmailMessage email = EmailMessage.Bind(emailService, new ItemId(emailId));

                // Find the target folder by name
                FolderView folderView = new FolderView(1);
                SearchFilter.IsEqualTo folderFilter = new SearchFilter.IsEqualTo(FolderSchema.DisplayName, targetFolderName);
                FindFoldersResults findFolderResults = emailService.FindFolders(WellKnownFolderName.Inbox, folderFilter, folderView);

                if (findFolderResults.Folders.Count == 0)
                {
                    errMsg = $"The folder '{targetFolderName}' was not found.";
                    logger.LogError(errMsg);

                    return errMsg;
                }

                Folder targetFolder = findFolderResults.Folders[0];

                // Move the email to the target folder
                email.Move(targetFolder.Id);

                logger.LogInformation($"Email with ID '{emailId}' moved to folder '{targetFolderName}'.");

                return errMsg;
            }
            catch (SmtpException smtpEx)
            {
                errMsg = $"ReadMailAndDownloadAttachment include attachment's error :{smtpEx.Message}";

                logger.LogError(errMsg);

                return errMsg;
            }
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                // This will throw format exception for invalid email formats
                MailAddress mailAddress = new MailAddress(email);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
        private static string EmailBody(string mainContent, string? customSenderInfo = null)
        {
            var senderInfo = @"<p class='content'>
					            Best Regards,<br>
					            Hup Soon Cheong App Support Team
					        </p>
					        <br>
					        <p class='focus'>
					            This is an auto-generated email; please do not reply.
					        </p> ";
            if (customSenderInfo != null) { senderInfo = customSenderInfo; }
            string htmlBody = @"<html>
					<head>
					    <title>Email</title>
					    <style>
					        body {
					            font-family: Arial, sans-serif;
					            background-color: #f9f9f9;
					        }
					        .container {
					            max-width: 1000px;
					            margin: 0 auto;
					            padding: 20px;
					            background-color: #ffffff;
					            border-radius: 5px;
					            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
					        }
					        .focus {
					            font-size: 20px;
					            margin-bottom: 20px;
								font-weight:bold;
								text-align: center;
								color:#2C3E50;
					        }
					        .content {
					            font-size: 14px;
					            margin-bottom: 20px;
					        }
					        .footer {
					            font-size: 12px;
					            color: #8AA2B9;
					        }
					    </style>
					</head>
					<body>
					    <div class='container'>
							<p class='content'>
					           " + mainContent + @"
					        </p>
							<br><br>
					        " + senderInfo + @"
                            <br>
					        <p class='footer'>
					            Disclaimer Statement<br><br>
					            All the information provided in this email is provided on an 'as is' and 'as available' basis and you agree that you use such information entirely at your own risk. 
								<br><br>
								Hup Soon Cheong (HSC) gives no warranty and accepts no responsibility or liability for the accuracy or the completeness of the information and materials contained in this email. 
								<br><br>
								Under no circumstances will HSC be held responsible or liable in any way for any claims, damages, losses, expenses, costs, or liabilities whatsoever (including, without limitation, any direct or indirect damages for loss of profits, business interruption, or loss of information) resulting or 
								arising directly or indirectly from your use of or inability to use information linked to it, or from your reliance on the information and material in this email, even if HSC has been advised of the possibility of such damages in advance.
					        </p>
					    </div>
					</body>
					</html>";

            return htmlBody;
        }

    }

}
