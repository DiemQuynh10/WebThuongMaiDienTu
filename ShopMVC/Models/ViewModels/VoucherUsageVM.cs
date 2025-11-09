namespace ShopMVC.Models.ViewModels
{
    public class VoucherUsageVM
    {
        public Voucher Voucher { get; set; } = null!;
        public List<DonHang> Orders { get; set; } = new();

        // Không tính đơn đã hủy
        public int UsedCount => Orders.Count(o => o.TrangThai != TrangThaiDonHang.DaHuy);
        public decimal TotalDiscount => Orders.Sum(o => o.TienGiam);
    }
}
