using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_Container
{
    public class ContainerViewDto
    {
        public Guid ContainerId { get; set; }
        public string ContainerNo_GW { get; set; } = default!;
        public DateTime? PlannedDelivery_GW { get; set; }
        public string? Remarks { get; set; }
        public string? ContainerURL { get; set; }
        public bool HasPhotos { get; set; }
        public string? UnstuffedBy { get; set; }
        public DateTime? UnstuffedDate { get; set; }
        public int PhotoCount { get; set; }
        public List<ContainerPhotoDto> ContainerPhotos { get; set; } = new List<ContainerPhotoDto>();
        public string? ConcatePO { get; set; }
        public int JobId { get; set; }
        public bool IsLoose { get; set; }
        public bool IsSamplingArrAtWarehouse { get; set; }
        public string? JobType { get; set; }
        public DateTime? StuffedDate { get; set; }
        public string? StuffedBy { get; set; }
        public string? JobReference { get; set; }
        public string? SealNo { get; set; }
        public int Size { get; set; }
        public bool IsGinger { get; set; }
    }
}
