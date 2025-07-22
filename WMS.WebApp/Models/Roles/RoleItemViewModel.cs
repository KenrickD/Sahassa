using System.ComponentModel.DataAnnotations;

namespace WMS.WebApp.Models.Roles
{
    public class RoleItemViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Name")]
        public string? Name { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;
   
        // Timestamp info for the table
        [Display(Name = "Created")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Last Modified")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime? ModifiedAt { get; set; }
    }
}
