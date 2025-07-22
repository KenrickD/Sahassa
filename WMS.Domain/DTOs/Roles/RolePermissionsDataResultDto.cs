using WMS.Domain.Models;

namespace WMS.Domain.DTOs.Roles
{
    public class RolePermissionsDataResultDto
    {
        public List<Permission> AllPermissions { get; set; } = new();
        public List<Guid> RolePermissionIds { get; set; } = new();
    }
}
