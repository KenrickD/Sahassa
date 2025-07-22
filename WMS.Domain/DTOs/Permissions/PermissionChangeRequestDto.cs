using System.ComponentModel.DataAnnotations;

namespace WMS.WebApp.Models.Permissions
{
    public class PermissionChangeRequestDto
    {
        [Required]
        public Guid PermissionId { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // "add" or "remove"
    }
}
