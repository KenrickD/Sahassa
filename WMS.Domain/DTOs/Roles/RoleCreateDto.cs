using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.DTOs.Roles
{
    public class RoleCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = default!;

        [MaxLength(500)]
        public string? Description { get; set; }

        public List<Guid>? PermissionIds { get; set; }
    }
}