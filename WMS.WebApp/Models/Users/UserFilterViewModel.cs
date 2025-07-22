using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WMS.WebApp.Models.Users
{
    // Filter, sort and search options
    public class UserFilterViewModel
    {
        [Display(Name = "Search")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Client")]
        public Guid? ClientId { get; set; }

        [Display(Name = "Warehouse")]
        public Guid? WarehouseId { get; set; }

        [Display(Name = "Status")]
        public bool? IsActive { get; set; }

        [Display(Name = "Role")]
        public Guid? RoleId { get; set; }

        // Sort options
        public string SortColumn { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;

        // Dropdown options
        public List<SelectListItem> ClientOptions { get; set; } = new();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<SelectListItem> RoleOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new()
        {
            new SelectListItem { Value = "", Text = "All" },
            new SelectListItem { Value = "true", Text = "Active" },
            new SelectListItem { Value = "false", Text = "Inactive" }
        };
    }
}
