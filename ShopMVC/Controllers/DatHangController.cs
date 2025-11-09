using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Helpers;
using ShopMVC.Models;
using ShopMVC.Models.ViewModels;

namespace ShopMVC.Controllers
{
    [Authorize] // bắt buộc đăng nhập để đặt hàng
    public class DatHangController : Controller
    {
        private readonly AppDbContext _db;
        private const string CART_KEY = "CART";
        private const string SELECT_KEY = "CHECK_IDS";
        public DatHangController(AppDbContext db) => _db = db;

        private List<GioHangItem> LayGio()
            => HttpContext.Session.GetObject<List<GioHangItem>>(CART_KEY) ?? new List<GioHangItem>();
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChonMua([FromForm] int[]? ids)
        {
            // Lưu tạm các id dòng được chọn vào Session
            HttpContext.Session.SetObject(SELECT_KEY, (ids ?? Array.Empty<int>()).ToList());
            // Điều hướng sang trang Checkout (GET)
            return RedirectToAction(nameof(Checkout));
        }

        // ===== Helper tính tiền giảm theo voucher =====
        private static decimal TinhTienGiam(decimal tong, Voucher v)
        {
            if (v.PhanTramGiam is > 0)
            {
                var g = tong * (decimal)(v.PhanTramGiam.Value / 100.0);
                if (v.GiamToiDa is > 0) g = Math.Min(g, v.GiamToiDa.Value);
                return Math.Max(0, g);
            }
            if (v.GiamTrucTiep is > 0)
            {
                return Math.Min(tong, v.GiamTrucTiep.Value);
            }
            return 0m;
        }
        private static List<GioHangItem> LocItemTheoVoucher(
    List<GioHangItem> gio,
    Voucher v,
    List<SanPham> sanPhamsTuDb)
        {
            var allowBrandIds = (v.VoucherThuongHieus ?? new List<VoucherThuongHieu>())
                                .Select(x => x.ThuongHieuId).ToHashSet();
            var allowCateIds = (v.VoucherDanhMucs ?? new List<VoucherDanhMuc>())
                                .Select(x => x.DanhMucId).ToHashSet();

            bool limitBrand = allowBrandIds.Any();
            bool limitCate = allowCateIds.Any();

            return gio.Where(i =>
            {
                var sp = sanPhamsTuDb.FirstOrDefault(p => p.Id == i.IdSanPham);
                if (sp == null) return false;

                // IdThuongHieu, IdDanhMuc là int thường
                var okBrand = !limitBrand || allowBrandIds.Contains(sp.IdThuongHieu);
                var okCate = !limitCate || allowCateIds.Contains(sp.IdDanhMuc);

                return okBrand && okCate;
            }).ToList();
        }


        // ===== GET: trang Checkout =====
        [HttpGet]
        public IActionResult Checkout()
        {
            var gio = LayGio();
            if (!gio.Any()) return RedirectToAction("Index", "GioHang");

            // lấy danh sách id đã chọn (nếu có) và lọc lại giỏ
            var selected = HttpContext.Session.GetObject<List<int>>(SELECT_KEY) ?? new List<int>();
            if (selected.Any())
            {
                gio = gio.Where(x => selected.Contains(x.IdSanPham)).ToList();
                HttpContext.Session.Remove(SELECT_KEY); // dùng xong thì xoá
            }

            var vm = new CheckoutVM
            {
                Gio = gio,
                HoTenNhan = User.Identity?.Name ?? "",
                PhiVanChuyen = 0,
                TienGiam = 0,
                VoucherCode = null
            };
            return View(vm);
        }


