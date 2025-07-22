using WMS.WebApp.Models.Permissions;

namespace WMS.WebApp.Models.Users
{
    public class UserPermissionsDataViewModel
    {
        public List<PermissionViewModel> AllPermissions { get; set; } = new();
        public List<Guid> UserPermissions { get; set; } = new();
    }
}
