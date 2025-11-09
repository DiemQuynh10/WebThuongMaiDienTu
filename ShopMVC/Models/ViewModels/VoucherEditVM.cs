using ShopMVC.Models;

namespace ShopMVC.Models.ViewModels
{
    public class VoucherEditVM
    {
        public Voucher Voucher { get; set; } = new();

        // các ID được chọn
        public List<int> SelectedThuongHieuIds { get; set; } = new();
        public List<int> SelectedDanhMucIds { get; set; } = new();

        // dữ liệu hiển thị
        public List<ThuongHieu> AllThuongHieus { get; set; } = new();
        public List<DanhMuc> AllDanhMucs { get; set; } = new();
    }
}
