using System.ComponentModel.DataAnnotations;

namespace ShopMVC.Models
{
    public class ThuongHieu
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Ten { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? MoTa { get; set; }

        [StringLength(500)]
        public string? LogoUrl { get; set; }

        [StringLength(200)]
        public string? Slug { get; set; }

        public bool HienThi { get; set; } = true;

        public ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
    }
}
