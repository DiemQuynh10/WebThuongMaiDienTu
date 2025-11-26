using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Helpers;
using ShopMVC.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopMVC.Controllers
{
    public class GioHangController : Controller
    {
        private readonly AppDbContext _db;
        private const string CART_KEY = "CART";
        private const string DIRECT_CART_KEY = "DIRECT_CART"; // Thêm key riêng cho Mua Ngay

        public GioHangController(AppDbContext db) => _db = db;

        // Helper lấy giỏ hàng từ Session
        private List<GioHangItem> LayGio()
            => HttpContext.Session.GetObject<List<GioHangItem>>(CART_KEY) ?? new List<GioHangItem>();

        // Helper lưu giỏ hàng vào Session
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

            // --- Helper kiểm tra AJAX ---
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            // ---------------------------

            if (sp == null || sp.TrangThai == Models.TrangThaiHienThi.An)
            {
                if (isAjax)
                    return Json(new { success = false, message = "Sản phẩm không tìm thấy." });
                return NotFound();
            }

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
                // Đảm bảo không vượt quá tồn kho
                int newQty = item.SoLuong + soLuong;
                item.SoLuong = Math.Min(newQty, sp.TonKho);
                item.DonGia = donGia;
            }

            LuuGio(gio);

            // Trả về JSON cho AJAX
            if (isAjax)
            {
                var totalItems = gio.Sum(i => i.SoLuong);
                return Json(new
                {
                    success = true,
                    message = "Đã thêm sản phẩm vào giỏ!",
                    totalItems = totalItems
                });
            }

            // Fallback
            TempData["toast"] = "Đã thêm thành công!";
            string refererUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(refererUrl)) return Redirect(refererUrl);
            return RedirectToAction("Index", "Home");
        }

        // ========================================================
        // ACTION MUA NGAY (Đã Fix lỗi Include ảnh)
        // ========================================================
        [HttpGet] // Dùng GET để có thể gọi từ đường dẫn URL hoặc window.location.href
        public IActionResult MuaNgay(int id, int quantity = 1)
        {
            // Thêm Include(p => p.Anhs) để tránh lỗi Null Reference khi lấy ảnh
            var sp = _db.SanPhams.Include(p => p.Anhs).FirstOrDefault(p => p.Id == id);

            if (sp == null) return NotFound();

            // Tạo 1 list giỏ hàng "tạm" chỉ chứa đúng sản phẩm này
            var directItem = new GioHangItem
            {
                IdSanPham = sp.Id,
                Ten = sp.Ten,
                // Lấy ảnh đại diện an toàn
                Anh = sp.Anhs.OrderBy(a => a.LaAnhChinh ? 0 : 1).FirstOrDefault()?.Url ?? "no-image.jpg",
                DonGia = sp.Gia, // Giá gốc, qua bên Checkout logic Flash Sale sẽ tự giảm
                SoLuong = Math.Max(1, quantity)
            };

            var directCart = new List<GioHangItem> { directItem };

            // Lưu vào Session KHÁC với giỏ hàng chính ("DIRECT_CART")
            HttpContext.Session.SetObject(DIRECT_CART_KEY, directCart);

            // Chuyển hướng sang Checkout kèm cờ isDirect=true
            return RedirectToAction("Checkout", "DonHang", new { isDirect = true });
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