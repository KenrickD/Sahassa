using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_Container
{
    public class Containers
    {
        public Guid ContainerId { get; set; }
        public string ContainerNo_GW { get; set; } = default!;
        public DateTime? PlannedDelivery_GW { get; set; }
        public string? Remarks { get; set; }
        public string? ContainerURL { get; set; }
        public bool HasPhotos { get; set; }
        public string? UnstuffedBy { get; set; }
        public DateTime? UnstuffedDate { get; set; }
        public string? JobReference { get; set; }
        public string? SealNo { get; set; }
        public int Size { get; set; }
    }
}
