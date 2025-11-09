namespace ShopMVC.Models
{
    public class VoucherThuongHieu
    {
        public int VoucherId { get; set; }
        public Voucher Voucher { get; set; } = null!;

        public int ThuongHieuId { get; set; }
        public ThuongHieu ThuongHieu { get; set; } = null!;
    }
}
