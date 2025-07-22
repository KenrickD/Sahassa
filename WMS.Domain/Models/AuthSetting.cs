namespace WMS.Domain.Models
{
    public class AuthSettings
    {
        public string JwtSecret { get; set; } = default!;
        public string JwtIssuer { get; set; } = default!;
        public string JwtAudience { get; set; } = default!;
        public int AccessTokenExpireMinutes { get; set; } = 60;
        public int RefreshTokenExpireDays { get; set; } = 7;
        public int PasswordMinLength { get; set; } = 8;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireDigit { get; set; } = true;
        public bool RequireNonAlphanumeric { get; set; } = true;
        public int MaxFailedAccessAttempts { get; set; } = 5;
        public int LockoutMinutes { get; set; } = 15;
    }
}