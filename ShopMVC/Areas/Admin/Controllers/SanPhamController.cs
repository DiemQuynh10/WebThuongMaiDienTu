using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Models;

namespace ShopMVC.Areas.Admin.Controllers
{
    public class SanPhamController : AdminBaseController
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        public SanPhamController(AppDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

        // ===== Select lists (DM, TH, Cha) =====
        private void LoadSelects()
        {
            ViewBag.DanhMuc = new SelectList(_db.DanhMucs.OrderBy(x => x.ThuTu), "Id", "Ten");
            ViewBag.ThuongHieu = new SelectList(_db.ThuongHieus.OrderBy(x => x.Ten), "Id", "Ten");

            // Cha = sản phẩm không có ParentId
            ViewBag.Parents = new SelectList(
                _db.SanPhams
                   .Where(p => p.ParentId == null)
                   .OrderBy(p => p.Ten)
                   .Select(p => new { p.Id, Ten = p.Ten }),
                "Id", "Ten"
            );
        }

        // ===== Danh sách =====
        public async Task<IActionResult> Index(int page = 1, int pageSize = 15, bool includeVariants = false)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 15;

            IQueryable<SanPham> q = _db.SanPhams.AsNoTracking();

            if (!includeVariants)
                q = q.Where(p => p.ParentId == null)
                     .Include(p => p.Children);

            q = q
                .Include(p => p.Anhs)
                .Include(p => p.Parent)               // thêm Parent
                    .ThenInclude(pa => pa.Anhs)       // và ảnh của Parent
                .Include(p => p.DanhMuc)
                .Include(p => p.ThuongHieu);


            int total = await q.CountAsync();
            int totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var list = await q
                .OrderByDescending(p => p.NgayCapNhat)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.IncludeVariants = includeVariants;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = totalPages;

            return View(list);
        }


        public IActionResult Create()
        {
            LoadSelects();
            return View(new SanPham { TrangThai = TrangThaiHienThi.Hien });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SanPham m, List<IFormFile>? files)
        {
            if (m.ParentId.HasValue)
            {
                var parentExists = await _db.SanPhams.AnyAsync(x => x.Id == m.ParentId && x.ParentId == null);
                if (!parentExists)
                    ModelState.AddModelError(nameof(m.ParentId), "Nhóm (cha) không hợp lệ hoặc không tồn tại.");
            }

            // ====== DEBUG MODELSTATE ======
            if (!ModelState.IsValid)
            {
                // Thu thập các lỗi để xem chi tiết
                var errors = ModelState
                    .Where(kv => kv.Value.Errors.Count > 0)
                    .Select(kv => new
                    {
                        Field = kv.Key,
                        Messages = kv.Value.Errors.Select(e => e.ErrorMessage).ToList()
                    })
                    .ToList();

                // (Tuỳ chọn) Ghi log vào console:
                Console.WriteLine("===== MODELSTATE ERRORS (Create) =====");
                foreach (var err in errors)
                {
                    Console.WriteLine($"Field: {err.Field}");
                    foreach (var msg in err.Messages)
                        Console.WriteLine($" - {msg}");
                }

                LoadSelects();
                return View(m);      // quay lại form
            }
            // ===============================

            m.NgayTao = m.NgayCapNhat = DateTime.UtcNow;
            _db.Add(m);
            await _db.SaveChangesAsync();

            await SaveImagesAsync(m.Id, files);
            TempData["toast"] = "Đã thêm sản phẩm mới.";
            return RedirectToAction(nameof(Index));
        }
 

        // ===== Edit =====
        public async Task<IActionResult> Edit(int id)
        {
            LoadSelects();
            var sp = await _db.SanPhams
                .Include(p => p.Anhs)
                .Include(p => p.Parent)
                .Include(p => p.Children)
                .FirstOrDefaultAsync(p => p.Id == id);
            return sp == null ? NotFound() : View(sp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SanPham m, List<IFormFile>? files)
        {
            if (m.ParentId == m.Id) m.ParentId = null;

            if (m.ParentId.HasValue)
            {
                var parent = await _db.SanPhams.FirstOrDefaultAsync(
                    x => x.Id == m.ParentId && x.ParentId == null);
                if (parent == null)
                    ModelState.AddModelError(nameof(m.ParentId), "Nhóm (cha) không hợp lệ hoặc không tồn tại.");
            }

            // ====== DEBUG MODELSTATE ======
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kv => kv.Value.Errors.Count > 0)
                    .Select(kv => new
                    {
                        Field = kv.Key,
                        Messages = kv.Value.Errors.Select(e => e.ErrorMessage).ToList()
                    })
                    .ToList();

                Console.WriteLine("===== MODELSTATE ERRORS (Edit) =====");
                foreach (var err in errors)
                {
                    Console.WriteLine($"Field: {err.Field}");
                    foreach (var msg in err.Messages)
                        Console.WriteLine($" - {msg}");
                }

                LoadSelects();
                return View(m);
            }
            // ===============================

            m.NgayCapNhat = DateTime.UtcNow;

            _db.Update(m);
            await _db.SaveChangesAsync();

            await SaveImagesAsync(m.Id, files);

