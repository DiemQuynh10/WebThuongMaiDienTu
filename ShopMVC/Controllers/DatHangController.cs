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

        // ===== FIX 1: THÊM ACTION MUA NGAY (TỪ TRANG CHI TIẾT) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MuaNgay(int idSanPham, int soLuong)
        {
            if (soLuong <= 0)
            {
                TempData["LoiMuaNgay"] = "Số lượng phải lớn hơn 0.";
                return RedirectToAction("ChiTiet", "SanPham", new { id = idSanPham });
            }

            // ===== SỬA QUERY TẠI ĐÂY =====
            // Chúng ta không lấy cả object SanPham, mà chỉ lấy các thông tin cần thiết
            // và lấy ảnh đầu tiên từ collection 'Anhs'
            var spData = await _db.SanPhams
                .AsNoTracking()
                .Where(p => p.Id == idSanPham)
                .Select(p => new {
                    p.Id,
                    p.Ten,
                    p.Gia,
                    p.GiaKhuyenMai,
                    p.TonKho,
                    // Lấy ảnh đầu tiên từ 'Anhs'.
                    // !!! QUAN TRỌNG:
                    // Mình đang đoán model 'AnhSanPham' của bạn có cột 'Id' và 'FileName'.
                    // Nếu tên cột của bạn khác, hãy SỬA LẠI cho đúng.
                    // Ví dụ: .OrderBy(a => a.ThuTu).Select(a => a.Url).FirstOrDefault()
                    //
                    // Lỗi sẽ xảy ra ở đây nếu 'AnhSanPham' không có 'Id' hoặc 'FileName'
                    Anh = p.Anhs.OrderBy(a => a.Id).Select(a => a.Url).FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (spData == null)
            {
                TempData["LoiMuaNgay"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index", "Home");
            }

            if (soLuong > spData.TonKho)
            {
                TempData["LoiMuaNgay"] = $"Chỉ còn {spData.TonKho} sản phẩm tồn kho.";
                return RedirectToAction("ChiTiet", "SanPham", new { id = idSanPham });
            }

            var gio = LayGio();

            // Dùng thông tin spData đã query
            var item = new GioHangItem
            {
                IdSanPham = spData.Id,
                Ten = spData.Ten, // Khớp với GioHangItem.Ten
                Anh = spData.Anh ?? "default.jpg", // Khớp với GioHangItem.Anh
                SoLuong = soLuong,
                DonGia = spData.GiaKhuyenMai ?? spData.Gia
            };

            // Thêm hoặc cập nhật (thay thế) item này trong giỏ
            var existing = gio.FirstOrDefault(i => i.IdSanPham == idSanPham);
            if (existing != null)
            {
                existing.SoLuong = soLuong; // Cập nhật số lượng
                existing.DonGia = item.DonGia; // Cập nhật giá
                existing.Anh = item.Anh; // Cập nhật ảnh
                existing.Ten = item.Ten; // Cập nhật tên
            }
            else
            {
                gio.Add(item);
            }

            // Lưu lại toàn bộ giỏ vào Session
            HttpContext.Session.SetObject(CART_KEY, gio);

            // *** Quan trọng: Set SELECT_KEY để Checkout (GET) chỉ lọc item này ***
            HttpContext.Session.SetObject(SELECT_KEY, new List<int> { idSanPham });

            // Chuyển đến trang Checkout
            return RedirectToAction(nameof(Checkout));
        }
        // ==========================================================

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


        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var gioFull = LayGio();
            if (!gioFull.Any()) return RedirectToAction("Index", "GioHang");

            // lấy danh sách id đã chọn (nếu có) và lọc lại giỏ
            var selected = HttpContext.Session.GetObject<List<int>>(SELECT_KEY) ?? new List<int>();

            List<GioHangItem> gioCanThanhToan;
            if (selected.Any())
            {
                gioCanThanhToan = gioFull.Where(x => selected.Contains(x.IdSanPham)).ToList();
            }
            else
            {
                gioCanThanhToan = gioFull;
            }

            if (!gioCanThanhToan.Any())
            {
                return RedirectToAction("Index", "GioHang");
            }

            // 🔥 ĐOẠN QUAN TRỌNG: lấy URL ảnh giống bên chi tiết
            var spIds = gioCanThanhToan.Select(x => x.IdSanPham).ToList();

            var anhDict = await _db.AnhSanPhams
                .Where(a => spIds.Contains(a.IdSanPham))
                .OrderByDescending(a => a.LaAnhChinh)
                .ThenBy(a => a.ThuTu)
                .GroupBy(a => a.IdSanPham)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.First().Url
                );

            foreach (var it in gioCanThanhToan)
            {
                if (anhDict.TryGetValue(it.IdSanPham, out var url))
                {
                    it.Anh = url; // Url đã dạng: /images/sp/xxx.jpg
                }
                else if (string.IsNullOrWhiteSpace(it.Anh))
                {
                    it.Anh = "/images/no-image.png";
                }
            }
        

            var vm = new CheckoutVM
            {
                Gio = gioCanThanhToan,
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
            // === FIX 2 (Phần B): ÁP DỤNG VOUCHER DỰA TRÊN GIỎ ĐÃ LỌC ===
            var gioFull = LayGio();
            if (!gioFull.Any())
                return Json(new { ok = false, message = "Giỏ hàng rỗng." });

            // Lọc giỏ theo các ID đã chọn
            var selectedIds = HttpContext.Session.GetObject<List<int>>(SELECT_KEY) ?? new List<int>();
            List<GioHangItem> gio;
            if (selectedIds.Any())
            {
                gio = gioFull.Where(x => selectedIds.Contains(x.IdSanPham)).ToList();
            }
            else
            {
                gio = gioFull; // Mặc định lấy cả giỏ nếu không có gì trong SELECT_KEY
            }

            if (!gio.Any())
                return Json(new { ok = false, message = "Giỏ hàng cần thanh toán rỗng." });
            // ========================================================

            code = (code ?? "").Trim().ToUpper();

            // Nạp voucher + scope brand/category
            var v = await _db.Vouchers
    .Include(x => x.VoucherThuongHieus)
    .Include(x => x.VoucherDanhMucs)
    .FirstOrDefaultAsync(x => x.Code == code
                              && x.IsActive
                              && !x.IsFlashSale);
            if (v == null)
                return Json(new { ok = false, message = "Mã không tồn tại hoặc đã ngừng." });

            var now = DateTime.Now;
            if (now < v.NgayBatDau || now > v.NgayHetHan)
                return Json(new { ok = false, message = "Mã chưa đến hạn hoặc đã hết hạn." });

            if (v.SoLanSuDungToiDa > 0 && v.SoLanDaSuDung >= v.SoLanSuDungToiDa)
                return Json(new { ok = false, message = "Mã đã hết lượt sử dụng." });

            // Nạp sản phẩm đang có trong giỏ (giỏ đã lọc) để biết brand/category
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
                // Có thể giữ dạng số cho tiện xử lý JS
                eligibleSubtotal = eligibleSubtotal,
                discount = discount,   // <-- SỬA DÒNG NÀY
                final = final,
                eligibleCount = eligibleItems.Count
            });
        }


        // ===== POST: đặt hàng thật (xác nhận lại voucher + trừ lượt) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutVM vm)
        {
            // === FIX 2 (Phần C): LỌC GIỎ HÀNG KHI ĐẶT HÀNG ===
            var gioFull = LayGio(); // Lấy giỏ đầy đủ
            if (!gioFull.Any())
                return RedirectToAction("Index", "GioHang");

            // Lọc giỏ theo các ID đã chọn từ bước GET
            var selectedIds = HttpContext.Session.GetObject<List<int>>(SELECT_KEY) ?? new List<int>();
            List<GioHangItem> gio; // Đây là giỏ sẽ được thanh toán

            if (selectedIds.Any())
            {
                gio = gioFull.Where(x => selectedIds.Contains(x.IdSanPham)).ToList();
            }
            else
            {
                // Mặc định lấy cả giỏ nếu không có gì trong SELECT_KEY
                gio = gioFull;
            }

            if (!gio.Any()) // Giỏ cần thanh toán rỗng
            {
                return RedirectToAction("Index", "GioHang");
            }
            // ===============================================

            // Kiểm tra form cơ bản
            if (!ModelState.IsValid)
            {
                vm.Gio = gio; // Trả về giỏ đã lọc
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

            // Tổng trước giảm của toàn giỏ (giỏ đã lọc)
            var tongTruocGiam = gio.Sum(x => x.ThanhTien);

            // ===== Xác nhận voucher lần cuối =====
            Voucher? voucher = null;
            decimal tienGiam = 0m;

            if (!string.IsNullOrWhiteSpace(vm.VoucherCode))
            {
                var code = vm.VoucherCode.Trim().ToUpper();

                voucher = await _db.Vouchers
     .Include(x => x.VoucherThuongHieus)
     .Include(x => x.VoucherDanhMucs)
     .FirstOrDefaultAsync(x => x.Code == code
                               && x.IsActive
                               && !x.IsFlashSale);

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

                // Chi tiết đơn + trừ kho (chỉ các item trong giỏ đã lọc)
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

                // ===== FIX 2 (Phần D): CẬP NHẬT LẠI GIỎ HÀNG SAU KHI MUA =====
                // Xoá các ID đã chọn khỏi SELECT_KEY
                HttpContext.Session.Remove(SELECT_KEY);

                // Lọc lại giỏ hàng chính (CART_KEY), chỉ giữ lại những
                // item KHÔNG CÓ trong giỏ vừa thanh toán (gio).
                var purchasedIds = gio.Select(i => i.IdSanPham).ToHashSet();
                var gioMoi = gioFull.Where(i => !purchasedIds.Contains(i.IdSanPham)).ToList();

                if (gioMoi.Any())
                {
                    HttpContext.Session.SetObject(CART_KEY, gioMoi);
                }
                else
                {
                    // Nếu giỏ mới rỗng, xoá key luôn
                    HttpContext.Session.Remove(CART_KEY);
                }
                // =============================================================

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
        [Produces("application/json")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [Route("/DatHang/ListApplicableVouchers")]
        public async Task<IActionResult> ListApplicableVouchers()
        {
            // 1. Lấy giỏ cần thanh toán (giống Checkout & ApplyVoucher)
            var gioFull = LayGio();
            if (!gioFull.Any())
                return Ok(new { ok = false, message = "Giỏ hàng rỗng.", items = Array.Empty<object>() });

            var selectedIds = HttpContext.Session.GetObject<List<int>>(SELECT_KEY) ?? new List<int>();
            List<GioHangItem> gio;
            if (selectedIds.Any())
                gio = gioFull.Where(x => selectedIds.Contains(x.IdSanPham)).ToList();
            else
                gio = gioFull;

            if (!gio.Any())
                return Ok(new { ok = false, message = "Giỏ hàng cần thanh toán rỗng.", items = Array.Empty<object>() });

            // 2. Lấy thông tin sản phẩm trong giỏ để check brand/category
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

            var now = DateTime.Now;

            // 3. Lấy danh sách voucher thường còn hiệu lực, còn lượt
            var vouchers = await _db.Vouchers
                .Include(v => v.VoucherThuongHieus)
                .Include(v => v.VoucherDanhMucs)
                .Where(v => v.IsActive
                            && !v.IsFlashSale        // ❗ Không lấy Flash Sale
                            && now >= v.NgayBatDau && now <= v.NgayHetHan
                            && (v.SoLanSuDungToiDa == 0 || v.SoLanDaSuDung < v.SoLanSuDungToiDa))
                .OrderBy(v => v.NgayHetHan)
                .ToListAsync();

            // 4. Lọc voucher nào thực sự áp được cho giỏ hiện tại
            var items = vouchers
                .Select(v =>
                {
                    var eligibleItems = LocItemTheoVoucher(gio, v, sanPhams);
                    if (!eligibleItems.Any()) return null;

                    var eligibleSubtotal = eligibleItems.Sum(x => x.ThanhTien);
                    var discount = TinhTienGiam(eligibleSubtotal, v);
                    if (discount <= 0) return null;

                    return new
                    {
                        code = v.Code,
                        ten = v.Ten,
                        phanTramGiam = v.PhanTramGiam ?? 0,
                        giamTrucTiep = v.GiamTrucTiep ?? 0,
                        giamToiDa = v.GiamToiDa ?? 0,
                        discount,
                        eligibleSubtotal,
                        eligibleCount = eligibleItems.Count,
                        hieuLuc = $"{v.NgayBatDau:dd/MM/yyyy} - {v.NgayHetHan:dd/MM/yyyy}"
                    };
                })
                .Where(x => x != null)
                .ToList();

            return Ok(new { ok = true, items });
        }

    }
}