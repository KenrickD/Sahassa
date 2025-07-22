using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RawMaterial
{
    public class RawMaterialEditDto
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string MaterialNo { get; set; } = default!;

        [StringLength(255)]
        public string? Description { get; set; }
    }
}
