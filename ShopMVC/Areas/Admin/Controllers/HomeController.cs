using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using ShopMVC.Areas.Admin.ViewModels;
using System.Collections.Generic;
using System.Text;

namespace ShopMVC.Areas.Admin.Controllers
{
    public class HomeController : AdminBaseController
    {
        private readonly AppDbContext _db;
        private readonly IOrderService _orderService;

        public HomeController(AppDbContext db, IOrderService orderService)
        {
            _db = db;
            _orderService = orderService;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Thống kê cơ bản
            ViewBag.SoSP = await _db.SanPhams.CountAsync();
            ViewBag.SoDH = await _db.DonHangs.CountAsync();
            ViewBag.DoanhThu = await _db.DonHangs
                .Where(d => d.TrangThai == Models.TrangThaiDonHang.HoanTat)
                .SumAsync(d => (decimal?)d.TongThanhToan) ?? 0m;

            // 2. Dữ liệu ban đầu cho View
            int currentMonth = DateTime.Now.Month;
            int currentYear = DateTime.Now.Year;

            var topProducts = await _orderService.GetTopSellingProductsAsync(currentMonth, currentYear);
            ViewBag.TopProducts = topProducts.ToList();

            var recentOrders = await _orderService.GetRecentOrdersAsync(5); // Lấy 5 đơn mới nhất
            ViewBag.RecentOrders = recentOrders.ToList();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetYearlyRevenue(int? year)
        {
            int currentYear = year ?? DateTime.Now.Year;
            var revenueData = await _orderService.GetMonthlyRevenueAsync(currentYear);
            var allMonths = Enumerable.Range(1, 12).ToList();
            var labels = allMonths.Select(m => $"Tháng {m}").ToList();
            var chartData = from month in allMonths
                            join revenue in revenueData on month equals revenue.Month into monthRevenue
                            from r in monthRevenue.DefaultIfEmpty()
                            select r?.Revenue ?? 0m;
            return Json(new { labels, data = chartData });
        }

        [HttpGet]
        public async Task<IActionResult> GetTopProductsFilter(int month, int year)
        {
            var data = await _orderService.GetTopSellingProductsAsync(month, year);
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderStatusChart()
        {
            var data = await _orderService.GetOrderStatusStatsAsync();
            return Json(new
            {
                labels = data.Select(x => x.StatusName),
                data = data.Select(x => x.Count)
            });
        }

        // --- HÀM XUẤT EXCEL 
        public async Task<IActionResult> ExportToExcel(int? year)
        {
            try
            {
                int currentYear = year ?? DateTime.Now.Year;

                // Lấy dữ liệu
                var revenueData = await _orderService.GetMonthlyRevenueAsync(currentYear);
                var topStats = await _orderService.GetTopSellingProductsAsync(0, currentYear, 20); // Top 20 cả năm

                // Tạo nội dung CSV
                var csv = new StringBuilder();
                csv.AppendLine($"BAO CAO TONG QUAN NAM {currentYear}");
                csv.AppendLine($"Ngay xuat: {DateTime.Now:dd/MM/yyyy HH:mm}");
                csv.AppendLine();

                // Phần 1: Doanh thu
                csv.AppendLine("I. DOANH THU THEO THANG");
                csv.AppendLine("Thang,Doanh thu (VND)");
                decimal totalRev = 0;
                for (int m = 1; m <= 12; m++)
                {
                    var monthRev = revenueData.FirstOrDefault(r => r.Month == m)?.Revenue ?? 0;
                    csv.AppendLine($"Thang {m},{monthRev}");
                    totalRev += monthRev;
                }
                csv.AppendLine($"TONG CONG,{totalRev}");
                csv.AppendLine();

                // Phần 2: Top Sản phẩm
                csv.AppendLine("II. TOP SAN PHAM BAN CHAY (CA NAM)");
                csv.AppendLine("STT,Ten san pham,So luong ban,Doanh thu (VND)");

                int rank = 1;
                if (topStats != null && topStats.Any())
                {
                    foreach (var stat in topStats)
                    {
                        // Xử lý tên sản phẩm để tránh lỗi CSV (thay dấu phẩy bằng khoảng trắng)
                        var pName = stat.TenSanPham?.Replace(",", " ").Replace("\"", "") ?? "SP Da Xoa";
                        csv.AppendLine($"{rank},{pName},{stat.SoLuongBan},{stat.DoanhThu}");
                        rank++;
                    }
                }
                else
                {
                    csv.AppendLine("Khong co du lieu ban hang.");
                }

                // Chuyển sang byte array với BOM (Byte Order Mark) để Excel hiển thị đúng tiếng Việt (nếu có)
                byte[] buffer = Encoding.UTF8.GetBytes(csv.ToString());
                byte[] bom = Encoding.UTF8.GetPreamble();
                var result = new byte[bom.Length + buffer.Length];
                Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
                Buffer.BlockCopy(buffer, 0, result, bom.Length, buffer.Length);

                return File(result, "text/csv", $"BaoCao_Nam_{currentYear}.csv");
            }
            catch (Exception)
            {
                // Nếu lỗi, quay về trang chủ
                return RedirectToAction(nameof(Index));
            }
        }
    }
}