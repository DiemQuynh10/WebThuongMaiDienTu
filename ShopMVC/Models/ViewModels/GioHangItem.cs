namespace ShopMVC.Models.ViewModels
{
    public class GioHangItem
    {
        public int IdSanPham { get; set; }
        public string Ten { get; set; } = string.Empty;
        public string? Anh { get; set; }
        public decimal DonGia { get; set; }   // dùng GiaKhuyenMai ?? Gia
        public int SoLuong { get; set; }

        public decimal ThanhTien => DonGia * SoLuong;
    }
}
