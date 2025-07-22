using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Domain.DTOs.GIV_Container
{
    public class ContainerCreateDto
    {
        public string ContainerNo_GW { get; set; } = default!;
        public DateTime? PlannedDelivery_GW { get; set; }
        public string? Remarks { get; set; }
        [Url(ErrorMessage = "ContainerURL must be a valid URL.")]
        [Required]
        public string ContainerURL { get; set; }

        public string? PO { get; set; }
        public string? HBL { get; set; }
        public DateTime? UnstuffStartTime { get; set; }
        public DateTime? UnstuffEndTime { get; set; }
        public ContainerProcessType ProcessType { get; set; } = ContainerProcessType.Import;
    }
}
