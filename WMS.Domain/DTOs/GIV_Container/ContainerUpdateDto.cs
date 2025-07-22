using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_Container
{
    public class ContainerUpdateDto
    {
        public Guid Id { get; set; }
        public DateTime? UnstuffedDate { get; set; }
        public string? UnstuffedBy { get; set; }
        public string? Remarks { get; set; }
        public List<IFormFile>? Photos { get; set; }
        public string? PO { get; set; }
        public string? HBL { get; set; }
        public DateTime? UnstuffStartTime { get; set; }
        public DateTime? UnstuffEndTime { get; set; }
        [Url(ErrorMessage = "ContainerURL must be a valid URL.")]
        public string? ContainerURL { get; set; }
        public string? SealNo { get; set; }
        public int Size { get; set; } = 0;
        public DateTime? StuffedDate { get; set; }
        public DateTime? StuffStartTime { get; set; }
        public DateTime? StuffEndTime { get; set; }
        public string? StuffedBy { get; set; }
    }
}
