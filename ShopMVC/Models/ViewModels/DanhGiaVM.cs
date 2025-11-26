using System.ComponentModel.DataAnnotations;

namespace ShopMVC.Models.ViewModels
{
    public class DanhGiaVM
    {
        // Thông tin để hiển thị
        public int IdSanPham { get; set; }
        public int IdDonHang { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string? AnhSanPham { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn chất lượng sản phẩm")]
        [Range(1, 5, ErrorMessage = "Vui lòng chọn chất lượng sản phẩm")]
        public int? SoSao
        {
            get; set;
        }

        [StringLength(1000)]
        public string? NoiDung { get; set; }

        public bool HienThiTen { get; set; } = true;

        // Dùng để nhận file upload
        public IFormFile? FileHinhAnh { get; set; }
    }
}