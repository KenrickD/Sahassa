using System;
using System.Collections.Generic;

namespace WMS.Domain.DTOs.Roles
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public List<PermissionDto>? Permissions { get; set; }
        public bool IsAssigned { get; set; } // For role assignment scenarios

        // this two for decide whether show and hide edit and delete button
        public bool HasWriteAccess { get; set; }
        public bool HasDeleteAccess { get; set; }
    }
}