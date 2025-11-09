// Services/OrderStatusRules.cs
using System.Collections.Generic;
using ShopMVC.Models;

namespace ShopMVC.Services
{
    public static class OrderStatusRules
    {
        public static readonly Dictionary<TrangThaiDonHang, HashSet<TrangThaiDonHang>> Allowed =
            new()
            {
                [TrangThaiDonHang.ChoXacNhan] = new() { TrangThaiDonHang.ChuanBi, TrangThaiDonHang.DaHuy },
                [TrangThaiDonHang.ChuanBi] = new() { TrangThaiDonHang.DangGiao, TrangThaiDonHang.DaHuy },
                [TrangThaiDonHang.DangGiao] = new() { TrangThaiDonHang.HoanTat },
                [TrangThaiDonHang.HoanTat] = new(),
                [TrangThaiDonHang.DaHuy] = new(),
            };

        public static bool IsBackward(TrangThaiDonHang from, TrangThaiDonHang to)
            => to < from;

        // yêu cầu lý do khi: hủy, override, hoặc đi ngoài allowed
        public static bool NeedReason(TrangThaiDonHang from, TrangThaiDonHang to, bool isOverride = false)
        {
            if (to == TrangThaiDonHang.DaHuy) return true;
            if (isOverride) return true;
            return !Allowed.TryGetValue(from, out var nexts) || !nexts.Contains(to);
        }
    }
}
