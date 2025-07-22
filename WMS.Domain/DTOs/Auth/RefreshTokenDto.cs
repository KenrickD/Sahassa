using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.DTOs.Auth
{
    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "Access token is required")]
        public string AccessToken { get; set; } = default!;

        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = default!;
    }
}