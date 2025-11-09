namespace ShopMVC.Models
{
    public class VoucherDanhMuc
    {
        public int VoucherId { get; set; }
        public Voucher Voucher { get; set; } = null!;

        public int DanhMucId { get; set; }
        public DanhMuc DanhMuc { get; set; } = null!;
    }
}
