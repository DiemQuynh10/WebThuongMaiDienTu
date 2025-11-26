using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Models;
using System.IO;

namespace ShopMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BannerController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment; // Dùng để lấy đường dẫn lưu ảnh

        public BannerController(AppDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. Danh sách Banner
        public async Task<IActionResult> Index()
        {
            var banners = await _db.Banners.OrderBy(b => b.ThuTu).ToListAsync();
            return View(banners);
        }

        // 2. Trang Thêm mới (Giao diện)
        public IActionResult Create()
        {
            return View();
        }

        // 3. Xử lý Thêm mới (Lưu DB + Upload ảnh)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Banner banner, IFormFile? fileAnh)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh
                if (fileAnh != null)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(fileAnh.FileName);
                    string folderPath = Path.Combine(wwwRootPath, "images", "banners");

                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string fullPath = Path.Combine(folderPath, fileName);
                    using (var fileStream = new FileStream(fullPath, FileMode.Create))
                    {
                        await fileAnh.CopyToAsync(fileStream);
                    }

                    // Lưu tên file vào database
                    banner.HinhAnh = "/images/banners/" + fileName;
                }

                _db.Add(banner);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(banner);
        }

        // 4. Trang sửa Banner (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var banner = await _db.Banners.FindAsync(id);
            if (banner == null)
            {
                return NotFound();
            }

            return View(banner);
        }

        // 5. Xử lý sửa Banner (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Banner banner, IFormFile? fileAnh)
        {
            if (id != banner.Id) 
            {
                return NotFound();
            }

            var bannerDb = await _db.Banners.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (bannerDb == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Nếu có ảnh mới -> upload, xóa ảnh cũ
                if (fileAnh != null)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(fileAnh.FileName);
                    string folderPath = Path.Combine(wwwRootPath, "images", "banners");

                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string fullPath = Path.Combine(folderPath, fileName);
                    using (var fileStream = new FileStream(fullPath, FileMode.Create))
                    {
                        await fileAnh.CopyToAsync(fileStream);
                    }

                    // Xóa file cũ nếu có
                    if (!string.IsNullOrEmpty(bannerDb.HinhAnh))
                    {
                        var oldPath = Path.Combine(
                            wwwRootPath,
                            bannerDb.HinhAnh.TrimStart('/')
                                             .Replace("/", Path.DirectorySeparatorChar.ToString())
                        );

                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    banner.HinhAnh = "/images/banners/" + fileName;
                }
                else
                {
                    banner.HinhAnh = bannerDb.HinhAnh;
                }

                try
                {
                    _db.Update(banner);
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _db.Banners.AnyAsync(b => b.Id == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            return View(banner);
        }

        // 6. Xóa Banner
        public async Task<IActionResult> Delete(int id)
        {
            var banner = await _db.Banners.FindAsync(id);
            if (banner != null)
            {
                // (Optional) Xóa file ảnh
                if (!string.IsNullOrEmpty(banner.HinhAnh))
                {
                    var wwwRootPath = _webHostEnvironment.WebRootPath;
                    var oldPath = Path.Combine(
                        wwwRootPath,
                        banner.HinhAnh.TrimStart('/')
                                      .Replace("/", Path.DirectorySeparatorChar.ToString())
                    );

                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                _db.Banners.Remove(banner);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
