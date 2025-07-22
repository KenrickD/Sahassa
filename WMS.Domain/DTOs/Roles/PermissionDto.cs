using System;

namespace WMS.Domain.DTOs.Roles
{
    public class PermissionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string Module { get; set; } = default!;
        public bool IsAssigned { get; set; }
    }
}