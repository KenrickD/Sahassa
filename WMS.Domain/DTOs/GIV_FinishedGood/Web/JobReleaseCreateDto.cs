using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FinishedGood.Web
{
    public class JobReleaseCreateDto
    {
        public List<JobReleaseCreateFinishedGoodDto> FinishedGoods { get; set; } = new List<JobReleaseCreateFinishedGoodDto>();
        public string? JobRemarks { get; set; }
    }

    public class JobReleaseCreateFinishedGoodDto
    {
        public Guid FinishedGoodId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public List<JobReleaseCreateReceiveDto> Receives { get; set; } = new List<JobReleaseCreateReceiveDto>();
    }

    public class JobReleaseCreateReceiveDto
    {
        public Guid ReceiveId { get; set; }
        public string ReleaseType { get; set; } = string.Empty; // "Full" or "Partial"
        public DateTime ReleaseDate { get; set; }
        public List<JobReleaseCreatePalletDto> Pallets { get; set; } = new List<JobReleaseCreatePalletDto>();
        public List<JobReleaseCreateItemDto> Items { get; set; } = new List<JobReleaseCreateItemDto>();
    }

    public class JobReleaseCreatePalletDto
    {
        public Guid PalletId { get; set; }
        public string PalletCode { get; set; } = string.Empty;
    }

    public class JobReleaseCreateItemDto
    {
        public Guid ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
    }
}
