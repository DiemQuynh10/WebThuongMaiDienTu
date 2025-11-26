using ShopMVC.Models;
namespace ShopMVC.Models
{
    public class DonHangNote
    {
        public int Id { get; set; }
        public int MaDH { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DonHang? DonHang { get; set; }
    }
}
