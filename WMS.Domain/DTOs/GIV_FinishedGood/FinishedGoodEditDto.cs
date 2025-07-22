using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FinishedGood
{
    public class FinishedGoodEditDto
    {
        public Guid Id { get; set; }

        [MaxLength(100)]
        public string? SKU { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }
    }

}
