using Microsoft.EntityFrameworkCore;
using ShopMVC.Areas.Admin.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using ShopMVC.Models;
using ShopMVC.Models.Dto;
using ShopMVC.Services.Interfaces;
using ShopMVC.Data;

namespace ShopMVC.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _db;
        public OrderService(AppDbContext db) => _db = db;

        // 1. Tìm đơn hàng
        public Task<DonHang?> FindAsync(int id)
            => _db.DonHangs.Include(d => d.ChiTiets).FirstOrDefaultAsync(o => o.Id == id);

        // 2. Đổi trạng thái (Có Transaction, Log, Voucher)
        public async Task ChangeStatusAsync(DonHang order, UpdateStatusDto dto, string? userId = null)
        {
            var from = order.TrangThai;
            var to = dto.To;
            if (from == to) return;

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // Log lịch sử
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

                // Log nhanh
                _db.DonHangLogs.Add(new DonHangLog
                {
                    MaDH = order.Id,
                    From = from,
                    To = to,
                    ByUser = userId,
                    At = DateTime.UtcNow
                });

                // Xử lý Voucher (Hoàn/Thu hồi lượt dùng)
                if (order.VoucherId != null)
                {
                    var v = await _db.Vouchers.SingleOrDefaultAsync(x => x.Id == order.VoucherId.Value);
                    if (v != null)
                    {
                        if (from != TrangThaiDonHang.DaHuy && to == TrangThaiDonHang.DaHuy)
                        {
                            // Hủy đơn -> Hoàn lại lượt dùng
                            if (v.SoLanDaSuDung > 0) v.SoLanDaSuDung -= 1;
                            _db.Vouchers.Update(v);
                        }
                        else if (from == TrangThaiDonHang.DaHuy && to != TrangThaiDonHang.DaHuy)
                        {
                            // Khôi phục đơn hủy -> Trừ lại lượt dùng
                            v.SoLanDaSuDung += 1;
                            _db.Vouchers.Update(v);
                        }
                    }
                }

                // Cập nhật trạng thái
                order.TrangThai = to;
                order.NgayCapNhat = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // 3. Hủy đơn & Hoàn kho
        public async Task CancelAndRestockAsync(DonHang order, string? reasonCode, string? note, string? userId)
        {
            // Log lịch sử
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

            // Cập nhật trạng thái
            order.TrangThai = TrangThaiDonHang.DaHuy;
            order.NgayCapNhat = DateTime.UtcNow;

            // Hoàn kho sản phẩm
            var spIds = order.ChiTiets.Select(x => x.IdSanPham).ToList();
            var sps = await _db.SanPhams.Where(p => spIds.Contains(p.Id)).ToListAsync();

            foreach (var ct in order.ChiTiets)
            {
                var sp = sps.First(p => p.Id == ct.IdSanPham);
                sp.TonKho += ct.SoLuong;
            }

            await _db.SaveChangesAsync();
        }

        // 4. Kiểm tra mở lại đơn
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

        // 5. Mở lại đơn hàng
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

        // ==========================================================
        // PHẦN BÁO CÁO THỐNG KÊ
        // ==========================================================

        public async Task<IEnumerable<MonthlyRevenueViewModel>> GetMonthlyRevenueAsync(int year)
        {
            var completedStatus = TrangThaiDonHang.HoanTat;
            return await _db.DonHangs
               .Where(o => o.TrangThai == completedStatus && o.NgayCapNhat.Year == year)
               .GroupBy(o => o.NgayCapNhat.Month)
               .Select(g => new MonthlyRevenueViewModel { Month = g.Key, Revenue = g.Sum(o => o.TongThanhToan) })
               .OrderBy(r => r.Month)
               .ToListAsync();
        }

        public async Task<IEnumerable<ProductSalesViewModel>> GetTopSellingProductsAsync(int month, int year, int count = 10)
        {
            var query = _db.DonHangChiTiets.Where(ct => ct.DonHang.TrangThai == TrangThaiDonHang.HoanTat && ct.DonHang.NgayCapNhat.Year == year);
            if (month > 0) query = query.Where(ct => ct.DonHang.NgayCapNhat.Month == month);

            var topStats = await query.GroupBy(ct => ct.IdSanPham)
                .Select(g => new { IdSanPham = g.Key, SoLuong = g.Sum(ct => ct.SoLuong), DoanhThu = g.Sum(ct => ct.ThanhTien) })
                .OrderByDescending(x => x.SoLuong).Take(count).ToListAsync();

            var productIds = topStats.Select(x => x.IdSanPham).ToList();
            var productsInfo = await _db.SanPhams.Include(p => p.Anhs).Where(p => productIds.Contains(p.Id)).ToListAsync();

            var results = new List<ProductSalesViewModel>();
            foreach (var stat in topStats)
            {
                var product = productsInfo.FirstOrDefault(p => p.Id == stat.IdSanPham);
                if (product != null)
                {
                    string? rawUrl = product.Anhs?.OrderBy(a => a.LaAnhChinh ? 0 : 1).FirstOrDefault()?.Url;
                    string finalUrl = "/images/no-image.png";
                    if (!string.IsNullOrEmpty(rawUrl)) finalUrl = (rawUrl.StartsWith("http") || rawUrl.StartsWith("/")) ? rawUrl : $"/images/sp/{rawUrl}";
                    results.Add(new ProductSalesViewModel { TenSanPham = product.Ten, HinhAnh = finalUrl, SoLuongBan = stat.SoLuong, DoanhThu = stat.DoanhThu });
                }
            }
            return results;
        }

        public async Task<IEnumerable<DonHang>> GetRecentOrdersAsync(int count = 5)
        {
            return await _db.DonHangs.OrderByDescending(o => o.NgayDat).Take(count).AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<OrderStatusViewModel>> GetOrderStatusStatsAsync()
        {
            var stats = await _db.DonHangs.GroupBy(o => o.TrangThai).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
            return stats.Select(s => new OrderStatusViewModel { StatusName = s.Status.ToString(), Count = s.Count });
        }

        
    }
}