using System.ComponentModel.DataAnnotations;
using WMS.WebApp.Validations;

namespace WMS.WebApp.Models.Roles
{
    public class RoleFormViewModel
    {
        [Required]
        [MaxLength(100)]
        [NoSqlInjection]
        public string Name { get; set; } = default!;

        [MaxLength(500)]
        [NoSqlInjection]
        public string? Description { get; set; }
    }
}
