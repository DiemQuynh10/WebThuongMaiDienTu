namespace ShopMVC.Models   // <- thêm dòng này
{
    public class DonHangLog
    {
        public int Id { get; set; }
        public int MaDH { get; set; }
        public TrangThaiDonHang From { get; set; }
        public TrangThaiDonHang To { get; set; }
        public string? ByUser { get; set; }
        public DateTime At { get; set; } = DateTime.UtcNow;

        public DonHang? DonHang { get; set; }
    }
}
