using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FG_ReceivePallet
{
    public class FG_ReceivePalletEditDto
    {
        public Guid Id { get; set; }

        [MaxLength(11)]
        [Display(Name = "Pallet Code")]
        public string? PalletCode { get; set; }

        [Required]
        [Display(Name = "Handled By")]
        public string HandledBy { get; set; } = default!;

        [Display(Name = "Stored By")]
        public string? StoredBy { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Pack Size")]
        public int PackSize { get; set; }

        [Display(Name = "Location")]
        public string? LocationName { get; set; }

        [Display(Name = "Released?")]
        public bool IsReleased { get; set; }

        public Guid ReceiveId { get; set; }
        public Guid? LocationId { get; set; }
    }
}
