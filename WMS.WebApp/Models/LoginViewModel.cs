﻿using System.ComponentModel.DataAnnotations;

namespace WMS.WebApp.Models
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Warehouse")]
        public Guid? WarehouseId { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
        public string? Fullname { get; set; }
    }
}
