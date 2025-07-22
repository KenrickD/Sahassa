using WMS.Domain.Models;

namespace WMS.Domain.DTOs.Users
{
    public class UserPermissionsDataResultDto
    {
        public List<Permission> AllPermissions { get; set; } = new();
        public List<Guid> UserPermissionIds { get; set; } = new();
    }
}
