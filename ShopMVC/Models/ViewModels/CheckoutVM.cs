using System.ComponentModel.DataAnnotations;

namespace ShopMVC.Models.ViewModels
{
    public class CheckoutVM
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(200)]
        public string HoTenNhan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(20)]
        public string DienThoaiNhan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [StringLength(500)]
        public string DiaChiNhan { get; set; } = string.Empty;

        // Thêm trường Ghi chú (Optional)
        public string? GhiChu { get; set; }

        public decimal PhiVanChuyen { get; set; } = 30000;
        public decimal TienGiam { get; set; } = 0;

        // Hiển thị tóm tắt
        public List<GioHangItem> Gio { get; set; } = new();

        // Logic tính toán
        public decimal TamTinh => Gio.Sum(x => x.ThanhTien);
        public decimal TongThanhToan => TamTinh + PhiVanChuyen - TienGiam;

        public string? VoucherCode { get; set; }
    }
}