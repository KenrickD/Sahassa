using WMS.Domain.Models;

namespace WMS.Domain.DTOs.Users
{
    public class UserRolesDataResultDto
    {
        public List<Role> AllRoles { get; set; } = new();
        public List<Guid> UserRoleIds { get; set; } = new();
    }
}
