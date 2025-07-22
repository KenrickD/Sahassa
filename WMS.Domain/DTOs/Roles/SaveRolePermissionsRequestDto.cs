using WMS.WebApp.Models.Permissions;

namespace WMS.Domain.DTOs.Roles
{
    public class SaveRolePermissionsRequestDto
    {
        public Guid RoleId { get; set; }

        public List<PermissionChangeRequestDto> Changes { get; set; } = new();
    }
}
