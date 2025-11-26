using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopMVC.Models
{
    public enum TrangThaiDonHang { ChoXacNhan, ChuanBi, DangGiao, HoanTat, DaHuy }
    public enum PhuongThucThanhToan { COD, OnlineMock }

    public class DonHang
    {
        public int Id { get; set; }

        [Required, StringLength(30)]
        public string MaDon { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string HoTenNhan { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string DienThoaiNhan { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string DiaChiNhan { get; set; } = string.Empty;

        public decimal PhiVanChuyen { get; set; }
        public decimal TienGiam { get; set; }
        public decimal TongTruocGiam { get; set; }
        public decimal TongThanhToan { get; set; }

        public PhuongThucThanhToan PhuongThucThanhToan { get; set; } = PhuongThucThanhToan.COD;
        public TrangThaiDonHang TrangThai { get; set; } = TrangThaiDonHang.ChoXacNhan;

        public DateTime NgayDat { get; set; } = DateTime.UtcNow;
        public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
        public int? VoucherId { get; set; }
        public string? VoucherCode { get; set; }
        public ICollection<DonHangChiTiet> ChiTiets { get; set; } = new List<DonHangChiTiet>();
    }

    public class DonHangChiTiet
    {
        public int Id { get; set; }

        public int IdDonHang { get; set; }
        [ForeignKey(nameof(IdDonHang))]
        public DonHang? DonHang { get; set; }

        public int IdSanPham { get; set; }
        [ForeignKey(nameof(IdSanPham))]
        public SanPham? SanPham { get; set; }

        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
    }
}
