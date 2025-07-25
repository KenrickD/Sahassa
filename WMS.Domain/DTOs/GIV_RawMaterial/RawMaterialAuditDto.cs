﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RawMaterial
{
    public class RawMaterialAuditDto
    {
        public Guid Id { get; set; }
        public string MaterialNo { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
