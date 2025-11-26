using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopMVC.Models
{
    public class ChiTietSanPham
    {
        [Key]
        public int Id { get; set; }

        // Liên kết với bảng sản phẩm cha
        public int SanPhamId { get; set; }
        [ForeignKey("SanPhamId")]
        public virtual SanPham SanPham { get; set; }

        // Các thuộc tính biến thể (Màu, Size...)
        // Bạn có thể sửa tên tùy theo ý định (VD: MauSac, KichThuoc)
        [Display(Name = "Kích thước/Màu sắc")]
        public string TenChiTiet { get; set; }

        // QUAN TRỌNG: Phải có giá ở đây để code trước đó tính toán được
        [Column(TypeName = "decimal(18,2)")]
        public decimal Gia { get; set; }

        public int SoLuongKho { get; set; }
    }
}