using System.ComponentModel.DataAnnotations;
using WMS.WebApp.Models.Permissions;

namespace WMS.WebApp.Models.Users
{
    public class SaveUserPermissionsRequestDto
    {
        public Guid UserId { get; set; }

        public List<PermissionChangeRequestDto> Changes { get; set; } = new();
    }
}
