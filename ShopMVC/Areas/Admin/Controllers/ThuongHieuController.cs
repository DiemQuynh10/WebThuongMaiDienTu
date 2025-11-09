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
            _ => query.OrderBy(x => x.Ten) // name_asc
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


    public IActionResult Create() => View(new ThuongHieu { HienThi = true });
        [HttpPost]
        public async Task<IActionResult> Create(ThuongHieu m)
        {
            if (!ModelState.IsValid) return View(m);
            _db.Add(m); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var th = await _db.ThuongHieus.FindAsync(id);
            return th == null ? NotFound() : View(th);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(ThuongHieu m)
        {
            if (!ModelState.IsValid) return View(m);
            _db.Update(m); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
        }

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

            // kiểm tra ràng buộc
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
            catch (Exception)
            {
                TempData["Err"] = "Xoá thất bại do ràng buộc dữ liệu.";
            }
            return RedirectToAction(nameof(Index));
        }

    }
}
