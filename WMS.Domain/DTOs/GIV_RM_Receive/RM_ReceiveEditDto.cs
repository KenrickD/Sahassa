using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.Models;

namespace WMS.Domain.DTOs.GIV_RM_Receive
{
    public class RM_ReceiveEditDto
    {
        public Guid Id { get; set; }
        public string BatchNo { get; set; } = string.Empty;
        public DateTime ReceivedDate { get; set; }
        [Required]
        [MaxLength(100)]
        public string ReceivedBy { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

}
