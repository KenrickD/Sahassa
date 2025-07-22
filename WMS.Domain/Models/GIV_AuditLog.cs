using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_AuditLog")]
    public class GIV_AuditLog:BaseEntity 
    {
        [Required]
        [MaxLength(20)]
        public string EntityName { get; set; } = default!;
        [Required]
        public Guid EntityId { get; set; }
        [Required]
        [MaxLength(20)]
        public string Action { get; set; } = default!; // Create, Update, Delete
        [Required]
        public string ChangesJson { get; set; } = default!; // JSON
        //[Required]
        //public string NewValues { get; set; } = default!; // JSON
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = default!;
        //[Required]
        [MaxLength(50)]
        public string? IpAddress { get; set; }
        public Guid? WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }
        [NotMapped]
        public EntityEntry? TempEntry { get; set; }
        public DateTime ChangesDate { get; set; }
    }
}
