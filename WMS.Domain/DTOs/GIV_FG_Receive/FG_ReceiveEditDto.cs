using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FG_Receive
{
    public class FG_ReceiveEditDto
    {
        public Guid Id { get; set; }

        [MaxLength(100)]
        public string BatchNo { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime ReceivedDate { get; set; }

        [Required]
        [MaxLength(100)]
        public string ReceivedBy { get; set; } = string.Empty;

        public string? Remarks { get; set; }
    }

}
