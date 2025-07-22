using System.ComponentModel.DataAnnotations;
using WMS.WebApp.Validations;

namespace WMS.WebApp.Models
{
    public class SecureLoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [SecureEmail]
        [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
