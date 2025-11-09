using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Models;
using ShopMVC.Models.Dto;
using ShopMVC.Services.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace ShopMVC.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _db;
        public OrderService(AppDbContext db) => _db = db;

        public Task<DonHang?> FindAsync(int id)
            => _db.DonHangs.Include(d => d.ChiTiets).FirstOrDefaultAsync(o => o.Id == id);

        public async Task ChangeStatusAsync(DonHang order, UpdateStatusDto dto, string? userId = null)
        {
            var from = order.TrangThai;     // BEFORE
            var to = dto.To;
            if (from == to) return;

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1) Log lịch sử trạng thái (giữ nguyên như m đang có)
                _db.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    FromStatus = from,
                    ToStatus = to,
                    ReasonCode = dto.ReasonCode,
                    Note = dto.Note,
                    ChangedByUserId = userId,
                    IsOverride = dto.IsOverride
                });

                // 2) Log nhanh cho trang Details
                _db.DonHangLogs.Add(new DonHangLog
                {
                    MaDH = order.Id,
                    From = from,
                    To = to,
                    ByUser = userId,
                    At = DateTime.UtcNow
                });

                // 3) Hoàn/thu hồi lượt dùng voucher nếu đơn có voucher
                if (order.VoucherId != null)
                {
                    var v = await _db.Vouchers.SingleOrDefaultAsync(x => x.Id == order.VoucherId.Value);
                    if (v != null)
                    {
                        // Từ KHÔNG hủy -> HỦY  => hoàn 1 lượt (không âm)
                        if (from != TrangThaiDonHang.DaHuy && to == TrangThaiDonHang.DaHuy)
                        {
                            if (v.SoLanDaSuDung > 0) v.SoLanDaSuDung -= 1;
                            _db.Vouchers.Update(v);
                        }
                        // Từ HỦY -> KHÔNG hủy  => thu hồi lại 1 lượt
                        else if (from == TrangThaiDonHang.DaHuy && to != TrangThaiDonHang.DaHuy)
                        {
                            v.SoLanDaSuDung += 1;
                            _db.Vouchers.Update(v);
                        }
                    }
                }

                // 4) Cập nhật đơn
                order.TrangThai = to;
                order.NgayCapNhat = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync();
                // Tuỳ m muốn ném lỗi hay add ModelState ở tầng gọi
                throw;
            }
        }



        public async Task CancelAndRestockAsync(DonHang order, string? reasonCode, string? note, string? userId)
        {
            // ghi history
            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = order.TrangThai,
                ToStatus = TrangThaiDonHang.DaHuy,
                ReasonCode = string.IsNullOrWhiteSpace(reasonCode) ? "CANCEL" : reasonCode,
                Note = note,
                ChangedByUserId = userId,
                IsOverride = false
            });

            // cập nhật trạng thái
            order.TrangThai = TrangThaiDonHang.DaHuy;
            order.NgayCapNhat = DateTime.UtcNow;

            // hoàn kho
            var spIds = order.ChiTiets.Select(x => x.IdSanPham).ToList();
            var sps = await _db.SanPhams.Where(p => spIds.Contains(p.Id)).ToListAsync();
            foreach (var ct in order.ChiTiets)
            {
                var sp = sps.First(p => p.Id == ct.IdSanPham);
                sp.TonKho += ct.SoLuong;
            }

            await _db.SaveChangesAsync();
        }

        // (nếu làm “mở lại ≤10’”, giữ 2 hàm dưới)
        public async Task<bool> CanReopenAsync(int orderId)
        {
            var order = await _db.DonHangs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == orderId);
            if (order == null || order.TrangThai != TrangThaiDonHang.HoanTat) return false;

            var lastComplete = await _db.OrderStatusHistories
                .Where(h => h.OrderId == orderId && h.ToStatus == TrangThaiDonHang.HoanTat)
                .OrderByDescending(h => h.ChangedAtUtc)
                .FirstOrDefaultAsync();

            if (lastComplete == null) return false;

            return DateTime.UtcNow - lastComplete.ChangedAtUtc <= OrderPolicy.ReopenWindow;
        }

        public async Task ReopenAsync(int orderId, string reasonCode, string? note, string? userId)
        {
            var order = await _db.DonHangs.Include(d => d.ChiTiets).FirstOrDefaultAsync(x => x.Id == orderId);
            if (order == null) throw new InvalidOperationException("Order not found.");

            var lastComplete = await _db.OrderStatusHistories
                .Where(h => h.OrderId == orderId && h.ToStatus == TrangThaiDonHang.HoanTat)
                .OrderByDescending(h => h.ChangedAtUtc)
                .FirstOrDefaultAsync();

            if (order.TrangThai != TrangThaiDonHang.HoanTat || lastComplete == null)
                throw new InvalidOperationException("Order is not completed.");

            if (DateTime.UtcNow - lastComplete.ChangedAtUtc > OrderPolicy.ReopenWindow)
                throw new InvalidOperationException("Reopen window expired.");

            var prev = lastComplete.FromStatus;

            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = order.TrangThai,
                ToStatus = prev,
                ReasonCode = string.IsNullOrWhiteSpace(reasonCode) ? "REOPEN" : reasonCode,
                Note = note,
                ChangedByUserId = userId,
                IsOverride = true
            });

            order.TrangThai = prev;
            order.NgayCapNhat = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }
    }
}
