namespace WMS.WebApp.Models.Users
{
    public class UserPermissionViewModel
    {
        public Guid PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string PermissionDescription { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public string AssignedBy { get; set; } = string.Empty;
    }
}
