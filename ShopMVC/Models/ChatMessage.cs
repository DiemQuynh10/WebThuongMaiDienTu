using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopMVC.Models
{
    // Đại diện cho 1 tin nhắn
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChatSessionId { get; set; }
        [ForeignKey(nameof(ChatSessionId))]
        public ChatSession? ChatSession { get; set; }

        [Required]
        public SenderType Sender { get; set; }

        [Required]
        public string NoiDung { get; set; } = string.Empty;
        public DateTime ThoiGian { get; set; } = DateTime.Now;
    }
}