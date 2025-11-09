using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;

namespace ShopMVC.Controllers
{
    [Authorize]
    public class DonHangController : Controller
    {
        private readonly AppDbContext _db;
        public DonHangController(AppDbContext db) => _db = db;

        // /DonHang/CuaToi
        public async Task<IActionResult> CuaToi()
        {
            var userName = User.Identity!.Name!;
            var userId = await _db.Users.Where(u => u.UserName == userName).Select(u => u.Id).FirstAsync();

            var ds = await _db.DonHangs
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            return View(ds);
        }

        // /DonHang/ChiTiet/5
        public async Task<IActionResult> ChiTiet(int id)
        {
            var userName = User.Identity!.Name!;
            var userId = await _db.Users.Where(u => u.UserName == userName).Select(u => u.Id).FirstAsync();

            var don = await _db.DonHangs
                .Include(d => d.ChiTiets)
                .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (don == null) return NotFound();
            return View(don);
        }
    }
}
