using System.ComponentModel.DataAnnotations;

namespace ShopMVC.Models.ViewModels
{
    public class CheckoutVM
    {
        [Required, StringLength(200)]
        public string HoTenNhan { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string DienThoaiNhan { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string DiaChiNhan { get; set; } = string.Empty;

        public decimal PhiVanChuyen { get; set; } = 30000;
        public decimal TienGiam { get; set; } = 0;

        // hiển thị tóm tắt
        public List<GioHangItem> Gio { get; set; } = new();
        public decimal TamTinh => Gio.Sum(x => x.ThanhTien);
        public decimal TongThanhToan => TamTinh +  PhiVanChuyen - TienGiam;
        public string? VoucherCode { get; set; }
    }
}
