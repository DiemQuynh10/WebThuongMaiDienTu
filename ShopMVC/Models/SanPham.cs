using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore; // để dùng [Index] & [Precision]

namespace ShopMVC.Models
{
    public enum TrangThaiHienThi { An = 0, Hien = 1 }

    // Index giúp lọc/nhóm nhanh trong admin & user
    [Index(nameof(ParentId))]
    [Index(nameof(IdDanhMuc))]
    [Index(nameof(IdThuongHieu))]
    public class SanPham
    {
        public int Id { get; set; }

        [Required, StringLength(250)]
        public string Ten { get; set; } = string.Empty;     // Tên mẫu (ở "cha"); ở "con" có thể trùng tên cha

        [StringLength(120)]
        public string? DisplaySuffix { get; set; }          // Hậu tố hiển thị của biến thể: "Hồng 128GB", "M", ...

        [StringLength(400)]
        public string? MoTaNgan { get; set; }

        public string? MoTaChiTiet { get; set; }

        // Dùng Precision để map tiền tệ ổn định (SQL Server: decimal(18,2))
        [Range(0, double.MaxValue)]
        [Precision(18, 2)]
        public decimal Gia { get; set; }

        [Range(0, double.MaxValue)]
        [Precision(18, 2)]
        public decimal? GiaKhuyenMai { get; set; }

        [Range(0, int.MaxValue)]
        public int TonKho { get; set; }

        public bool LaNoiBat { get; set; } = false;

        public TrangThaiHienThi TrangThai { get; set; } = TrangThaiHienThi.Hien;

        public DateTime NgayTao { get; set; }           // set ở service/controller khi tạo
        public DateTime NgayCapNhat { get; set; }       // set khi cập nhật

        // Nhóm cha ↔ con
        public int? ParentId { get; set; }              // null = sản phẩm cha; khác null = biến thể
        public SanPham? Parent { get; set; }            // navigation về "cha"
        public ICollection<SanPham> Children { get; set; } = new List<SanPham>(); // các biến thể

        // Thuộc tính phân biệt biến thể
        [StringLength(60)]
        public string? Mau { get; set; }                // "Hồng", "Đen", ...

        [StringLength(60)]
        public string? ThuocTinh2 { get; set; }         // "128GB" / "M" / "42mm"...

        [StringLength(80)]
        public string? SKU { get; set; }                // Mã biến thể duy nhất (khuyến nghị Unique ở DB)

        public bool IsActive { get; set; } = true;      // ẩn/hiện biến thể độc lập với TrangThai

        // FK
        public int IdDanhMuc { get; set; }
        [ForeignKey(nameof(IdDanhMuc))]
        public DanhMuc? DanhMuc { get; set; }

        public int IdThuongHieu { get; set; }
        [ForeignKey(nameof(IdThuongHieu))]
        public ThuongHieu? ThuongHieu { get; set; }

        public ICollection<AnhSanPham> Anhs { get; set; } = new List<AnhSanPham>();
        public virtual ICollection<ChiTietSanPham>? ChiTietSanPhams { get; set; }
     = new List<ChiTietSanPham>();

        // Trong class SanPham.cs
        [NotMapped]
        public VoucherSanPham? FlashSaleInfo { get; set; } // Chứa thông tin giá giảm, số lượng đã bán...

        // Tiện ích hiển thị (không map DB)
        [NotMapped]
        public string TenDayDu => string.IsNullOrWhiteSpace(DisplaySuffix) ? Ten : $"{Ten} {DisplaySuffix}";
    }
}
