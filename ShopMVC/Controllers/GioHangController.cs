using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Helpers;
using ShopMVC.Models.ViewModels;

namespace ShopMVC.Controllers
{
    public class GioHangController : Controller
    {
        private readonly AppDbContext _db;
        private const string CART_KEY = "CART";

        public GioHangController(AppDbContext db) => _db = db;

        private List<GioHangItem> LayGio()
            => HttpContext.Session.GetObject<List<GioHangItem>>(CART_KEY) ?? new List<GioHangItem>();

        private void LuuGio(List<GioHangItem> gio)
            => HttpContext.Session.SetObject(CART_KEY, gio);

        // GET: /GioHang
        public IActionResult Index()
        {
            var gio = LayGio();
            ViewBag.TamTinh = gio.Sum(x => x.ThanhTien);
            return View(gio);
        }

        // POST: /GioHang/Them/5?soLuong=1
        [HttpPost]
        public async Task<IActionResult> Them(int id, int soLuong = 1)
        {
            var sp = await _db.SanPhams.Include(p => p.Anhs).FirstOrDefaultAsync(p => p.Id == id);
            if (sp == null || sp.TrangThai == Models.TrangThaiHienThi.An) return NotFound();

            var gio = LayGio();
            var item = gio.FirstOrDefault(x => x.IdSanPham == id);
            var donGia = sp.GiaKhuyenMai ?? sp.Gia;

            if (item == null)
            {
                if (soLuong > sp.TonKho) soLuong = sp.TonKho;
                gio.Add(new GioHangItem
                {
                    IdSanPham = sp.Id,
                    Ten = sp.Ten,
                    DonGia = donGia,
                    SoLuong = Math.Max(1, soLuong),
                    Anh = sp.Anhs.OrderBy(a => a.LaAnhChinh ? 0 : 1).ThenBy(a => a.ThuTu).FirstOrDefault()?.Url
                });
            }
            else
            {
                item.SoLuong = Math.Min(item.SoLuong + soLuong, sp.TonKho);
                item.DonGia = donGia; // sync giá nếu có khuyến mãi mới
            }
            TempData["toast"] = "Đã thêm vào giỏ!";
            LuuGio(gio);
            return RedirectToAction(nameof(Index));
        }

        // POST: /GioHang/Tang/5
        [HttpPost]
        public async Task<IActionResult> Tang(int id)
        {
            var sp = await _db.SanPhams.FindAsync(id);
            if (sp == null) return NotFound();
            var gio = LayGio();
            var item = gio.FirstOrDefault(x => x.IdSanPham == id);
            if (item != null) item.SoLuong = Math.Min(item.SoLuong + 1, sp.TonKho);
            LuuGio(gio);
            return RedirectToAction(nameof(Index));
        }

        // POST: /GioHang/Giam/5
        [HttpPost]
        public IActionResult Giam(int id)
        {
            var gio = LayGio();
            var item = gio.FirstOrDefault(x => x.IdSanPham == id);
            if (item != null)
            {
                item.SoLuong -= 1;
                if (item.SoLuong <= 0) gio.Remove(item);
            }
            LuuGio(gio);
            return RedirectToAction(nameof(Index));
        }

        // POST: /GioHang/Xoa/5
        [HttpPost]
        public IActionResult Xoa(int id)
        {
            var gio = LayGio();
            gio.RemoveAll(x => x.IdSanPham == id);
            LuuGio(gio);
            return RedirectToAction(nameof(Index));
        }

        // POST: /GioHang/XoaHet
        [HttpPost]
        public IActionResult XoaHet()
        {
            HttpContext.Session.Remove(CART_KEY);
            return RedirectToAction(nameof(Index));
        }
    }
}
