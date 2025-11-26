using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Helpers;
using ShopMVC.Models;
using ShopMVC.Models.ViewModels;
using System.Security.Claims;

namespace ShopMVC.Controllers
{
    [Authorize]
    public class DonHangController : Controller
    {
        private readonly AppDbContext _db;

        // Tách biệt 2 Session Key
        private const string CART_KEY = "CART";        // Giỏ hàng chính
        private const string DIRECT_CART_KEY = "DIRECT_CART"; // Giỏ hàng mua ngay

        public DonHangController(AppDbContext db) => _db = db;

        // GET: /DonHang/Checkout
        // Thêm tham số isDirect để biết nguồn hàng
        public async Task<IActionResult> Checkout(bool isDirect = false)
        {
            // 1. Xác định nguồn giỏ hàng
            string sessionKey = isDirect ? DIRECT_CART_KEY : CART_KEY;
            var gio = HttpContext.Session.GetObject<List<GioHangItem>>(sessionKey) ?? new List<GioHangItem>();

            // Nếu mua ngay mà không có hàng thì về trang chủ
            if (gio.Count == 0) return RedirectToAction("Index", "Home");

            // 2. Lấy thông tin user
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            // =================================================================================
            // START: LOGIC TỰ ĐỘNG ÁP DỤNG FLASH SALE
            // =================================================================================
            decimal tuDongGiamGia = 0;
            string maVoucherTuDong = "";

            var productIds = gio.Select(x => x.IdSanPham).ToList();

            var activeFlashSale = await _db.VoucherSanPhams
                .Include(vp => vp.Voucher)
                .Where(vp => productIds.Contains(vp.SanPhamId)
                             && vp.Voucher.IsFlashSale
                             && vp.Voucher.IsActive
                             && DateTime.Now >= vp.Voucher.NgayBatDau
                             && DateTime.Now <= vp.Voucher.NgayHetHan)
                .OrderByDescending(vp => vp.Voucher.NgayBatDau)
                .Select(vp => vp.Voucher)
                .FirstOrDefaultAsync();

            if (activeFlashSale != null)
            {
                maVoucherTuDong = activeFlashSale.Code;
                foreach (var item in gio)
                {
                    var saleItem = await _db.VoucherSanPhams
                        .FirstOrDefaultAsync(x => x.VoucherId == activeFlashSale.Id && x.SanPhamId == item.IdSanPham);

                    if (saleItem != null)
                    {
                        decimal thanhTienGoc = item.DonGia * item.SoLuong;
                        if (saleItem.GiaGiam.HasValue && saleItem.GiaGiam > 0)
                        {
                            decimal tienGiamItem = (item.DonGia - saleItem.GiaGiam.Value) * item.SoLuong;
                            if (tienGiamItem > 0) tuDongGiamGia += tienGiamItem;
                        }
                        else if (activeFlashSale.PhanTramGiam.HasValue && activeFlashSale.PhanTramGiam > 0)
                        {
                            decimal phanTram = (decimal)activeFlashSale.PhanTramGiam;
                            tuDongGiamGia += thanhTienGoc * phanTram / 100;
                        }
                    }
                }
                if (tuDongGiamGia == 0 && activeFlashSale.GiamTrucTiep.HasValue && activeFlashSale.GiamTrucTiep > 0)
                {
                    tuDongGiamGia = (decimal)activeFlashSale.GiamTrucTiep;
                }
            }
            // =================================================================================

            var vm = new CheckoutVM
            {
                Gio = gio,
                HoTenNhan = user?.HoTen ?? "",
                DienThoaiNhan = user?.PhoneNumber ?? "",
                DiaChiNhan = "",
                PhiVanChuyen = 30000,
                TienGiam = tuDongGiamGia,
                VoucherCode = maVoucherTuDong
            };

            ViewBag.AppliedVoucher = maVoucherTuDong;

            // Truyền cờ isDirect sang View để lát POST còn biết mà xử lý
            ViewBag.IsDirect = isDirect;

            return View("~/Views/DatHang/Checkout.cshtml", vm);
        }

