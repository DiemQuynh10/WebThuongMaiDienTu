// Models/OrderStatusHistory.cs
using System;

namespace ShopMVC.Models
{
    public class OrderStatusHistory
    {
        public int Id { get; set; }
        public int OrderId { get; set; }                // khóa ngoại tới DonHang.Id
        public TrangThaiDonHang FromStatus { get; set; }
        public TrangThaiDonHang ToStatus { get; set; }
        public string? ReasonCode { get; set; }         // ví dụ: OUT_OF_STOCK, CUSTOMER_REQUEST...
        public string? Note { get; set; }               // ghi chú chi tiết
        public string? ChangedByUserId { get; set; }    // nếu đang dùng Identity
        public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
        public string? MetadataJson { get; set; }       // optional: lưu trackingCode, carrier...
        public bool IsOverride { get; set; } = false;

        public DonHang? Order { get; set; }
    }
}
