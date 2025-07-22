using System.ComponentModel.DataAnnotations;

namespace WMS.WebApp.Models.Users
{
    // Create a dedicated view model
    public class ChangePasswordViewModel
    {
        public Guid UserId { get; set; }
        public string? CurrentPassword { get; set; }
        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }
        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Password and confirmation password do not match")]
        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }
    }
}
