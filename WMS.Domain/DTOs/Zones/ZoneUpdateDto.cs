using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.Zones
{
    public class ZoneUpdateDto
    {
        [Required(ErrorMessage = "Zone name is required")]
        [StringLength(250, ErrorMessage = "Zone name cannot exceed 250 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Zone code is required")]
        [StringLength(100, ErrorMessage = "Zone code cannot exceed 100 characters")]
        public string Code { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters")]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }
}
