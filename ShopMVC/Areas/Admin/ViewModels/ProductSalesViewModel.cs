namespace ShopMVC.Areas.Admin.ViewModels
{
    public class ProductSalesViewModel
    {
        public string TenSanPham { get; set; } = string.Empty;
        public int SoLuongBan { get; set; }
        public decimal DoanhThu { get; set; } // Thêm doanh thu nếu cần hiển thị sau này
        public string? HinhAnh { get; set; } // Thêm trường hình ảnh
    }
}