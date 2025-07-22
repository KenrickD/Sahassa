using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WMS.WebApp.Models.Users
{
    // User profile
    public class UserProfileViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Username")]
        public string? Username { get; set; }

        [Display(Name = "Email")]
        [EmailAddress]
        public string? Email { get; set; }

        [Display(Name = "First Name")]
        [Required(ErrorMessage = "First Name is required")]
        [MaxLength(100, ErrorMessage = "First Name cannot exceed 100 characters")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        [MaxLength(100, ErrorMessage = "Last Name cannot exceed 100 characters")]
        public string? LastName { get; set; }

        [Display(Name = "Phone Number")]
        [Phone]
        [MaxLength(20, ErrorMessage = "Phone Number cannot exceed 20 characters")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Profile Image")]
        public string? ProfileImageUrl { get; set; }

        [Display(Name = "Profile Image")]
        public IFormFile? ProfileImage { get; set; }

        [Display(Name = "Warehouse")]
        public Guid WarehouseId { get; set; }

        [Display(Name = "Warehouse")]
        public string? WarehouseName { get; set; }

        [Display(Name = "Client")]
        public Guid? ClientId { get; set; }

        [Display(Name = "Client")]
        public string? ClientName { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();

        //// For password change
        //[Display(Name = "Current Password")]
        //public string CurrentPassword { get; set; }

        //[Display(Name = "New Password")]
        //[StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        //public string NewPassword { get; set; }

        //[Display(Name = "Confirm Password")]
        //[Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        //public string ConfirmPassword { get; set; }
    }
}
