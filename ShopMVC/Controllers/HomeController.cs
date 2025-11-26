using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Models;
using System.Diagnostics; // Dùng cho ErrorViewModel

namespace ShopMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        // Constructor gọn gàng như của bạn
        public HomeController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            // 1. === FIX PHẦN BANNER SLIDER ===
            var banners = await _db.Banners
                           .Where(b => b.HienThi == true)
                           .OrderBy(b => b.ThuTu)
                           .ToListAsync();

            // QUAN TRỌNG: Đã đổi tên thành BannerSlider để khớp với code bên View
            ViewBag.BannerSlider = banners;


            // 2. === DANH MỤC (Code cũ giữ nguyên) ===
            ViewBag.DanhMucs = await _db.DanhMucs
                .Where(d => d.HienThi)
                .OrderBy(d => d.ThuTu).ThenBy(d => d.Ten)
                .Take(16)
                .ToListAsync();


            // 3. === ĐÁNH GIÁ (Code cũ giữ nguyên) ===
            var danhGias = await _db.DanhGias
                .Where(d => d.TrangThai == TrangThaiDanhGia.DaDuyet)
                .OrderByDescending(d => d.NgayTao)
                .Take(5)
                .ToListAsync();
            ViewBag.DanhGias = danhGias;


            // 4. === SẢN PHẨM NỔI BẬT (Code cũ giữ nguyên) ===
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
            var featuredReviews = await _db.DanhGias
            .Include(d => d.SanPham)
            .Where(d => d.TrangThai == TrangThaiDanhGia.DaDuyet && d.LaNoiBat)
            .OrderByDescending(d => d.NgayTao)
            .Take(6) // tối đa 6 review
            .ToListAsync();

            ViewBag.DanhGias = featuredReviews;

            return View();
        }

        // Các Action mặc định thường có (Privacy, Error), tớ để lại phòng khi cần dùng
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}