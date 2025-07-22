using Microsoft.AspNetCore.Mvc.Rendering;
using WMS.WebApp.Models.Roles;

namespace WMS.WebApp.Models.Users
{
    public class ViewProfileModel
    {
        public UserProfileViewModel User { get; set; }
        public bool IsOwnProfile { get; set; }
        public bool HasEditAccess { get; set; }
        public SelectList Warehouses { get; set; }
        public SelectList Clients { get; set; }

        // For error/success messages
        public string StatusMessage { get; set; }
        public bool IsSuccess { get; set; }
        public ChangePasswordViewModel ChangePasswordVM { get; set; }

        //User permission
        // Add these new properties for permissions
        public ICollection<UserPermissionViewModel>? UserPermissions { get; set; }
        public List<Guid> SelectedRoleIds { get; set; } = new();
        public List<SelectListItem> RoleOptions { get; set; } = new();
    }
}
