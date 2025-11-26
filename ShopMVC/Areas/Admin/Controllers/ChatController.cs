using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;

namespace ShopMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "QuanTri")]
    public class ChatController : Controller
    {
        private readonly AppDbContext _db;
        public ChatController(AppDbContext db) => _db = db;

        // Trang danh sách các cuộc trò chuyện
        public async Task<IActionResult> Index()
        {
            var sessions = await _db.ChatSessions
                .Include(s => s.User)
                .Include(s => s.Messages) // Cần Include Messages để check Count
                .Where(s => !s.DaDong && s.Messages.Count > 0) // FIX: CHỈ HIỆN KHI CÓ TIN NHẮN
                .OrderByDescending(s => s.ThoiGianTao) 
                .ToListAsync();

            return View(sessions);
        }

        public async Task<IActionResult> ChiTiet(int id)
        {
            var session = await _db.ChatSessions
                .Include(s => s.Messages)
                .Include(s => s.User)
                .Include(s => s.SanPham)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null) return NotFound();

            return View(session);
        }
    }
}