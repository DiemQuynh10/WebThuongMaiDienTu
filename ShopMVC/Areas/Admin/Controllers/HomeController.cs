using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;

namespace ShopMVC.Areas.Admin.Controllers
{
    public class HomeController : AdminBaseController
    {
        private readonly AppDbContext _db;
        public HomeController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.SoSP = await _db.SanPhams.CountAsync();
            ViewBag.SoDH = await _db.DonHangs.CountAsync();
            ViewBag.DoanhThu = await _db.DonHangs
                .Where(d => d.TrangThai == Models.TrangThaiDonHang.HoanTat)
                .SumAsync(d => (decimal?)d.TongThanhToan) ?? 0m;

            return View();
        }
    }
}
