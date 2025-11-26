using System.Threading.Tasks;
using ShopMVC.Areas.Admin.ViewModels;
using ShopMVC.Models;
using ShopMVC.Models.Dto;

namespace ShopMVC.Services.Interfaces
{
    public interface IOrderService
    {
        Task<DonHang?> FindAsync(int id);
        Task ChangeStatusAsync(DonHang order, UpdateStatusDto dto, string? userId = null);

        // PHẢI có hàm này (đúng tên, đúng tham số)
        Task CancelAndRestockAsync(DonHang order, string? reasonCode, string? note, string? userId);

        // (nếu làm tính năng mở lại)
        Task<bool> CanReopenAsync(int orderId);
        Task ReopenAsync(int orderId, string reasonCode, string? note, string? userId);
        Task<IEnumerable<MonthlyRevenueViewModel>> GetMonthlyRevenueAsync(int year);
        Task<IEnumerable<ProductSalesViewModel>> GetTopSellingProductsAsync(int month, int year, int count = 10);
        Task<IEnumerable<DonHang>> GetRecentOrdersAsync(int count = 5);
        Task<IEnumerable<OrderStatusViewModel>> GetOrderStatusStatsAsync();
        

    }
}
