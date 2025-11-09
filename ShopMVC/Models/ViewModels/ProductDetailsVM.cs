using System.Collections.Generic;

namespace ShopMVC.Models.ViewModels
{
    public class ProductDetailsVM
    {
        public SanPham Product { get; set; } = default!;
        public List<SanPham> Siblings { get; set; } = new();
        public string? ThuocTinh2Label { get; set; }   // ví dụ "Size", "Dung lượng" (tuỳ danh mục)
        public string ShippingEtaText { get; set; } = "";
    }
}
