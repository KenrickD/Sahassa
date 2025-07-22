using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.Models
{
    public abstract class TenantEntity : BaseEntity
    {
        public Guid WarehouseId { get; set; }
        public virtual Warehouse Warehouse { get; set; } = null!;
    }
}
