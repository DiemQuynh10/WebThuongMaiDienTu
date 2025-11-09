using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopMVC.Models
{
    public class DanhMuc
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Ten { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Slug { get; set; }

        [StringLength(1000)]
        public string? MoTa { get; set; }

        public int? DanhMucChaId { get; set; }
        [ForeignKey(nameof(DanhMucChaId))]
        public DanhMuc? DanhMucCha { get; set; }

        public int ThuTu { get; set; } = 0;
        public bool HienThi { get; set; } = true;
        public string? IconUrl { get; set; }

        public ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
    }
}
