using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ShopMVC.Models;

namespace ShopMVC.Areas.Admin.ViewModels
{
    public class VoucherCreateViewModel
    {
        // --- ID để dùng khi sửa (nếu có) ---
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã voucher")]
        [Display(Name = "Mã voucher")]
        public string Code { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập tên chương trình")]
        [Display(Name = "Tên/ghi chú voucher")]
        public string Ten { get; set; } = "";

        [Display(Name = "% giảm")]
        [Range(0, 100, ErrorMessage = "% giảm từ 0 đến 100")]
        public double? PhanTramGiam { get; set; }

        [Display(Name = "Giảm trực tiếp (đ)")]
        public decimal? GiamTrucTiep { get; set; }

        [Display(Name = "Giảm tối đa (đ)")]
        public decimal? GiamToiDa { get; set; }

        [Required(ErrorMessage = "Chọn ngày bắt đầu")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày bắt đầu")]
        public DateTime NgayBatDau { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Chọn ngày hết hạn")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày hết hạn")]
        public DateTime NgayHetHan { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "Lượt dùng tối đa")]
        public int SoLanSuDungToiDa { get; set; } = 100;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        // --- Dữ liệu để hiển thị Checkbox ---
        public List<ThuongHieu> AvailableBrands { get; set; } = new List<ThuongHieu>();
        public List<DanhMuc> AvailableCategories { get; set; } = new List<DanhMuc>();

        // --- Dữ liệu hứng từ Form (Các ID được tick chọn) ---
        public List<int> SelectedBrandIds { get; set; } = new List<int>();
        public List<int> SelectedCategoryIds { get; set; } = new List<int>();

        // --- MỚI: Hỗ trợ Flash Sale ---
        [Display(Name = "Là chương trình Flash Sale")]
        public bool IsFlashSale { get; set; } = false;

        // Để hỗ trợ Concurrency check khi sửa
        public byte[]? RowVersion { get; set; }
    }
}