using Microsoft.AspNetCore.Http;
using WMS.Domain.DTOs;
using WMS.Domain.Interfaces;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUserService _currentUserService;

    public TenantService(IHttpContextAccessor httpContextAccessor, ICurrentUserService currentUserService)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public Guid CurrentWarehouseId
    {
        get
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                // Fall back to a default warehouse ID or get from _currentUserService
                return _currentUserService.CurrentWarehouseId;
            }

            var warehouseIdClaim = _httpContextAccessor.HttpContext.User
                .FindFirst("warehouse_id")?.Value;

            if (string.IsNullOrEmpty(warehouseIdClaim))
            {
                // Fall back to a default or throw a more specific exception
                return Guid.Empty; // Or another default value
            }

            return Guid.Parse(warehouseIdClaim);
        }
    }

    // Apply similar changes to other properties
    public Guid? CurrentClientId
    {
        get
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return null;
            }

            var clientIdClaim = _httpContextAccessor.HttpContext.User
                .FindFirst("client_id")?.Value;

            if (string.IsNullOrEmpty(clientIdClaim))
            {
                return null;
            }

            return Guid.Parse(clientIdClaim);
        }
    }

    public bool IsSystemAdmin =>
        _httpContextAccessor.HttpContext?.User?.IsInRole(AppConsts.Roles.SYSTEM_ADMIN) ?? false;

    public bool IsWarehouseManager =>
        _httpContextAccessor.HttpContext?.User?.IsInRole(AppConsts.Roles.WAREHOUSE_MANAGER) ?? false;

    public bool IsClientUser =>
        _httpContextAccessor.HttpContext?.User?.IsInRole(AppConsts.Roles.CLIENT_USER) ?? false;
}