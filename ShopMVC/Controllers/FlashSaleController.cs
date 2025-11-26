using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Models;
using ShopMVC.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ShopMVC.Controllers
{
    public class FlashSaleController : Controller
    {
        private readonly AppDbContext _db;
        public FlashSaleController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            // 1. Tìm Flash Sale đang chạy & còn lượt
            var activeFlashSale = await _db.Vouchers
                .Where(v =>
                    v.IsActive &&
                    v.IsFlashSale &&
                    v.NgayBatDau <= now &&
                    v.NgayHetHan >= now &&
                    (v.SoLanSuDungToiDa == 0 || v.SoLanDaSuDung < v.SoLanSuDungToiDa))
                .OrderBy(v => v.NgayHetHan)
                .FirstOrDefaultAsync();

            if (activeFlashSale == null)
                return View("Empty");

            // 2. Lấy danh sách sản phẩm còn phân bổ
            var flashSaleItems = await _db.VoucherSanPhams
                .Include(vp => vp.SanPham).ThenInclude(p => p.Anhs)
                .Include(vp => vp.SanPham).ThenInclude(p => p.ChiTietSanPhams)
                .Where(vp => vp.VoucherId == activeFlashSale.Id)
                .Where(vp => vp.SanPham != null)
                .Where(vp => vp.SoLuongPhanBo > vp.SoLuongDaBan) // còn hàng trong chương trình
                .ToListAsync();

            // 3. Map ViewModel với logic giá giống Admin
            var viewModel = new FlashSaleViewModel
            {
                VoucherId = activeFlashSale.Id,
                TenChuongTrinh = activeFlashSale.Ten,
                EndTime = activeFlashSale.NgayHetHan,
                Items = flashSaleItems.Select(vp =>
                {
                    var sp = vp.SanPham!;

                    // Giá gốc: giống logic bên Admin AddProductToVoucher
                    decimal originalPrice = sp.Gia > 0
                        ? sp.Gia
                        : (sp.ChiTietSanPhams.Any()
                            ? sp.ChiTietSanPhams.Min(c => c.Gia)
                            : 0);

                    decimal finalPrice;

                    if (vp.GiaGiam.HasValue && vp.GiaGiam.Value > 0)
                    {
                        // Admin đã set tay hoặc tính sẵn
                        finalPrice = vp.GiaGiam.Value;
                    }
                    else
                    {
                        // Tính tự động lại theo cấu hình voucher (backup)
                        decimal discountAmount = 0;

                        if (activeFlashSale.PhanTramGiam > 0)
                        {
                            decimal percent = (decimal)(activeFlashSale.PhanTramGiam ?? 0);
                            discountAmount = originalPrice * percent / 100m;
                        }
                        else if (activeFlashSale.GiamTrucTiep > 0)
                        {
                            discountAmount = (decimal)(activeFlashSale.GiamTrucTiep ?? 0);
                        }

                        // Giảm tối đa
                        if (activeFlashSale.GiamToiDa > 0 && discountAmount > activeFlashSale.GiamToiDa)
                        {
                            discountAmount = (decimal)activeFlashSale.GiamToiDa;
                        }

                        finalPrice = originalPrice - discountAmount;
                    }

                    if (finalPrice < 0) finalPrice = 0;
                    if (originalPrice > 0 && finalPrice > originalPrice)
                        finalPrice = originalPrice;

                    return new FlashSaleItemViewModel
                    {
                        SanPham = sp,
                        GiaGoc = originalPrice,
                        GiaSale = finalPrice,
                        SoLuongPhanBo = vp.SoLuongPhanBo,
                        SoLuongDaBan = vp.SoLuongDaBan
                    };
                }).ToList()
            };

            return View(viewModel);
        }
    }
}
