using Microsoft.AspNetCore.Http;
using WMS.Domain.DTOs.SignalR;

namespace WMS.Application.Interfaces
{
    public interface IAPIService
    {
        Task NotifyLocationUpdateAsync(Guid zoneId, string locationCode);
        Task NotifyBulkLocationUpdateAsync(Guid zoneId, List<string> locationCodes);
        Task<string> GetLoginTokenAsync(CancellationToken cancellationToken);
        Task<string> GetPortListAsync(CancellationToken cancellationToken);
        Task<string> GetVesselInfoListAsync(CancellationToken cancellationToken);
        Task<string> GetAddressListAsync(CancellationToken cancellationToken);
        Task<string> GetAddressByPostalCode(string postalCode, CancellationToken cancellationToken);
        Task<string> GetVesselInfoPornetAsync(string type, string vesselName, string inVoy, string outVoy, CancellationToken cancellationToken);
        Task<string> GetVesselInfoByIDsAsync(List<int> vesselIDList, CancellationToken cancellationToken);
        Task<string?> GetBearerTokenFromEmailPasswordAsync(string email, string password, string authUrl, CancellationToken cancellationToken = default);
    }
}
