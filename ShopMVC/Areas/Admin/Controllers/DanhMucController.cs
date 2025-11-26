using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Models;

namespace ShopMVC.Areas.Admin.Controllers
{
    public class DanhMucController : AdminBaseController
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public DanhMucController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db; _env = env;
        }

        public async Task<IActionResult> Index()
            => View(await _db.DanhMucs.OrderBy(x => x.ThuTu).ToListAsync());

        public IActionResult Create() => View(new DanhMuc { HienThi = true });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DanhMuc m, IFormFile? icon)
        {
            if (!ModelState.IsValid) return View(m);

            // --- CHECK TRÙNG TÊN / SLUG ---
            var name = (m.Ten ?? "").Trim().ToLower();
            var slug = (m.Slug ?? "").Trim().ToLower();

            bool existed = await _db.DanhMucs
                .AnyAsync(x =>
                    x.Ten.ToLower() == name ||
                    x.Slug.ToLower() == slug
                );

            if (existed)
            {
                ModelState.AddModelError("Ten", "Tên hoặc slug danh mục đã tồn tại.");
                return View(m);
            }
            // --- HẾT PHẦN CHECK TRÙNG ---

            // Lưu icon nếu có
            if (icon != null && icon.Length > 0)
            {
                var ext = Path.GetExtension(icon.FileName).ToLowerInvariant();
                var allow = new[] { ".png", ".jpg", ".jpeg", ".webp" };
                if (!allow.Contains(ext)) ModelState.AddModelError("", "Chỉ hỗ trợ PNG/JPG/WEBP.");
                if (!ModelState.IsValid) return View(m);

                var fileName = $"{(string.IsNullOrWhiteSpace(m.Slug) ? $"dm-{Guid.NewGuid():N}" : m.Slug.ToLower())}{ext}";
                var folder = Path.Combine(_env.WebRootPath, "images", "categories");
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, fileName);
                using var s = System.IO.File.Create(path);
                await icon.CopyToAsync(s);
                m.IconUrl = $"/images/categories/{fileName}";
            }

            _db.Add(m);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var dm = await _db.DanhMucs.FindAsync(id);
            return dm == null ? NotFound() : View(dm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DanhMuc m, IFormFile? icon)
        {
            if (!ModelState.IsValid) return View(m);

            // --- CHECK TRÙNG TÊN / SLUG (TRỪ CHÍNH NÓ) ---
            var name = (m.Ten ?? "").Trim().ToLower();
            var slug = (m.Slug ?? "").Trim().ToLower();

            bool existed = await _db.DanhMucs
                .AnyAsync(x =>
                    x.Id != m.Id &&      
                    (
                        x.Ten.ToLower() == name ||
                        x.Slug.ToLower() == slug
                    )
                );

            if (existed)
            {
                ModelState.AddModelError("Ten", "Tên hoặc slug danh mục đã tồn tại.");
                return View(m);
            }

            // Nếu upload icon mới → ghi đè
            if (icon != null && icon.Length > 0)
            {
                var ext = Path.GetExtension(icon.FileName).ToLowerInvariant();
                var allow = new[] { ".png", ".jpg", ".jpeg", ".webp" };
                if (!allow.Contains(ext)) ModelState.AddModelError("", "Chỉ hỗ trợ PNG/JPG/WEBP.");
                if (!ModelState.IsValid) return View(m);

                var fileName = $"{(string.IsNullOrWhiteSpace(m.Slug) ? $"dm-{Guid.NewGuid():N}" : m.Slug.ToLower())}{ext}";
                var folder = Path.Combine(_env.WebRootPath, "images", "categories");
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, fileName);
                using var s = System.IO.File.Create(path);
                await icon.CopyToAsync(s);
                m.IconUrl = $"/images/categories/{fileName}";
            }

            _db.Update(m);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // tìm danh mục
            var dm = await _db.DanhMucs.FindAsync(id);
            if (dm == null)
            {
                TempData["Error"] = "Danh mục không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            // kiểm tra còn sản phẩm thuộc danh mục này không
            bool hasProducts = await _db.SanPhams.AnyAsync(p => p.IdDanhMuc == id);
            if (hasProducts)
            {
                TempData["Error"] = "Danh mục đang có sản phẩm, không thể xoá. " +
                                    "Hãy chuyển sản phẩm sang danh mục khác hoặc xoá sản phẩm trước.";
                return RedirectToAction(nameof(Index));
            }

            // nếu không có sản phẩm thì cho xoá
            _db.DanhMucs.Remove(dm);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã xoá danh mục.";
            return RedirectToAction(nameof(Index));
        }
    }
}
