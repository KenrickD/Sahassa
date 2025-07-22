namespace WMS.Domain.DTOs.Auth
{
    /// <summary>
    /// Request to revoke a refresh token
    /// </summary>
    public class RevokeTokenRequestDto
    {
        /// <summary>
        /// The refresh token to revoke. If not provided, will use token from cookie.
        /// </summary>
        public string? Token { get; set; }
    }
}
