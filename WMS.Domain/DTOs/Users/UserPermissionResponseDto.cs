using System;

namespace WMS.Domain.DTOs.Users
{
    public class UserPermissionResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
