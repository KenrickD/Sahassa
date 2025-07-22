using System;
using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.DTOs.Warehouses
{
    public class WarehouseDto
    {

        public Guid Id { get; set; }

        [Required]
        [MaxLength(250)]
        public string Name { get; set; } = default!;

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = default!;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(20)]
        public string? ZipCode { get; set; }

        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        [MaxLength(100)]
        [EmailAddress]
        public string? ContactEmail { get; set; }

        [MaxLength(100)]
        public string? ContactPhone { get; set; }

        public bool IsActive { get; set; }

        // Navigation properties
        public int ClientCount { get; set; }
        public int ZoneCount { get; set; }
        public int LocationCount { get; set; }
        public int UserCount { get; set; }

        // Configuration
        public WarehouseConfigurationDto? Configuration { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}