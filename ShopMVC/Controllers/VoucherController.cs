using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ShopMVC.Controllers
{
    // Controller phía khách hàng (không phải Admin)
    public class VoucherController : Controller
    {
        private readonly AppDbContext _db;

        public VoucherController(AppDbContext db)
        {
            _db = db;
        }

        // Trang chính hiển thị danh sách voucher thường
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Now.Date;

            var vouchers = await _db.Vouchers
                .Where(v =>
                    v.IsActive &&
                    !v.IsFlashSale &&             // ⚡ loại các Flash Sale, chỉ giữ voucher nhập mã
                    v.NgayBatDau.Date <= today &&
                    v.NgayHetHan.Date >= today &&
                    (v.SoLanSuDungToiDa == 0 || v.SoLanDaSuDung < v.SoLanSuDungToiDa))
                .OrderBy(v => v.NgayHetHan)
                .ToListAsync();

            return View(vouchers);
        }
    }
}
