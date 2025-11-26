using System.ComponentModel.DataAnnotations;

namespace ShopMVC.Models
{
    public class Banner
    {
        [Key]
        public int Id { get; set; }

        public string? TenBanner { get; set; } // Tên để quản lý cho dễ

        public string? HinhAnh { get; set; }   // Lưu tên file ảnh (ví dụ: banner1.jpg)

        public int ThuTu { get; set; }         // Để sắp xếp banner nào hiện trước

        public bool HienThi { get; set; }      // Ẩn/Hiện banner
    }
}