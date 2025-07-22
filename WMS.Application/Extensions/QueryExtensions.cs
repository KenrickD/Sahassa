using WMS.Domain.DTOs;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;

namespace WMS.Application.Extensions
{
    public static class QueryExtensions
    {
        public static IQueryable<T> ApplyTenantFilter<T>(
            this IQueryable<T> query,
            ICurrentUserService currentUserService)
            where T : TenantEntity
        {
            // Global query filters for multi-tenancy
            // Apply warehouse filter ONLY if not system admin or general manager
            if (!currentUserService.IsInRole(AppConsts.Roles.SYSTEM_ADMIN) &&
                !currentUserService.IsInRole(AppConsts.Roles.WAREHOUSE_GM))
            {
                var warehouseId = currentUserService.CurrentWarehouseId;
                query = query.Where(e => e.WarehouseId == warehouseId);
            }

            return query;
        }

        public static IQueryable<T> ApplyClientFilter<T>(
            this IQueryable<T> query,
            ICurrentUserService currentUserService)
            where T : Client
        {
            // Global query filters for multi-tenancy
            // Apply warehouse filter ONLY if not system admin or general manager
            if (!currentUserService.IsInRole(AppConsts.Roles.SYSTEM_ADMIN) &&
                !currentUserService.IsInRole(AppConsts.Roles.WAREHOUSE_GM))
            {
                var clientId = currentUserService.CurrentClientId;
                query = query.Where(e => e.Id == clientId);
            }

            return query;
        }
    }
}
