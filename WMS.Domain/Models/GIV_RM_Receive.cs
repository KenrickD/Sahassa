using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.Models
{
    [Table("TB_GIV_RM_Receive")]
    public class GIV_RM_Receive : TenantEntity
    {
        public TransportType TypeID { get; set; }
        public Guid RawMaterialId { get; set; }
        public virtual GIV_RawMaterial RawMaterial { get; set; } = default!;
        public Guid? ContainerId { get; set; }
        public virtual GIV_Container Container { get; set; } = default!;
        public Guid? PackageTypeId { get; set; }
        public virtual GeneralCode? PackageType { get; set; } = null!;
        [MaxLength(100)]
        public string? BatchNo { get; set; } = default!;
        public DateTime ReceivedDate { get; set; }
        public string ReceivedBy { get; set; } = default!;
        public string? Remarks { get; set; }
        public string? PO { get; set; }
        public virtual ICollection<GIV_RM_ReceivePallet> RM_ReceivePallets { get; set; } = new List<GIV_RM_ReceivePallet>();
        public Guid? GroupId { get; set; }

    }
    public enum TransportType
    { Container,Lorry }

}
