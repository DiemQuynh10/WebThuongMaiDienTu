namespace ShopMVC.Areas.Admin.ViewModels
{
    // Lớp này dùng để chứa kết quả truy vấn doanh thu theo tháng
    public class MonthlyRevenueViewModel
    {
        public int Month { get; set; } // Sẽ là 1, 2, 3, ... 12
        public decimal Revenue { get; set; }
    }
}