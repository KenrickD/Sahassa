
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace WMS.Application.Helpers
{
    public class WhatsappHelper
    {
        // Define classes to match the JSON structure
        public class Subscription
        {
            public DateTime Expiry { get; set; }
            public DateTime PaymentDate { get; set; }
        }

        public class Channel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Status { get; set; }
            public Subscription Subscription { get; set; }
        }

        public class Result
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public string Company { get; set; }
            public string Phone { get; set; }
            public string Status { get; set; }
            public List<Channel> Channels { get; set; }
        }

        public class JsonResponse
        {
            public bool Success { get; set; }
            public int Code { get; set; }
            public Result Result { get; set; }
        }

        //private static readonly ILogger<WhatsAppHelper> _logger;

        public async static Task<bool> GetContactPhoneAsync(string accountId, string secretKey, string phoneNumber)
        {
            try
            {
                string apiUrl = "https://wacontact.readyspace.com/v1.0/contacts/phone/{phone}";
                //string phone = "+6590024528"; // Soo Shin

                // Current time in milliseconds since Unix Epoch
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Construct the token
                string token = $"{accountId}:{secretKey}:{currentTime}";
                string base64Token = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));

                // Setup HttpClient
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", base64Token);

                    // Make the GET request
                    HttpResponseMessage response = await client.GetAsync($"{apiUrl.Replace("{phone}", phoneNumber)}?accountId={accountId}&time={currentTime}");

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Response: " + responseBody);
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Error: " + response.StatusCode);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                //_logger.LogError("GetContactPhoneAsync's error" + ex.Message);
                throw new Exception(ex.Message);
            }
        }

        public async static Task<bool> SendWATextMessage(string accountId, string secretKey, string toPhoneNumber, string message)
        {
            try
            {
                //_logger.LogInformation($"Sending WhatsApp notification to phone: {toPhoneNumber} with message: {message}");
                //api end points
                string apiUrlAccounts = "https://wacontact.readyspace.com/accounts";
                string apiUrlSendmsg = "https://wacontact.readyspace.com/whatsapp/send-text";
                //string phone = "+6590024528"; // Soo Shin

                // Current time in milliseconds since Unix Epoch
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Construct the token
                string token = $"{accountId}:{secretKey}:{currentTime}";
                string base64Token = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));

                // Setup HttpClient
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", base64Token);
                    //get channel id
                    HttpResponseMessage response = await client.GetAsync($"{apiUrlAccounts}?accountId={accountId}&time={currentTime}");
                    int channelId = 0;

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        // Deserialize JSON string to object
                        JsonResponse responseObject = JsonConvert.DeserializeObject<JsonResponse>(responseBody)!;
                        Console.WriteLine("Response: " + responseBody);
                        channelId = responseObject.Result.Channels.FirstOrDefault()!.Id;
                    }
                    else
                    {
                        Console.WriteLine("Error: " + response.StatusCode);
                    }

                    // Current time in Unix time milliseconds
                    long sendCurrentTime = DateTimeOffset.UtcNow.AddSeconds(15).ToUnixTimeMilliseconds();
                    string constructAPIUrl = $"{apiUrlSendmsg}?accountId={accountId}&time={currentTime}&to={toPhoneNumber}&message={message}&sendTime={sendCurrentTime}&channelId={channelId}";
                    HttpResponseMessage response3 = await client.PostAsync(constructAPIUrl, null);

                    if (response3.IsSuccessStatusCode)
                    {
                        string responseBody = await response3.Content.ReadAsStringAsync();
                        Console.WriteLine("Response: " + responseBody);
                        //_logger.LogInformation($"Sending WhatsApp notification to phone: {toPhoneNumber} with message: {message} are sent.");

                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Error: " + response3.StatusCode);
                        //_logger.LogInformation($"Sending WhatsApp notification to phone: {toPhoneNumber} with message: {message} are failed.");

                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
                //_logger.LogError("SendWATextMessage's error" + ex.Message);
            }
        }
    }
}
