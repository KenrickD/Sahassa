namespace WMS.WebApp.Models.Roles
{
    public class RolePermissionViewModel
    {
        public Guid PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string PermissionDescription { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
    }
}
