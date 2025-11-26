using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopMVC.Models
{
    public class DanhGia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int IdSanPham { get; set; }
        [ForeignKey(nameof(IdSanPham))]
        public SanPham? SanPham { get; set; }

        [Required]
        public int IdDonHang { get; set; }
        [ForeignKey(nameof(IdDonHang))]
        public DonHang? DonHang { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        // Giả sử bạn dùng Identity, liên kết với ApplicationUser
        // [ForeignKey(nameof(UserId))]
        // public ApplicationUser? User { get; set; }

        [Range(1, 5)]
        public int SoSao { get; set; } = 5;

        public string? NoiDung { get; set; }

        public string? HinhAnh { get; set; } // Sẽ lưu URL của ảnh

        public bool HienThiTen { get; set; } = true; // Toggle "Hiển thị tên"

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public TrangThaiDanhGia TrangThai { get; set; } = TrangThaiDanhGia.ChoDuyet;
        public bool LaNoiBat { get; set; } = false;
    }
}