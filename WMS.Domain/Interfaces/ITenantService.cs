namespace WMS.Domain.Interfaces
{
    public interface ITenantService
    {
        Guid CurrentWarehouseId { get; }
        Guid? CurrentClientId { get; }
        bool IsSystemAdmin { get; }
        bool IsWarehouseManager { get; }
        bool IsClientUser { get; }
    }
}