        // POST: /DonHang/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Nhận tham số isDirect từ Form
        public async Task<IActionResult> Checkout(CheckoutVM model, bool isDirect = false)
        {
            // 1. Xác định đúng Session để lấy hàng (Tránh lấy nhầm giỏ hàng chính)
            string sessionKey = isDirect ? DIRECT_CART_KEY : CART_KEY;
            var gio = HttpContext.Session.GetObject<List<GioHangItem>>(sessionKey);

            if (gio == null || gio.Count == 0) return RedirectToAction("Index", "Home");

            model.Gio = gio;

            if (!ModelState.IsValid)
            {
                ViewBag.AppliedVoucher = model.VoucherCode;
                ViewBag.IsDirect = isDirect; // Giữ lại trạng thái nếu validate lỗi
                return View("~/Views/DatHang/Checkout.cshtml", model);
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            var donHang = new DonHang
            {
                MaDon = DateTime.Now.Ticks.ToString(),
                UserId = user?.Id ?? "",
                NgayDat = DateTime.UtcNow,
                NgayCapNhat = DateTime.UtcNow,
                HoTenNhan = model.HoTenNhan,
                DienThoaiNhan = model.DienThoaiNhan,
                DiaChiNhan = model.DiaChiNhan,
                PhiVanChuyen = model.PhiVanChuyen,
                TienGiam = model.TienGiam,
                TongTruocGiam = gio.Sum(x => x.ThanhTien) + model.PhiVanChuyen,
                TongThanhToan = (gio.Sum(x => x.ThanhTien) + model.PhiVanChuyen) - model.TienGiam,
                TrangThai = TrangThaiDonHang.ChoXacNhan,
                PhuongThucThanhToan = PhuongThucThanhToan.COD,
                VoucherCode = model.VoucherCode
            };

            _db.DonHangs.Add(donHang);
            await _db.SaveChangesAsync();

            // Logic cập nhật Flash Sale
            Voucher? appliedFlashSale = null;
            if (!string.IsNullOrEmpty(model.VoucherCode))
            {
                appliedFlashSale = await _db.Vouchers.FirstOrDefaultAsync(v => v.Code == model.VoucherCode && v.IsFlashSale);
            }

            foreach (var item in gio)
            {
                var chiTiet = new DonHangChiTiet
                {
                    IdDonHang = donHang.Id,
                    IdSanPham = item.IdSanPham,
                    DonGia = item.DonGia,
                    SoLuong = item.SoLuong,
                    ThanhTien = item.ThanhTien
                };
                _db.Set<DonHangChiTiet>().Add(chiTiet);

                // Trừ kho
                var sp = await _db.SanPhams.FindAsync(item.IdSanPham);
                if (sp != null) sp.TonKho -= item.SoLuong;

                // Cập nhật đã bán Flash Sale
                if (appliedFlashSale != null)
                {
                    var flashSaleItem = await _db.VoucherSanPhams
                        .FirstOrDefaultAsync(x => x.VoucherId == appliedFlashSale.Id && x.SanPhamId == item.IdSanPham);
                    if (flashSaleItem != null) flashSaleItem.SoLuongDaBan += item.SoLuong;
                }
            }
            await _db.SaveChangesAsync();

            // 4. Xóa đúng Session đã dùng (Mua ngay thì xóa DIRECT_CART, Mua thường xóa CART)
            HttpContext.Session.Remove(sessionKey);
            TempData["toast"] = "Đặt hàng thành công!";

            return RedirectToAction("ChiTiet", new { id = donHang.Id });
        }

        // POST: /DonHang/ApplyVoucher
        [HttpPost]
        public IActionResult ApplyVoucher(string code)
        {
            var voucher = _db.Vouchers.FirstOrDefault(v => v.Code == code && v.IsActive && v.NgayBatDau <= DateTime.Now && v.NgayHetHan >= DateTime.Now);
            if (voucher == null) return Json(new { ok = false, message = "Mã không hợp lệ!" });

            // Lấy giỏ hàng thì phải check xem đang ở chế độ nào (tạm thời lấy CART, nếu cần chính xác phải truyền isDirect)
            // Tuy nhiên ApplyVoucher thường dùng chung, ta có thể lấy cả 2 để check
            var gio = HttpContext.Session.GetObject<List<GioHangItem>>(DIRECT_CART_KEY) ?? HttpContext.Session.GetObject<List<GioHangItem>>(CART_KEY);

            if (gio == null) return Json(new { ok = false, message = "Giỏ hàng trống" });

            decimal tamTinh = gio.Sum(x => x.ThanhTien);
            decimal mucGiam = voucher.GiamTrucTiep ?? (tamTinh * (decimal)(voucher.PhanTramGiam ?? 0) / 100);

            return Json(new { ok = true, code = voucher.Code, discount = mucGiam, eligibleCount = gio.Count });
        }

        public async Task<IActionResult> ListVouchers()
        {
            var now = DateTime.Now;
            var vouchers = await _db.Vouchers
                .Where(v => v.IsActive && v.NgayBatDau <= now && v.NgayHetHan >= now)
                .Select(v => new { v.Code, v.Ten, v.PhanTramGiam, v.GiamTrucTiep, HieuLuc = v.NgayHetHan.ToString("dd/MM/yyyy") })
                .ToListAsync();
            return Json(new { ok = true, items = vouchers });
        }

        // Action CuaToi đã được cập nhật để nhận tham số lọc
        public async Task<IActionResult> CuaToi(string status) // <-- THÊM THAM SỐ LỌC
        {
            var userName = User.Identity!.Name!;
            var userId = await _db.Users.Where(u => u.UserName == userName).Select(u => u.Id).FirstAsync();

            // BẮT ĐẦU TRUY VẤN
            var query = _db.DonHangs.Where(d => d.UserId == userId);

            // [LOGIC LỌC]
            if (!string.IsNullOrEmpty(status) && Enum.TryParse(typeof(TrangThaiDonHang), status, true, out var parsedStatus))
            {
                // Lọc nếu trạng thái hợp lệ
                query = query.Where(d => d.TrangThai == (TrangThaiDonHang)parsedStatus!);
            }

            // LƯU TRẠNG THÁI LỌC VÀO VIEWBAG
            ViewBag.StatusFilter = status;

            // TIẾP TỤC TRUY VẤN VÀ LẤY DỮ LIỆU
            var ds = await query
                .Include(d => d.ChiTiets).ThenInclude(ct => ct.SanPham).ThenInclude(sp => sp.Anhs)
                .OrderByDescending(d => d.NgayDat).ToListAsync();

            return View(ds);
        }

        [HttpPost]
        public async Task<IActionResult> HuyDon(int id)
        {
            var userName = User.Identity!.Name!;
            var userId = await _db.Users.Where(u => u.UserName == userName).Select(u => u.Id).FirstAsync();

            var donHang = await _db.DonHangs.Include(d => d.ChiTiets)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (donHang == null) return NotFound();
            if (donHang.TrangThai != TrangThaiDonHang.ChoXacNhan)
            {
                TempData["error"] = "Đơn hàng đã được xử lý, không thể hủy!";
                return RedirectToAction("ChiTiet", new { id = id });
            }

            donHang.TrangThai = TrangThaiDonHang.DaHuy;
            Voucher? appliedFlashSale = null;
            if (!string.IsNullOrEmpty(donHang.VoucherCode))
            {
                appliedFlashSale = await _db.Vouchers.FirstOrDefaultAsync(v => v.Code == donHang.VoucherCode && v.IsFlashSale);
            }

            foreach (var ct in donHang.ChiTiets)
            {
                var sp = await _db.SanPhams.FindAsync(ct.IdSanPham);
                if (sp != null) sp.TonKho += ct.SoLuong;

                if (appliedFlashSale != null)
                {
                    var flashSaleItem = await _db.VoucherSanPhams
                        .FirstOrDefaultAsync(x => x.VoucherId == appliedFlashSale.Id && x.SanPhamId == ct.IdSanPham);
                    if (flashSaleItem != null)
                    {
                        flashSaleItem.SoLuongDaBan -= ct.SoLuong;
                        if (flashSaleItem.SoLuongDaBan < 0) flashSaleItem.SoLuongDaBan = 0;
                    }
                }
            }
            await _db.SaveChangesAsync();
            TempData["toast"] = "Đã hủy đơn hàng thành công!";
            return RedirectToAction("ChiTiet", new { id = id });
        }

        public async Task<IActionResult> ChiTiet(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var donHang = await _db.DonHangs
                .Include(d => d.ChiTiets)
                    .ThenInclude(ct => ct.SanPham)
                        .ThenInclude(sp => sp.Anhs)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (donHang == null) return NotFound();

            // Lấy danh sách Id sản phẩm mà user này đã đánh giá trong đơn này
            var reviewedIds = await _db.DanhGias
                .Where(d => d.IdDonHang == id && d.UserId == userId)
                .Select(d => d.IdSanPham)
                .ToListAsync();

            ViewBag.ReviewedProductIds = reviewedIds;

            return View(donHang);
        }
        [HttpGet]
        public async Task<IActionResult> MuaLai(int id)
        {
            // User hiện tại
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Lấy đơn + chi tiết + sản phẩm, đảm bảo là đơn của user đó
            var donHang = await _db.DonHangs
                .Include(d => d.ChiTiets)
                    .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (donHang == null)
                return NotFound();

            // Tạo giỏ mới cho lần "mua lại" (dùng DIRECT_CART như luồng MuaNgay)
            var gio = new List<GioHangItem>();

            foreach (var ct in donHang.ChiTiets)
            {
                if (ct.SanPham == null) continue;

                gio.Add(new GioHangItem
                {
                    IdSanPham = ct.IdSanPham,                 // FK tới sản phẩm
                    Ten = ct.SanPham.TenDayDu ?? ct.SanPham.Ten,
                    DonGia = ct.DonGia,
                    SoLuong = ct.SoLuong,
                    // Nếu ThanhTien là property tính toán thì không cần set,
                    // còn nếu là field bình thường thì có thể set:
                    // ThanhTien = ct.DonGia * ct.SoLuong
                });
            }

            // Lưu giỏ này vào session DIRECT_CART (giống luồng mua ngay)
            HttpContext.Session.SetObject(DIRECT_CART_KEY, gio);

            // Điều hướng sang Checkout với cờ isDirect = true
            return RedirectToAction(nameof(Checkout), new { isDirect = true });
        }

    }
}