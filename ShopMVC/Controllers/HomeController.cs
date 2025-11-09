using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;

namespace ShopMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        public HomeController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            // Danh mục: lấy những mục được bật hiển thị
            ViewBag.DanhMucs = await _db.DanhMucs
                .Where(d => d.HienThi)
                .OrderBy(d => d.ThuTu).ThenBy(d => d.Ten)
                .Take(16)
                .ToListAsync();

            // Nổi bật: ưu tiên bán chạy 30 ngày, nếu chưa có đơn thì fallback LaNoiBat
            var to = DateTime.UtcNow;
            var from = to.AddDays(-30);

            var topIds = await _db.DonHangChiTiets
                .Where(ct => ct.DonHang.TrangThai == Models.TrangThaiDonHang.HoanTat
                          && ct.DonHang.NgayDat >= from && ct.DonHang.NgayDat <= to)
                .GroupBy(ct => ct.IdSanPham)
                .Select(g => new { Id = g.Key, Qty = g.Sum(x => x.SoLuong) })
                .OrderByDescending(x => x.Qty)
                .Take(8)
                .Select(x => x.Id)
                .ToListAsync();

            List<ShopMVC.Models.SanPham> noiBat;
            if (topIds.Any())
            {
                var dict = await _db.SanPhams
                    .Include(p => p.Anhs).Include(p => p.ThuongHieu)
                    .Where(p => topIds.Contains(p.Id) && p.IsActive && p.TrangThai == Models.TrangThaiHienThi.Hien)
                    .ToDictionaryAsync(p => p.Id, p => p);

                noiBat = topIds.Where(id => dict.ContainsKey(id)).Select(id => dict[id]).ToList();
            }
            else
            {
                noiBat = await _db.SanPhams
                    .Include(p => p.Anhs).Include(p => p.ThuongHieu)
                    .Where(p => p.LaNoiBat && p.IsActive && p.TrangThai == Models.TrangThaiHienThi.Hien)
                    .OrderByDescending(p => p.Id).Take(8).ToListAsync();
            }

            ViewBag.NoiBat = noiBat;
            return View();
        }
    }
}
