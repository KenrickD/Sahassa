namespace WMS.Domain.DTOs.Auth
{
    public class AuthResultDto
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public Guid? UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? ProfileImagePath { get; set; }
        public string? ProfileImageUrl { get; set; }
        public Guid? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public Guid? ClientId { get; set; }
        public string? ClientName { get; set; }
        public List<string>? Roles { get; set; }
        public List<string>? Permissions { get; set; }
        public List<string>? Errors { get; set; }
    }
}