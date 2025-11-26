
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopMVC.Models
{
    // Đại diện cho 1 cuộc trò chuyện
    public class ChatSession
    {
        [Key]
        public int Id { get; set; }

        // ID kết nối của user, rất quan trọng để SignalR biết gửi tin cho ai
        [Required]
        [StringLength(100)]
        public string UserConnectionId { get; set; } = string.Empty;

        // ID của user nếu đã đăng nhập
        public string? UserId { get; set; }

        public DateTime ThoiGianTao { get; set; } = DateTime.Now;
        public bool DaDong { get; set; } = false;
        [ForeignKey(nameof(UserId))]
        public NguoiDung? User { get; set; }
        public int? SanPhamId { get; set; } // ID sản phẩm đang xem (nullable)
        [ForeignKey(nameof(SanPhamId))]
        public SanPham? SanPham { get; set; }

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}