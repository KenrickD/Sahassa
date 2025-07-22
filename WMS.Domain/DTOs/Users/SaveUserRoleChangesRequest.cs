namespace WMS.Domain.DTOs.Users
{
    public class SaveUserRoleChangesRequest
    {
        public Guid UserId { get; set; }
        public List<RoleChangeRequestDto> Changes { get; set; } = new();
    }
}
