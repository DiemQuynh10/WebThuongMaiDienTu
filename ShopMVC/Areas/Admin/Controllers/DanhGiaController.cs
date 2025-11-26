using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Models;

[Area("Admin")]
[Authorize(Roles = "QuanTri")] 
public class DanhGiaController : Controller
{
    private readonly AppDbContext _db;
    public DanhGiaController(AppDbContext db) => _db = db;

    // lọc theo trạng thái
    // keyword và onlyFeatured để lọc đánh giá nổi bật
    public async Task<IActionResult> Index(TrangThaiDanhGia? trangThai, string? keyword, bool? onlyFeatured)
    {
        var query = _db.DanhGias
            .Include(d => d.SanPham)
            .OrderByDescending(d => d.NgayTao)
            .AsQueryable();

        // Lọc theo trạng thái 
        if (trangThai != null)
        {
            query = query.Where(d => d.TrangThai == trangThai);
            ViewBag.CurrentFilter = trangThai.ToString();
        }

        // Lọc theo từ khoá (tên SP / nội dung / user)
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(d =>
                   (d.NoiDung ?? "").ToLower().Contains(kw)
                || (d.SanPham != null && d.SanPham.TenDayDu.ToLower().Contains(kw))
                || d.UserId.ToLower().Contains(kw));
            ViewBag.Keyword = keyword;
        }

        // Chỉ xem đánh giá nổi bật
        if (onlyFeatured == true)
        {
            query = query.Where(d => d.LaNoiBat);
            ViewBag.OnlyFeatured = true;
        }

        var list = await query.ToListAsync();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duyet(int id)
    {
        var dg = await _db.DanhGias.FindAsync(id);
        if (dg != null)
        {
            dg.TrangThai = TrangThaiDanhGia.DaDuyet;
            await _db.SaveChangesAsync();
            TempData["success"] = "Đã duyệt đánh giá.";
        }
        else
        {
            TempData["error"] = "Không tìm thấy đánh giá.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(int id)
    {
        var dg = await _db.DanhGias.FindAsync(id);
        if (dg != null)
        {
            dg.TrangThai = TrangThaiDanhGia.TuChoi;
            dg.LaNoiBat = false; // đã từ chối thì không còn nổi bật
            await _db.SaveChangesAsync();
            TempData["success"] = "Đã từ chối đánh giá.";
        }
        else
        {
            TempData["error"] = "Không tìm thấy đánh giá.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleNoiBat(int id)
    {
        var dg = await _db.DanhGias.FindAsync(id);
        if (dg == null)
        {
            TempData["error"] = "Không tìm thấy đánh giá.";
            return RedirectToAction(nameof(Index));
        }

        // Chỉ cho đặt nổi bật nếu đánh giá đã được duyệt
        if (dg.TrangThai != TrangThaiDanhGia.DaDuyet)
        {
            TempData["error"] = "Chỉ có thể đặt nổi bật với đánh giá đã duyệt.";
            return RedirectToAction(nameof(Index));
        }

        // Nếu đang set thành nổi bật -> check giới hạn mỗi sản phẩm tối đa 3 đánh giá nổi bật
        if (!dg.LaNoiBat)
        {
            int featuredCount = await _db.DanhGias
                .CountAsync(x => x.IdSanPham == dg.IdSanPham
                              && x.TrangThai == TrangThaiDanhGia.DaDuyet
                              && x.LaNoiBat);

            if (featuredCount >= 3)
            {
                TempData["error"] = "Mỗi sản phẩm chỉ được tối đa 3 đánh giá nổi bật.";
                return RedirectToAction(nameof(Index));
            }
        }

        dg.LaNoiBat = !dg.LaNoiBat;
        await _db.SaveChangesAsync();

        TempData["success"] = dg.LaNoiBat
            ? "Đã đặt đánh giá này là NỔI BẬT."
            : "Đã bỏ nổi bật đánh giá.";

        return RedirectToAction(nameof(Index));
    }
}
