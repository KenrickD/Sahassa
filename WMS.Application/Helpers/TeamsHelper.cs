using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using static WMS.Application.Helpers.TeamsHelper.Webhook;

namespace WMS.Application.Helpers
{
    public class TeamsHelper
    {
        public TeamsHelper()
        {
        }
        public static async Task PostMessage(string Webhook_URL, string sharePointURL)
        {
            Webhook APIConnect = new Webhook();


            APIConnect.type = "MessageCard";
            APIConnect.context = "http://schema.org/extensions";
            APIConnect.themeColor = "0076D7";
            APIConnect.summary = "Error Log";

            CntrInfoResuls1 DocInfo = new CntrInfoResuls1();


            DocInfo.activityTitle = "Error Log on Warehouse Management System Web";
            DocInfo.activitySubtitle = "Subtitle";
            DocInfo.activityText = string.Format("Error text");

            APIConnect.sections.Add(DocInfo);

            CntrInfoResuls2 DocInfo1 = new CntrInfoResuls2();

            DocInfo1.name = "Error Log File";

            DocInfo1.value = sharePointURL;

            DocInfo.facts.Add(DocInfo1);

            using (var client = new HttpClient())
            {
                //var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes("kokbeng@hsc.sg:protectMe19"));
                //client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                client.BaseAddress = new Uri(Webhook_URL);

                ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xc0 | 0x300 | 0xc00);


                var json = JsonConvert.SerializeObject(APIConnect);

                var responseTask = await client.PostAsync(client.BaseAddress, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                var jsonString = await responseTask.Content.ReadAsStringAsync();


                if (Convert.ToInt32(responseTask.StatusCode) == 200) //request is success
                {

                    //success

                }
                else
                {
                    //Log("Webhook error. The status code is " + Convert.ToInt32(result.StatusCode).ToString());
                }
            }
        }
        public static async Task PostMessageToTeams(string webhookUrl, bool isError, string functionClassName, string summary, string activityTitle, string message, ILogger _logger)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    _logger.LogInformation("Post message to teams start here.");

                    _logger.LogInformation("Initialize object and convert into json.");

                    client.BaseAddress = new Uri(webhookUrl);
                    Webhook APIConnect = new Webhook();
                    APIConnect.type = "MessageCard";
                    APIConnect.context = "http://schema.org/extensions";
                    APIConnect.themeColor = "0076D7";
                    //APIConnect.summary = "Error Notification Database Synchronization";
                    APIConnect.summary = summary;

                    CntrInfoResuls1 DocInfo = new CntrInfoResuls1();


                    //DocInfo.activityTitle = "Error Alert: Database Synchronization";
                    DocInfo.activityTitle = activityTitle;
                    if (isError)
                    {
                        DocInfo.activitySubtitle = string.Format("Error on {0}", functionClassName);
                        DocInfo.activityText = string.Format("Error message: {0} ", message);
                    }
                    else
                    {
                        DocInfo.activityText = string.Format("Information: {0} ", message);
                    }


                    APIConnect.sections.Add(DocInfo);

                    //CntrInfoResuls2 DocInfo1 = new CntrInfoResuls2();
                    //DocInfo1.name = "Error Log File";
                    //DocInfo1.value = strSharePoint + strSharePointEDI + "//" + sFilePathAttName;
                    //DocInfo.facts.Add(DocInfo1);

                    var json = JsonConvert.SerializeObject(APIConnect);

                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    _logger.LogInformation("Posting message to Teams..");
                    var response = await client.PostAsync(client.BaseAddress, content);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Message posted successfully to Teams..");
                        //Console.WriteLine("Message posted successfully to Teams.");
                    }
                    else
                    {
                        _logger.LogInformation($"Failed to post message to Teams. Status code: {response.StatusCode}");
                        //Console.WriteLine($"Failed to post message to Teams. Status code: {response.StatusCode}");
                    }
                    _logger.LogInformation("Post message to teams end here.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while posting message to Teams: {ex.Message}");
            }
        }
        public class Webhook
        {

            public string? type { get; set; }
            public string? context { get; set; }
            public string? themeColor { get; set; }
            public string? summary { get; set; }

            public List<CntrInfoResuls1> sections = new List<CntrInfoResuls1>();


            public class CntrInfoResuls1
            {

                public string? activityTitle { get; set; }

                public string? activitySubtitle { get; set; }

                public string? activityText { get; set; }

                public List<CntrInfoResuls2> facts = new List<CntrInfoResuls2>();
            }
            public class CntrInfoResuls2
            {
                public string? name { get; set; }
                public string? value { get; set; }
            }

        }
    }
}
