using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_Container")]
    public class GIV_Container : BaseEntity
    {
        [MaxLength(100)]
        public string ContainerNo_GW { get; set; } = default!;
        public DateTime? PlannedDelivery_GW { get; set; }
        public string? Remarks { get; set; }
        public string? ContainerURL { get; set; }
        public Guid? WarehouseId { get; set; }
        public DateTime? UnstuffedDate { get; set; }
        public DateTime? UnstuffStartTime { get; set; }
        public DateTime? UnstuffEndTime { get; set; }
        public string? UnstuffedBy { get; set; }
        public virtual Warehouse Warehouse { get; set; } = null!;
        public string? PO { get; set; }
        public string? HBL { get; set; }

        public Guid? StatusId { get; set; }

        // Navigation properties
        public virtual GeneralCode? Status { get; set; } = null!;
        public virtual ICollection<GIV_ContainerPhoto> ContainerPhotos { get; set; } = new List<GIV_ContainerPhoto>();
        public bool IsLoose { get; set; }
        public int LooseNoPallet { get; set; }
        public int LooseNoItem { get; set; }
        public bool IsSamplingArrAtWarehouse { get; set; }
        public ContainerProcessType ProcessType { get; set; }
        public string? JobReference {  get; set; }
        public string? SealNo { get; set; }
        public int Size { get; set; }
        public DateTime? StuffedDate { get; set; }
        public DateTime? StuffStartTime { get; set; }
        public DateTime? StuffEndTime { get; set; }
        public string? StuffedBy { get; set; }
    }
}
