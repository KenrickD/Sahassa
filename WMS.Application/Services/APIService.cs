using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using WMS.Application.Helpers;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.API;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.SignalR;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace WMS.Application.Services
{
    public class APIService : IAPIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly IConfiguration _configuration;
        private readonly JWTHelper _jwtHelper;
        private readonly ILogger<APIService> _logger;
        private string? _token;

        public APIService(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, IConfiguration configuration, JWTHelper jwtHelper, ILogger<APIService> logger)
        {
            this._httpClientFactory = httpClientFactory;
            this._memoryCache = memoryCache;
            this._configuration = configuration;
            this._jwtHelper = jwtHelper;
            this._logger = logger;
        }
        #region Internal web app API call
        public async Task NotifyLocationUpdateAsync(Guid zoneId, string locationCode)
        {
            try
            {
                // Generate service JWT token
                var token = _jwtHelper.GenerateServiceToken(
                    serviceName: "WMS.WebAPI",
                    permissions: new[] { "location-update", "notifications" }
                );

                _logger.LogDebug("Generated JWT token for notification service");
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var webAppUrl = _configuration["APIInfo:URLWebsiteApplication"];
                var endpoint = _configuration["APIInfo:EndpointNotifyLocationGrid"];
                var urlWithEndPoint = $"{webAppUrl}/{endpoint}";

                var update = new LocationUpdateAPIRequestDto();
                update.LocationCodes.Add(locationCode);
                update.ZoneId = zoneId;

                _logger.LogInformation("Sending location update notification to: {urlWithEndPoint}, Location: {LocationCode}",
                    urlWithEndPoint, locationCode);

                //var response = await client.PostAsJsonAsync(urlWithEndPoint, update);
                // In your NotifyLocationUpdateAsync method, before PostAsJsonAsync:
                var json = System.Text.Json.JsonSerializer.Serialize(update);
                _logger.LogDebug("Sending JSON: {Json}", json);

                var response = await client.PostAsJsonAsync(urlWithEndPoint, update);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Notification sent successfully: {Response}", responseContent);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Notification failed with status {StatusCode}: {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send location update notification for: {LocationCode}", locationCode);
                // Don't throw - notification failure shouldn't break main operation
            }
        }
        public async Task NotifyBulkLocationUpdateAsync(Guid zoneId, List<string> locationCodes)
        {
            try
            {
                // Generate service JWT token
                var token = _jwtHelper.GenerateServiceToken(
                    serviceName: "WMS.WebAPI",
                    permissions: new[] { "location-update", "notifications" }
                );

                _logger.LogDebug("Generated JWT token for notification service");
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var webAppUrl = _configuration["APIInfo:URLWebsiteApplication"];
                var endpoint = _configuration["APIInfo:EndpointNotifyBulkLocationGrid"];
                var urlWithEndPoint = $"{webAppUrl}/{endpoint}";

                var update = new LocationUpdateAPIRequestDto();
                update.LocationCodes.AddRange(locationCodes);
                update.ZoneId = zoneId;

                _logger.LogInformation("Sending location update notification to: {UrlWithEndPoint}, LocationCodes: {LocationCodes}",
                    urlWithEndPoint, string.Join(",", update.LocationCodes ?? Enumerable.Empty<string>()));

                //var response = await client.PostAsJsonAsync(urlWithEndPoint, update);
                // In your NotifyLocationUpdateAsync method, before PostAsJsonAsync:
                var json = System.Text.Json.JsonSerializer.Serialize(update);
                _logger.LogDebug("Sending JSON: {Json}", json);

                var response = await client.PostAsJsonAsync(urlWithEndPoint, update);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Notification sent successfully: {Response}", responseContent);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Notification failed with status {StatusCode}: {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send location update notification for: {LocationCode}", string.Join(",", locationCodes ?? Enumerable.Empty<string>()));
                // Don't throw - notification failure shouldn't break main operation
            }
        }
        #endregion
        #region On premise API Call
        public async Task<string> GetLoginTokenAsync(CancellationToken cancellationToken)
        {
            try
            {
                string? username = _configuration["APIInfo:GetTokenUsername"];
                string? password = _configuration["APIInfo:GetTokenPassword"];
                string? apiURL = _configuration["APIInfo:URLGetToken"];
                string? apiEndPoint = _configuration["APIInfo:EndPointGetToken"];

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                var client = new HttpClient(handler);

                var loginUrl = apiURL + apiEndPoint;
                var loginData = new
                {
                    Username = username,
                    Password = password
                };

                // Log request details
                Console.WriteLine("Request Headers:");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                Console.WriteLine($"Request URI: {loginUrl}");

                var response = await client.PostAsJsonAsync(loginUrl, loginData, cancellationToken);

                // Log response details
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.WriteLine("Response:");
                Console.WriteLine(responseContent);
                LoginResponse responseObj = JsonConvert.DeserializeObject<LoginResponse>(responseContent)!;

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error: {response.StatusCode}, {response.ReasonPhrase}, {responseContent}");
                }

                _token = responseObj.Data;
                return _token!;
            }
            catch (Exception ex)
            {
                // Log exception details
                Console.WriteLine($"Exception: {ex.Message}");
                throw;
            }
        }
        public async Task<string> GetPortListAsync(CancellationToken cancellationToken)
        {
            string? apiURL = _configuration["APIInfo:URLGetGeneralData"];
            string? apiEndPoint = _configuration["APIInfo:EndPointGetPortList"];

            if (string.IsNullOrEmpty(_token))
            {
                throw new InvalidOperationException("Token is not available. Please login first.");
            }

            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Add("x-access-token", _token);

            //var portListUrl = apiURL + "api/v1/generalInfo/getPortList";
            var portListUrl = apiURL + apiEndPoint;
            var response = await client.GetAsync(portListUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            //PortResponse responseObj = JsonConvert.DeserializeObject<PortResponse>(responseBody)!;

            return responseBody;
        }
        public async Task<string> GetVesselInfoListAsync(CancellationToken cancellationToken)
        {
            string? apiURL = _configuration["APIInfo:URLGetGeneralData"];
            string? apiEndPoint = _configuration["APIInfo:EndPointGetVesselInfoList"];

            if (string.IsNullOrEmpty(_token))
            {
                throw new InvalidOperationException("Token is not available. Please login first.");
            }

            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Add("x-access-token", _token);

            //var portListUrl = apiURL + "api/v1/generalInfo/getVesselInfoList";
            var portListUrl = apiURL + apiEndPoint;
            var response = await client.GetAsync(portListUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            //VesselInfoResponse responseObj = JsonConvert.DeserializeObject<VesselInfoResponse>(responseBody)!;

            return responseBody;
        }
        public async Task<string> GetAddressListAsync(CancellationToken cancellationToken)
        {
            string? apiURL = _configuration["APIInfo:URLGetGeneralData"];
            string? apiEndPoint = _configuration["APIInfo:EndPointGetAddressList"];

            if (string.IsNullOrEmpty(_token))
            {
                throw new InvalidOperationException("Token is not available. Please login first.");
            }

            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Add("x-access-token", _token);

            //var portListUrl = apiURL + "api/v1/generalInfo/getAddressList";
            var portListUrl = apiURL + apiEndPoint;
            var response = await client.GetAsync(portListUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            //AddressResponse responseObj = JsonConvert.DeserializeObject<AddressResponse>(responseBody)!;

            return responseBody;
        }
        public async Task<string> GetVesselInfoByIDsAsync(List<int> vesselIDList, CancellationToken cancellationToken)
        {
            string? apiURL = _configuration["APIInfo:URLGetGeneralData"];
            string? apiEndPoint = _configuration["APIInfo:EndPointGetVesselInfoListByIds"];

            if (string.IsNullOrEmpty(_token))
            {
                throw new InvalidOperationException("Token is not available. Please login first.");
            }

            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Add("x-access-token", _token);

            //var portListUrl = apiURL + "api/v1/generalInfo/getVesselInfoListByIds";
            var portListUrl = apiURL + apiEndPoint;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var data = new
            {
                VesselIDs = vesselIDList,
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, portListUrl)
            {
                Content = requestContent
            };
            var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return responseBody;
        }
        #endregion
        public async Task<string> GetAddressByPostalCode(string postalCode, CancellationToken cancellationToken)
        {
            var searchUrl = $"https://www.onemap.gov.sg/api/common/elastic/search?searchVal={postalCode}&returnGeom=Y&getAddrDetails=Y";
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync(searchUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            //AddressSearchResultResponse responseObj = JsonConvert.DeserializeObject<AddressSearchResultResponse>(responseBody)!;

            return responseBody;
        }
        #region Portnet API Call
        public async Task<string> GetVesselInfoPornetAsync(string type, string vesselName, string inVoy, string outVoy, CancellationToken cancellationToken)
        {
            string? portnetApiHost = _configuration["APIInfo:PortnetURL"];
            string? portnetApiKey = _configuration["APIInfo:PortnetAPIKey"];
            string? portnetEPBerthing = _configuration["APIInfo:PortnetEndPointBerthing"];
            string? portnetEPOperation = _configuration["APIInfo:PortnetEndPointOperation"];

            //var portnetApiHost = "https://api.portnet.com";
            //var portnetApiKey = "924fe41250084cecba39f488cfd625ef";
            var url = $"{portnetApiHost}{portnetEPBerthing}";
            var urlOperation = $"{portnetApiHost}{portnetEPOperation}";

            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Add("Apikey", portnetApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var data = new
            {
                vslM = vesselName,
                inVoyN = inVoy,
                outVoyN = outVoy
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, type == "Berthing" ? url : urlOperation)
            {
                Content = requestContent
            };
            var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return responseBody;
        }

        #endregion
        #region GIVAUDAN API call
        public async Task<string?> GetBearerTokenFromEmailPasswordAsync(string email, string password, string authUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var authPayload = new
                {
                    email,
                    password
                };

                var content = new StringContent(JsonConvert.SerializeObject(authPayload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(authUrl, content, cancellationToken);
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Bearer token request failed. Status: {StatusCode}, Content: {Content}", response.StatusCode, responseString);
                    return null;
                }

                var result = JsonConvert.DeserializeObject<BearerTokenResponseDto>(responseString);
                return result?.Token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Bearer token retrieval");
                return null;
            }
        }

        #endregion
        private static string GenerateJwtToken(string filename, string secret)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[] { new Claim("filename", filename) };

            var tokenDescriptor = new JwtSecurityToken(
            claims: claims,
                expires: DateTime.UtcNow.AddHours(1), // Set token expiration time
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
        private byte[] ConvertToByteArray(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream); // Copies the file content into the memory stream
                return memoryStream.ToArray(); // Converts the memory stream content into a byte array
            }
        }

        public void SaveGeneralDataInMemory(AddressResponse addressResponse, PortResponse portResponse, VesselInfoResponse vesselInfoResponse)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60), // Cache for 60 minutes
            };

            _memoryCache.Set(AppConsts.MemoryCacheKey.ADDRESS_LOOKUP, addressResponse.Data, cacheEntryOptions);
            _memoryCache.Set(AppConsts.MemoryCacheKey.PORT_LOOKUP, portResponse.Data, cacheEntryOptions);
            _memoryCache.Set(AppConsts.MemoryCacheKey.VESSEL_INFO, vesselInfoResponse.Data, cacheEntryOptions);
        }
        public List<AddressDto> GetAddressList()
        {
            if (_memoryCache.TryGetValue(AppConsts.MemoryCacheKey.ADDRESS_LOOKUP, out List<AddressDto>? addressList))
            {
                return addressList!;
            }

            return new List<AddressDto>();
        }
        public List<PortDto> GetPortList()
        {
            if (_memoryCache.TryGetValue(AppConsts.MemoryCacheKey.PORT_LOOKUP, out List<PortDto>? portList))
            {
                return portList!;
            }

            return new List<PortDto>();
        }
        public List<VesselInfoDto> GetVesselInfoList()
        {
            if (_memoryCache.TryGetValue(AppConsts.MemoryCacheKey.VESSEL_INFO, out List<VesselInfoDto>? vesselInfoList))
            {
                return vesselInfoList!;
            }

            return new List<VesselInfoDto>();
        }
        public void RemoveGeneralDataInMemory()
        {
            _memoryCache.Remove(AppConsts.MemoryCacheKey.ADDRESS_LOOKUP);
            _memoryCache.Remove(AppConsts.MemoryCacheKey.PORT_LOOKUP);
            _memoryCache.Remove(AppConsts.MemoryCacheKey.VESSEL_INFO);
        }
        public class LoginResponse
        {
            public bool Error { get; set; }
            public string? Message { get; set; }
            public string? Data { get; set; }
            public int Code { get; set; }
        }
        public class AddressResponse
        {
            public bool Error { get; set; }
            public string? Message { get; set; }
            public List<AddressDto>? Data { get; set; }
            public int Code { get; set; }
        }

        public class Address
        {
            public int AddressID { get; set; }
            public string? PostalCode { get; set; }
            public int StreetNo { get; set; }
            public string? StreetName { get; set; }
            public string? PlaceName { get; set; }
            public string? LevelNo { get; set; }
            public string? UnitNo { get; set; }
            public string? BlockNo { get; set; }
            public string? BuildingName { get; set; }
            public string? TelephoneNo { get; set; }
            public string? Remarks { get; set; }
            public bool IsRoadSide { get; set; }
            public bool IsForkLift { get; set; }
            public bool IsTailGate { get; set; }
            public bool IsPalletJack { get; set; }
            public bool IsLoadingBay { get; set; }
            public bool IsSmallTruck { get; set; }
            public bool IsPermitEndorse { get; set; }
        }
        public class PortResponse
        {
            public bool Error { get; set; }
            public string? Message { get; set; }
            public List<PortDto>? Data { get; set; }
            public int Code { get; set; }
        }

        public class Port
        {
            public string? Region { get; set; }
            public string? Country { get; set; }
            public string? PortName { get; set; }
            public string? PortCode { get; set; }
            public bool? AMS { get; set; }
        }

        public class VesselInfoResponse
        {
            public bool Error { get; set; }
            public string? Message { get; set; }
            public List<VesselInfoDto>? Data { get; set; }
            public int Code { get; set; }
        }

        public class VesselInfo
        {
            public int VesselID { get; set; }
            public string? VesselName { get; set; }
            public string? InVoy { get; set; }
            public string? OutVoy { get; set; }
            public string? ETA { get; set; }
            public string? Berth { get; set; }
        }
        public class AddressSearchResultResponse
        {
            public int found { get; set; }
            public int totalNumPages { get; set; }
            public int pageNum { get; set; }
            public List<ResultItem>? results { get; set; }
        }

        public class ResultItem
        {
            public string? SEARCHVAL { get; set; }
            public string? BLK_NO { get; set; }
            public string? ROAD_NAME { get; set; }
            public string? BUILDING { get; set; }
            public string? ADDRESS { get; set; }
            public string? POSTAL { get; set; }
            public string? X { get; set; }
            public string? Y { get; set; }
            public string? LATITUDE { get; set; }
            public string? LONGITUDE { get; set; }
        }
        public class VesselInfoPortnetBerthingResponse
        {
            public List<VesselInfoPortnetErrorObject>? errors { get; set; }
            public List<VesselInfoPortnetResultObjectBerthing>? results { get; set; }
        }
        public class VesselInfoPortnetOperationResponse
        {
            public List<VesselInfoPortnetErrorObject>? errors { get; set; }
            public List<VesselInfoPortnetResultObjectOperation>? results { get; set; }
        }

        public class VesselInfoPortnetErrorObject
        {
            public string? type { get; set; }
            public string? code { get; set; }
            public string? message { get; set; }
        }
        public class VesselInfoPortnetResultObjectBerthing
        {
            public int? imoN { get; set; }
            public string? fullVslM { get; set; }
            public string? abbrVslM { get; set; }
            public string? fullInVoyN { get; set; }
            public string? inVoyN { get; set; }
            public string? fullOutVoyN { get; set; }
            public string outVoyN { get; set; }
            public string? shiftSeqN { get; set; }
            public DateTime? bthgDt { get; set; }
            public DateTime? unbthgDt { get; set; }
            public string? berthN { get; set; }
            public string? status { get; set; }
            public string? abbrTerminalM { get; set; }
        }
        public class VesselInfoPortnetResultObjectOperation
        {
            public int? imoN { get; set; }
            public string? fullVslM { get; set; }
            public string? abbrVslM { get; set; }
            public string? fullInVoyN { get; set; }
            public string? inVoyN { get; set; }
            public string? fullOutVoyN { get; set; }
            public string outVoyN { get; set; }
            public string? shiftSeqN { get; set; }
            public DateTime? atbDt { get; set; }
            public DateTime? atuDt { get; set; }
            public DateTime? codDt { get; set; }
            public DateTime? colDt { get; set; }
            public DateTime? cobDt { get; set; }
            public string? berthN { get; set; }
            public string? status { get; set; }
            public string? abbrTerminalM { get; set; }
        }

        public class SendJobPermitResponse
        {
            public bool Error { get; set; }
            public string Message { get; set; }
            public List<JobPermitResult> Data { get; set; }
            public int Code { get; set; }
        }

        public class JobPermitResult
        {
            public int InseredId { get; set; }
            public string GIVSO { get; set; }
            public string Permit { get; set; }
            public bool IsSuccess { get; set; }
            public string DetailMessage { get; set; }
        }
        public class SendJobPhotoSITSResponse
        {
            public bool Error { get; set; }
            public string message { get; set; }
            public int status { get; set; }
            public SendJobPhotoSITSResult result { get; set; }
        }
        public class SendJobPhotoSITSResult
        {
            public bool data { get; set; }
            public string message { get; set; }
        }

        public class SendJobImportResponse
        {
            public bool Error { get; set; }
            public string Message { get; set; }
            public List<JobImportResult> Data { get; set; }
            public int Code { get; set; }
        }

        public class JobImportResult
        {
            public int InseredId { get; set; }
            public string JobNo { get; set; }
            public string PO { get; set; }
            public bool IsSuccess { get; set; }
            public string DetailMessage { get; set; }
        }
    }
}
