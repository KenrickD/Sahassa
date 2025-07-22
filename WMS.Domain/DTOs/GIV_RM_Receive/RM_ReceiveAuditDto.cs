using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RM_Receive
{
    public class RM_ReceiveAuditDto
    {
        public Guid Id { get; set; }
        public int TypeID { get; set; }
        [Required]
        [StringLength(100)]
        public string BatchNo { get; set; } = default!;
        public Guid? ContainerId { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string ReceivedBy { get; set; } = default!;
        public string? Remarks { get; set; }
    }
}