        // ===== AJAX: áp dụng voucher (tính thử cho UI) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyVoucher(string code)
        {
            var gio = LayGio();
            if (!gio.Any())
                return Json(new { ok = false, message = "Giỏ hàng rỗng." });

            code = (code ?? "").Trim().ToUpper();

            // Nạp voucher + scope brand/category
            var v = await _db.Vouchers
                .Include(x => x.VoucherThuongHieus)
                .Include(x => x.VoucherDanhMucs)
                .FirstOrDefaultAsync(x => x.Code == code && x.IsActive);
            if (v == null)
                return Json(new { ok = false, message = "Mã không tồn tại hoặc đã ngừng." });

            var now = DateTime.Now;
            if (now < v.NgayBatDau || now > v.NgayHetHan)
                return Json(new { ok = false, message = "Mã chưa đến hạn hoặc đã hết hạn." });

            if (v.SoLanSuDungToiDa > 0 && v.SoLanDaSuDung >= v.SoLanSuDungToiDa)
                return Json(new { ok = false, message = "Mã đã hết lượt sử dụng." });

            // Nạp sản phẩm đang có trong giỏ để biết brand/category
            var spIds = gio.Select(x => x.IdSanPham).ToList();
            var sanPhams = await _db.SanPhams
                .Where(p => spIds.Contains(p.Id))
                .Select(p => new SanPham
                {
                    Id = p.Id,
                    IdThuongHieu = p.IdThuongHieu,
                    IdDanhMuc = p.IdDanhMuc,
                    Gia = p.Gia,
                    GiaKhuyenMai = p.GiaKhuyenMai
                })
                .ToListAsync();

            // Chỉ lấy các item thuộc phạm vi voucher
            var eligibleItems = LocItemTheoVoucher(gio, v, sanPhams);
            if (!eligibleItems.Any())
                return Json(new { ok = false, message = "Không có sản phẩm nào phù hợp điều kiện voucher." });

            var eligibleSubtotal = eligibleItems.Sum(x => x.ThanhTien);
            var discount = TinhTienGiam(eligibleSubtotal, v);
            var cartTotal = gio.Sum(x => x.ThanhTien);
            var final = Math.Max(0, cartTotal - discount);

            return Json(new
            {
                ok = true,
                code = v.Code,
                eligibleSubtotal = eligibleSubtotal.ToString("N0"),
                discount = discount.ToString("N0"),
                final = final.ToString("N0"),
                eligibleCount = eligibleItems.Count
            });
        }


