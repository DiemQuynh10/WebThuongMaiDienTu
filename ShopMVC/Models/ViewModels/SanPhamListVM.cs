using ShopMVC.Models;
using System.ComponentModel.DataAnnotations;

namespace ShopMVC.Models.ViewModels
{
    public class SanPhamListVM
    {
        // dữ liệu hiển thị
        public IEnumerable<SanPham> Items { get; set; } = Enumerable.Empty<SanPham>();
        public IEnumerable<DanhMuc> DanhMucs { get; set; } = Enumerable.Empty<DanhMuc>();
        public IEnumerable<ThuongHieu> ThuongHieus { get; set; } = Enumerable.Empty<ThuongHieu>();

        // filter đầu vào
        public int? IdDanhMuc { get; set; }
        public int? IdThuongHieu { get; set; }
        public decimal? GiaMin { get; set; }
        public decimal? GiaMax { get; set; }
        public string? TuKhoa { get; set; }
        public string? SapXep { get; set; } // "moi", "banchay", "gia-asc", "gia-desc"

        // phân trang
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }
}
