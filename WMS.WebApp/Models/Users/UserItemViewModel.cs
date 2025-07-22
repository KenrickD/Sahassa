using System.ComponentModel.DataAnnotations;

namespace WMS.WebApp.Models.Users
{
    public class UserItemViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Profile")]
        public string? ProfileImageUrl { get; set; }

        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Client")]
        public string? ClientName { get; set; }

        [Display(Name = "Warehouse")]
        public string WarehouseName { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public bool IsActive { get; set; }

        [Display(Name = "Roles")]
        public List<string> Roles { get; set; } = new();

        // Timestamp info for the table
        [Display(Name = "Created")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Last Modified")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime? ModifiedAt { get; set; }

        // Helper properties for the UI
        public string StatusClass => IsActive ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800";
        public string StatusText => IsActive ? "Active" : "Inactive";
        public string RolesDisplay => string.Join(", ", Roles);
    }
}
