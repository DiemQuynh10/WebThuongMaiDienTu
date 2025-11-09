using Microsoft.AspNetCore.Mvc;
using ShopMVC.Helpers;
using ShopMVC.Models.ViewModels;

namespace ShopMVC.ViewComponents
{
    public class CartBadgeViewComponent : ViewComponent
    {
        private const string CART_KEY = "CART";
        public IViewComponentResult Invoke()
        {
            var gio = HttpContext.Session.GetObject<List<GioHangItem>>(CART_KEY) ?? new();
            int count = gio.Sum(x => x.SoLuong);
            decimal total = gio.Sum(x => x.ThanhTien);
            ViewBag.Total = total;
            return View(count);
        }
    }
}
