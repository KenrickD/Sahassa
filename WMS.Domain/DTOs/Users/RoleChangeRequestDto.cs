using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.Users
{
    public class RoleChangeRequestDto
    {
        public string Action { get; set; } = string.Empty; // "add" or "remove"
        public Guid RoleId { get; set; }
    }
}
