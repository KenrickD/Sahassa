using System.ComponentModel.DataAnnotations;
using WMS.WebApp.Validations;

namespace WMS.WebApp.Models
{
    public class SecureForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [SecureEmail]
        [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
        public string Email { get; set; } = string.Empty;
    }
}
