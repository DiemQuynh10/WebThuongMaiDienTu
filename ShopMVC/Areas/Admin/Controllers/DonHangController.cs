using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Models;
using ShopMVC.Models.Dto;
using ShopMVC.Services;
using ShopMVC.Services.Interfaces;
using System.Security.Claims;

namespace ShopMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "QuanTri,Staff")]
    public class DonHangController : AdminBaseController
    {
        private readonly AppDbContext _db;
        private readonly IOrderService _svc;

        public DonHangController(AppDbContext db, IOrderService svc)
        {
            _db = db;
            _svc = svc;
        }

        public async Task<IActionResult> Index(TrangThaiDonHang? trangThai)
        {
            ViewBag.TrangThai = trangThai;

            var q = _db.DonHangs.AsQueryable();
            if (trangThai.HasValue) q = q.Where(d => d.TrangThai == trangThai.Value);

            var ds = await q.OrderByDescending(d => d.NgayDat).ToListAsync();
            return View(ds);
        }

        public async Task<IActionResult> Details(int id)
        {
            var d = await _db.DonHangs
                .Include(x => x.ChiTiets).ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (d == null) return NotFound();

            // nạp Notes & Logs cho view
            ViewBag.Notes = await _db.DonHangNotes
                .Where(n => n.MaDH == id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            ViewBag.Logs = await _db.DonHangLogs
                .Where(l => l.MaDH == id)
                .OrderByDescending(l => l.At)
                .ToListAsync();

            return View(d);
        }

        // Đổi trạng thái từ danh sách (flow chuẩn + override)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickUpdate(int id, UpdateStatusDto dto)
        {
            var d = await _svc.FindAsync(id);
            if (d == null) return NotFound();

            var from = d.TrangThai;
            var to = dto.To;

            // KHÓA trạng thái kết thúc
            if (from is TrangThaiDonHang.HoanTat or TrangThaiDonHang.DaHuy)
            {
                TempData["toast"] = $"Đơn {d.MaDon} đã kết thúc, không thể thay đổi.";
                return RedirectToAction(nameof(Index));
            }

            if (!dto.IsOverride)
            {
                // Flow chuẩn: chỉ cho bước hợp lệ (không lùi)
                if (!OrderStatusRules.Allowed.TryGetValue(from, out var nexts) || !nexts.Contains(to))
                {
                    TempData["toast"] = $"Không thể chuyển {d.MaDon} từ {from} → {to}.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                // Override: chỉ Admin, chỉ cho lùi, và chỉ khi đang ở ChuẩnBị/ĐangGiao
                if (!(User.IsInRole("Admin") || User.IsInRole("QuanTri")))
                {
                    TempData["toast"] = "Bạn không có quyền override trạng thái.";
                    return RedirectToAction(nameof(Index));
                }
                if (!(from == TrangThaiDonHang.ChuanBi || from == TrangThaiDonHang.DangGiao))
                {
                    TempData["toast"] = "Không được override từ trạng thái hiện tại.";
                    return RedirectToAction(nameof(Index));
                }
                if (to >= from)
                {
                    TempData["toast"] = "Override chỉ dùng để quay lại trạng thái trước.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Bắt buộc lý do với: Huỷ / override / ngoài Allowed
            var needReason = OrderStatusRules.NeedReason(from, to, dto.IsOverride);
            if (needReason && string.IsNullOrWhiteSpace(dto.ReasonCode) && string.IsNullOrWhiteSpace(dto.Note))
            {
                TempData["toast"] = "Vui lòng nhập lý do cho thao tác này.";
                return RedirectToAction(nameof(Index));
            }

            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            await _svc.ChangeStatusAsync(d, dto, userId);

            TempData["toast"] = dto.IsOverride
                ? $"ĐÃ OVERRIDE: {d.MaDon} {from} → {to}."
                : $"Đã cập nhật đơn {d.MaDon} → {to}.";
            return RedirectToAction(nameof(Index));
        }

        // Huỷ đơn (hoàn kho + bắt lý do)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Huy(int id, string? reasonCode, string? note)
        {
            var d = await _svc.FindAsync(id);
            if (d == null) return NotFound();

            // Không cho huỷ khi đang giao/hoàn tất
            if (d.TrangThai == TrangThaiDonHang.DangGiao || d.TrangThai == TrangThaiDonHang.HoanTat)
            {
                TempData["toast"] = $"Đơn {d.MaDon} không thể huỷ ở trạng thái hiện tại.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(reasonCode) && string.IsNullOrWhiteSpace(note))
            {
                TempData["toast"] = "Vui lòng nhập lý do huỷ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            await _svc.CancelAndRestockAsync(d, reasonCode, note, userId);

            TempData["toast"] = $"Đã huỷ đơn {d.MaDon}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Chuyển trạng thái “từng bước” trong trang Details (flow chuẩn)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChuyenTrangThai(int id, TrangThaiDonHang trangThai, string? reasonCode, string? note)
        {
            var d = await _svc.FindAsync(id);
            if (d == null) return NotFound();

            var dto = new UpdateStatusDto
            {
                To = trangThai,
                ReasonCode = reasonCode,
                Note = note,
                NotifyCustomer = false,
                IsOverride = false
            };

            var from = d.TrangThai; var to = dto.To;

            // Flow chuẩn: chỉ cho bước hợp lệ
            if (!OrderStatusRules.Allowed.TryGetValue(from, out var nexts) || !nexts.Contains(to))
            {
                TempData["toast"] = $"Không thể chuyển {d.MaDon} từ {from} → {to}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var needReason = OrderStatusRules.NeedReason(from, to, dto.IsOverride);
            if (needReason && string.IsNullOrWhiteSpace(dto.ReasonCode) && string.IsNullOrWhiteSpace(dto.Note))
            {
                TempData["toast"] = "Vui lòng nhập lý do.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            await _svc.ChangeStatusAsync(d, dto, userId);

            TempData["toast"] = $"Đã cập nhật đơn {d.MaDon} → {to}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Mở lại đơn trong 10 phút kể từ khi Hoàn tất (quay về trạng thái trước Hoàn tất)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(int id, string reasonCode, string? note)
        {
            if (!User.IsInRole("Admin"))
            {
                TempData["toast"] = "Chỉ Admin được phép mở lại đơn.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var ok = await _svc.CanReopenAsync(id);
                if (!ok)
                {
                    TempData["toast"] = "Đơn không đủ điều kiện mở lại (quá thời gian hoặc không ở trạng thái Hoàn tất).";
                    return RedirectToAction(nameof(Index));
                }

                var uid = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                await _svc.ReopenAsync(id, reasonCode, note, uid);

                TempData["toast"] = "Đã mở lại đơn thành công.";
            }
            catch (Exception ex)
            {
                TempData["toast"] = "Không thể mở lại đơn: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(int id, string noiDung)
        {
            if (string.IsNullOrWhiteSpace(noiDung))
                return RedirectToAction(nameof(Details), new { id });

            _db.DonHangNotes.Add(new DonHangNote
            {
                MaDH = id,
                NoiDung = noiDung.Trim(),
                CreatedBy = User.Identity?.Name
            });
            await _db.SaveChangesAsync();

            TempData["toast"] = "Đã thêm ghi chú.";
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Invoice(int id)
        {
            var d = await _db.DonHangs
                .Include(x => x.ChiTiets).ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (d == null) return NotFound();

            return View(d); // View Layout = null; và có nút window.print()
        }
    }
}
