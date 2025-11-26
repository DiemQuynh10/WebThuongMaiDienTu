using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopMVC.Models
{
    public class VoucherSanPham
    {
        public int VoucherId { get; set; }
        [ForeignKey(nameof(VoucherId))]
        public Voucher? Voucher { get; set; }

        public int SanPhamId { get; set; }
        [ForeignKey(nameof(SanPhamId))]
        public SanPham? SanPham { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? GiaGiam { get; set; } // Giá fix cho Flash Sale

        public int SoLuongPhanBo { get; set; } = 0; // Tổng số lượng bán
        public int SoLuongDaBan { get; set; } = 0;  // Số lượng đã bán
    }
}