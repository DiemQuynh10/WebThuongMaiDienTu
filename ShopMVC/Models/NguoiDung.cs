using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ShopMVC.Models
{
    public class NguoiDung : IdentityUser
    {
        [StringLength(200)]
        public string? HoTen { get; set; }

        [StringLength(500)]
        public string? DiaChiMacDinh { get; set; }
    }
}
