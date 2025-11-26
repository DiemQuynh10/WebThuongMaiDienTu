using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Models;

namespace ShopMVC.Areas.Admin.Controllers
{
    public class ThuongHieuController : AdminBaseController
    {
        private readonly AppDbContext _db;
        public ThuongHieuController(AppDbContext db) => _db = db;

        // INDEX
        public async Task<IActionResult> Index(int page = 1, int pageSize = 12, string? q = null, string sort = "name_asc")
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 12;

            IQueryable<ThuongHieu> query = _db.ThuongHieus.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                string kw = q.Trim();
                query = query.Where(x => x.Ten.Contains(kw));
            }

            query = sort switch
            {
                "name_desc" => query.OrderByDescending(x => x.Ten),
                "id_desc" => query.OrderByDescending(x => x.Id),
                "id_asc" => query.OrderBy(x => x.Id),
                _ => query.OrderBy(x => x.Ten)
            };

            int total = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = totalPages;
            ViewBag.Q = q;
            ViewBag.Sort = sort;

            return View(items);
        }

        // CREATE GET
        public IActionResult Create() => View(new ThuongHieu { HienThi = true });

        // CREATE POST
        [HttpPost]
        public async Task<IActionResult> Create(ThuongHieu m)
        {
            if (!ModelState.IsValid) return View(m);

            // --- CHECK TRÙNG ---
            string name = (m.Ten ?? "").Trim().ToLower();

            bool existed = await _db.ThuongHieus
                .AnyAsync(x => x.Ten.ToLower() == name);

            if (existed)
            {
                ModelState.AddModelError("Ten", "Tên thương hiệu đã tồn tại.");
                return View(m);
            }
            // ---------------------

            _db.Add(m);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int id)
        {
            var th = await _db.ThuongHieus.FindAsync(id);
            return th == null ? NotFound() : View(th);
        }

        // EDIT POST
        [HttpPost]
        public async Task<IActionResult> Edit(ThuongHieu m)
        {
            if (!ModelState.IsValid) return View(m);

            // --- CHECK TRÙNG (trừ chính nó) ---
            string name = (m.Ten ?? "").Trim().ToLower();

            bool existed = await _db.ThuongHieus
                .AnyAsync(x =>
                    x.Id != m.Id &&
                    x.Ten.ToLower() == name
                );

            if (existed)
            {
                ModelState.AddModelError("Ten", "Tên thương hiệu đã tồn tại.");
                return View(m);
            }
            // ------------------------------------

            _db.Update(m);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var th = await _db.ThuongHieus.FindAsync(id);
            if (th == null)
            {
                TempData["Err"] = "Thương hiệu không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            bool inUse = await _db.SanPhams.AnyAsync(p => p.IdThuongHieu == id);
            if (inUse)
            {
                TempData["Err"] = "Không thể xoá vì còn sản phẩm thuộc thương hiệu này. Hãy chuyển sang thương hiệu khác hoặc xoá sản phẩm trước.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _db.ThuongHieus.Remove(th);
                await _db.SaveChangesAsync();
                TempData["Ok"] = "Đã xoá thương hiệu.";
            }
            catch
            {
                TempData["Err"] = "Xoá thất bại do ràng buộc dữ liệu.";
            }

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var th = await _db.ThuongHieus.FindAsync(id);
            if (th == null)
            {
                TempData["Err"] = "Thương hiệu không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            th.HienThi = !th.HienThi;
            await _db.SaveChangesAsync();

            TempData["Ok"] = th.HienThi
                ? "Đã bật hiển thị thương hiệu."
                : "Đã tắt hiển thị thương hiệu.";
            return RedirectToAction(nameof(Index));
        }
    }
}
