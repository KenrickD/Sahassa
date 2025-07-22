namespace WMS.Domain.DTOs.Users
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? ProfileImagePath { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Related entities
        public Guid? ClientId { get; set; }
        public string? ClientName { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public List<Guid> CurrentRoleIds { get; set; } = new List<Guid>();
        public List<string> Permissions { get; set; } = new List<string>();

        // this two for decide whether show and hide edit and delete button
        public bool HasWriteAccess { get; set; }
        public bool HasDeleteAccess { get; set; }
        public bool IsSystemAdmin
        {
            get
            {
                return this.Roles.Contains(AppConsts.Roles.SYSTEM_ADMIN);
            }
        }
    }
}
