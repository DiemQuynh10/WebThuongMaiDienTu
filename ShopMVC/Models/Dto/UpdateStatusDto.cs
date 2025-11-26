// Models/Dto/UpdateStatusDto.cs
namespace ShopMVC.Models.Dto
{
    public class UpdateStatusDto
    {
        public TrangThaiDonHang To { get; set; }
        public string? ReasonCode { get; set; }
        public string? Note { get; set; }
        public bool NotifyCustomer { get; set; }

        // NEW: cho phép override (đi ngược flow)
        public bool IsOverride { get; set; }
    }
}
