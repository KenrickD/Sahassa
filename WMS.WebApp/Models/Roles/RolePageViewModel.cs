namespace WMS.WebApp.Models.Roles
{
    public class RolePageViewModel
    {
        public RoleViewModel Role { get; set; } = new();
        public bool HasEditAccess { get; set; }
        public bool IsEdit { get; set; }
    }
}