        // ===== POST: đặt hàng thật (xác nhận lại voucher + trừ lượt) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutVM vm)
        {
            var gio = LayGio();
            if (!gio.Any())
                return RedirectToAction("Index", "GioHang");

            // Kiểm tra form cơ bản
            if (!ModelState.IsValid)
            {
                vm.Gio = gio;
                return View(vm);
            }

            // Đồng bộ giá & tồn kho (chống sửa giá client)
            var spIds = gio.Select(x => x.IdSanPham).ToList();
            var sanPhams = await _db.SanPhams
                .Where(p => spIds.Contains(p.Id))
                .ToListAsync();

            foreach (var i in gio)
            {
                var sp = sanPhams.First(p => p.Id == i.IdSanPham);
                var donGia = sp.GiaKhuyenMai ?? sp.Gia;

                if (i.SoLuong <= 0 || i.SoLuong > sp.TonKho)
                    ModelState.AddModelError(string.Empty, $"Sản phẩm {sp.Ten} không đủ tồn kho.");

                // sync giá hiện tại
                i.DonGia = donGia;
            }

            if (!ModelState.IsValid)
            {
                vm.Gio = gio;
                return View(vm);
            }

            // Tổng trước giảm của toàn giỏ
            var tongTruocGiam = gio.Sum(x => x.ThanhTien);

            // ===== Xác nhận voucher lần cuối =====
            Voucher? voucher = null;
            decimal tienGiam = 0m;

            if (!string.IsNullOrWhiteSpace(vm.VoucherCode))
            {
                var code = vm.VoucherCode.Trim().ToUpper();

                voucher = await _db.Vouchers
                    .Include(x => x.VoucherThuongHieus)   // nếu dùng many-to-many
                    .Include(x => x.VoucherDanhMucs)      // nếu dùng many-to-many
                    .FirstOrDefaultAsync(x => x.Code == code && x.IsActive);

                if (voucher == null)
                {
                    ModelState.AddModelError(nameof(vm.VoucherCode), "Mã không tồn tại hoặc đã ngừng.");
                }
                else
                {
                    var now = DateTime.Now;
                    if (now < voucher.NgayBatDau || now > voucher.NgayHetHan)
                    {
                        ModelState.AddModelError(nameof(vm.VoucherCode), "Mã chưa đến hạn hoặc đã hết hạn.");
                    }
                    else if (voucher.SoLanSuDungToiDa > 0 && voucher.SoLanDaSuDung >= voucher.SoLanSuDungToiDa)
                    {
                        ModelState.AddModelError(nameof(vm.VoucherCode), "Mã đã hết lượt sử dụng.");
                    }
                    else
                    {
                        // Lọc các item thuộc phạm vi voucher (brand/danh mục)
                        var eligibleItems = LocItemTheoVoucher(gio, voucher, sanPhams);
                        if (!eligibleItems.Any())
                        {
                            ModelState.AddModelError(nameof(vm.VoucherCode), "Giỏ hàng không có sản phẩm phù hợp điều kiện voucher.");
                        }
                        else
                        {
                            var eligibleSubtotal = eligibleItems.Sum(x => x.ThanhTien);
                            tienGiam = TinhTienGiam(eligibleSubtotal, voucher);
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                vm.Gio = gio;
                vm.TienGiam = tienGiam;
                return View(vm);
            }

            // ===== Tạo đơn trong transaction =====
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // Lấy UserId hiện tại (đổi theo Identity của m nếu cần)
                var userId = await _db.Users
                    .Where(u => u.UserName == User.Identity!.Name!)
                    .Select(u => u.Id)
                    .FirstAsync();

                var don = new DonHang
                {
                    MaDon = $"DH{DateTime.UtcNow:yyyyMMddHHmmss}",
                    UserId = userId,

                    HoTenNhan = vm.HoTenNhan,
                    DienThoaiNhan = vm.DienThoaiNhan,
                    DiaChiNhan = vm.DiaChiNhan,

                    PhiVanChuyen = vm.PhiVanChuyen,
                    TienGiam = tienGiam,
                    TongTruocGiam = tongTruocGiam,
                    TongThanhToan = Math.Max(0, tongTruocGiam + vm.PhiVanChuyen - tienGiam),

                    VoucherId = voucher?.Id,
                    VoucherCode = voucher?.Code,

                    PhuongThucThanhToan = PhuongThucThanhToan.COD, // đổi nếu m có chọn khác
                    TrangThai = TrangThaiDonHang.ChoXacNhan,
                    NgayDat = DateTime.UtcNow,
                    NgayCapNhat = DateTime.UtcNow
                };

                _db.DonHangs.Add(don);
                await _db.SaveChangesAsync();

                // Chi tiết đơn + trừ kho
                var chiTiets = new List<DonHangChiTiet>();
                foreach (var i in gio)
                {
                    chiTiets.Add(new DonHangChiTiet
                    {
                        IdDonHang = don.Id,
                        IdSanPham = i.IdSanPham,
                        SoLuong = i.SoLuong,
                        DonGia = i.DonGia,
                        ThanhTien = i.ThanhTien
                    });

                    var sp = sanPhams.First(p => p.Id == i.IdSanPham);
                    sp.TonKho -= i.SoLuong;
                    _db.SanPhams.Update(sp);
                }
                _db.DonHangChiTiets.AddRange(chiTiets);

                // Tăng lượt sử dụng voucher (nếu có) — có chống đua
                if (voucher != null)
                {
                    voucher.SoLanDaSuDung += 1;
                    // gắn RowVersion gốc để EF kiểm tra xung đột
                    if (voucher.RowVersion != null)
                        _db.Entry(voucher).OriginalValues["RowVersion"] = voucher.RowVersion;
                    _db.Vouchers.Update(voucher);
                }


                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                HttpContext.Session.Remove(CART_KEY);
                return RedirectToAction("ChiTiet", "DonHang", new { id = don.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Voucher vừa được dùng hết lượt. Vui lòng chọn mã khác.");
                vm.Gio = gio;
                vm.TienGiam = tienGiam;
                return View(vm);
            }
            catch
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi tạo đơn. Vui lòng thử lại.");
                vm.Gio = gio;
                vm.TienGiam = tienGiam;
                return View(vm);
            }
        }


        [HttpGet]
    [AllowAnonymous] // hoặc bỏ nếu muốn bắt buộc đăng nhập
    [Produces("application/json")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [Route("/DatHang/ListVouchers")] // ép path rõ ràng
    public async Task<IActionResult> ListVouchers()
    {
        var now = DateTime.Now;

        // Lấy thẳng từ DB, KHÔNG format trong Select
        var raw = await _db.Vouchers
            .AsNoTracking()
            .Where(v => v.IsActive
                && now >= v.NgayBatDau && now <= v.NgayHetHan
                && (v.SoLanSuDungToiDa == 0 || v.SoLanDaSuDung < v.SoLanSuDungToiDa))
            .OrderBy(v => v.NgayHetHan)
            .Select(v => new
            {
                v.Code,
                v.Ten,
                v.PhanTramGiam,
                v.GiamTrucTiep,
                v.GiamToiDa,
                v.NgayBatDau,
                v.NgayHetHan,
                v.SoLanSuDungToiDa,
                v.SoLanDaSuDung
            })
            .ToListAsync();

        // Format NGÀY và chuẩn camelCase ngoài DB
        var items = raw.Select(v => new
        {
            code = v.Code,
            ten = v.Ten,
            phanTramGiam = v.PhanTramGiam ?? 0,
            giamTrucTiep = v.GiamTrucTiep ?? 0,
            giamToiDa = v.GiamToiDa ?? 0,
            hieuLuc = $"{v.NgayBatDau:dd/MM/yyyy} - {v.NgayHetHan:dd/MM/yyyy}",
            conLai = v.SoLanSuDungToiDa == 0 ? "∞" : (v.SoLanSuDungToiDa - v.SoLanDaSuDung).ToString()
        });

        return Ok(new { ok = true, items });
    }

}
}
