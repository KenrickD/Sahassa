using WMS.WebApp.Models.Permissions;

namespace WMS.WebApp.Models.Roles
{
    public class RolePermissionDataViewModel
    {
        public List<PermissionViewModel> AllPermissions { get; set; } = new();
        public List<Guid> RolePermissions { get; set; } = new();
    }
}