            TempData["toast"] = "Đã lưu sản phẩm thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ===== Delete sản phẩm =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Xoa(int id)
        {
            var sp = await _db.SanPhams
                .Include(p => p.Children)
                .Include(p => p.Anhs)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (sp == null) return RedirectToAction(nameof(Index));

            // Nếu là cha và còn biến thể → chặn xoá (tránh xoá nhầm)
            if (sp.ParentId == null && sp.Children.Any())
            {
                TempData["Err"] = "Không thể xoá sản phẩm CHA khi vẫn còn biến thể. Hãy xoá/di chuyển các biến thể trước.";
                return RedirectToAction(nameof(Index));
            }

            // Xoá ảnh file vật lý
            foreach (var a in sp.Anhs) TryDeleteFile(a.Url);

            _db.SanPhams.Remove(sp);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Đã xoá sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> XoaAnh(int id)
        {
            var a = await _db.AnhSanPhams.FindAsync(id);
            if (a != null)
            {
                TryDeleteFile(a.Url);
                _db.AnhSanPhams.Remove(a); await _db.SaveChangesAsync();
            }
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        public async Task<IActionResult> DatAnhChinh(int id)
        {
            var a = await _db.AnhSanPhams.FindAsync(id);
            if (a == null) return NotFound();
            var anhs = _db.AnhSanPhams.Where(x => x.IdSanPham == a.IdSanPham);
            await anhs.ForEachAsync(x => x.LaAnhChinh = false);
            a.LaAnhChinh = true;
            await _db.SaveChangesAsync();
            return Redirect(Request.Headers["Referer"].ToString());
        }


        public async Task<IActionResult> Images(int id)
        {
            var p = await _db.SanPhams
                .Include(x => x.Anhs.OrderBy(a => a.ThuTu))
                .FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();
            return View(p);
        }

        // Upload nhiều ảnh 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImages(int id, List<IFormFile> files)
        {
            var p = await _db.SanPhams.Include(x => x.Anhs).FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();

            var uploadRoot = Path.Combine(_env.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadRoot);

            int nextOrder = p.Anhs.Any() ? p.Anhs.Max(a => a.ThuTu) + 1 : 0;

            foreach (var f in files.Where(f => f?.Length > 0))
            {
                var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
                var ok = ext is ".jpg" or ".jpeg" or ".png" or ".webp";
                if (!ok) continue;

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var savePath = Path.Combine(uploadRoot, fileName);
                await using (var stream = System.IO.File.Create(savePath))
                    await f.CopyToAsync(stream);

                var relUrl = $"/uploads/products/{fileName}";
                _db.AnhSanPhams.Add(new AnhSanPham
                {
                    IdSanPham = id,
                    Url = relUrl,
                    ThuTu = nextOrder++,
                    LaAnhChinh = !p.Anhs.Any()
                });
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Images), new { id });
        }

        // Đặt ảnh chính
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetMainImage(int id, int imageId)
        {
            var p = await _db.SanPhams.Include(x => x.Anhs).FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();

            foreach (var a in p.Anhs) a.LaAnhChinh = (a.Id == imageId);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Images), new { id });
        }

        // Xóa ảnh (kèm file)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id, int imageId)
        {
            var img = await _db.AnhSanPhams.FirstOrDefaultAsync(a => a.Id == imageId && a.IdSanPham == id);
            if (img == null) return RedirectToAction(nameof(Images), new { id });

            var physical = Path.Combine(_env.WebRootPath, img.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);

            _db.AnhSanPhams.Remove(img);
            await _db.SaveChangesAsync();

            var remain = await _db.AnhSanPhams.Where(a => a.IdSanPham == id).OrderBy(a => a.ThuTu).ToListAsync();
            if (remain.Any() && !remain.Any(a => a.LaAnhChinh))
            {
                remain.First().LaAnhChinh = true;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Images), new { id });
        }

        // Sắp xếp ảnh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveImage(int id, int imageId, string dir)
        {
            var list = await _db.AnhSanPhams
                .Where(a => a.IdSanPham == id)
                .OrderBy(a => a.ThuTu).ToListAsync();

            var idx = list.FindIndex(a => a.Id == imageId);
            if (idx == -1) return RedirectToAction(nameof(Images), new { id });

            int swapWith = dir == "up" ? idx - 1 : idx + 1;
            if (swapWith < 0 || swapWith >= list.Count) return RedirectToAction(nameof(Images), new { id });

            (list[idx].ThuTu, list[swapWith].ThuTu) = (list[swapWith].ThuTu, list[idx].ThuTu);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Images), new { id });
        }

        // ===== Helpers =====
        private async Task SaveImagesAsync(int spId, List<IFormFile>? files)
        {
            if (files == null || files.Count == 0) return;

            // Chuẩn hoá: chỉ dùng /uploads/products
            var dir = Path.Combine(_env.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(dir);

            bool first = !await _db.AnhSanPhams.AnyAsync(x => x.IdSanPham == spId);
            int order = (await _db.AnhSanPhams
    .Where(x => x.IdSanPham == spId)
    .Select(x => (int?)x.ThuTu)
    .MaxAsync()) ?? 0;

            foreach (var f in files.Where(f => f?.Length > 0))
            {
                var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
                var ok = ext is ".jpg" or ".jpeg" or ".png" or ".webp";
                if (!ok) continue;

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var savePath = Path.Combine(dir, fileName);
                await using (var stream = System.IO.File.Create(savePath))
                    await f.CopyToAsync(stream);

                var url = $"/uploads/products/{fileName}";
                _db.AnhSanPhams.Add(new AnhSanPham
                {
                    IdSanPham = spId,
                    Url = url,
                    LaAnhChinh = first,   // ảnh đầu tiên là ảnh chính
                    ThuTu = ++order
                });
                first = false;
            }
            await _db.SaveChangesAsync();
        }

        private void TryDeleteFile(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            var full = Path.Combine(_env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }
    }
}
