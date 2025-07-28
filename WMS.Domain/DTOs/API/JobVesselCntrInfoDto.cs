using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.API
{
    public class JobVesselCntrInfoDto
    {
        public int JobId { get; set; }
        public string? YourRef { get; set; }
        public string? ContainerNumber { get; set; }
        public string? SealNumber { get; set; }
        public int ContainerSize { get; set; }
        public string? VesselInfoName { get; set; }
        public string? VesselInfoFullName { get; set; }
        public string? VesselInfoInVoy { get; set; }
        public string? VesselInfoOutVoy { get; set; }
        public string? VesselInfoBerth { get; set; }
        public DateTime? VesselInfoETA { get; set; }
        public string? POL { get; set; }
        public string? HBL { get; set; }
        public string? Marks { get; set; }
        public string? JobType { get; set; }
        public bool IsGinger { get; set; }
    }
}
