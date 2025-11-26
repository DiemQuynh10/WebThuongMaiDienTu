using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Areas.Admin.ViewModels;
using ShopMVC.Data;
using ShopMVC.Models;
using ShopMVC.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class VoucherController : Controller
    {
        private readonly AppDbContext _db;

        public VoucherController(AppDbContext db)
        {
            _db = db;
        }

        // ================== HÀM DÙNG CHUNG: VALIDATE NGHIỆP VỤ VOUCHER ==================
        private void ValidateVoucherCore(VoucherCreateViewModel model)
        {
            // Ngày hết hạn phải >= ngày bắt đầu
            if (model.NgayHetHan.Date < model.NgayBatDau.Date)
            {
                ModelState.AddModelError(nameof(model.NgayHetHan),
                    "Ngày hết hạn phải lớn hơn hoặc bằng ngày bắt đầu.");
            }

            // Bắt buộc phải có hoặc % giảm, hoặc giảm trực tiếp
            var percent = model.PhanTramGiam ?? 0;
            var fixedAmount = model.GiamTrucTiep ?? 0;

            if (percent <= 0 && fixedAmount <= 0)
            {
                ModelState.AddModelError("",
                    "Phải nhập giảm theo % hoặc giảm trực tiếp (ít nhất một trong hai > 0).");
            }

            // Phần trăm giảm phải trong khoảng 0–100
            if (percent < 0 || percent > 100)
            {
                ModelState.AddModelError(nameof(model.PhanTramGiam),
                    "Phần trăm giảm phải trong khoảng 0–100.");
            }

            // Giảm tối đa không được âm
            var maxDiscount = model.GiamToiDa ?? 0;
            if (maxDiscount < 0)
            {
                ModelState.AddModelError(nameof(model.GiamToiDa),
                    "Giảm tối đa không được âm.");
            }

            // Flash Sale nên có giới hạn lượt sử dụng
            if (model.IsFlashSale && model.SoLanSuDungToiDa == 0)
            {
                ModelState.AddModelError(nameof(model.SoLanSuDungToiDa),
                    "Flash Sale phải có giới hạn số lượt sử dụng (SoLanSuDungToiDa > 0).");
            }
        }

        // ================== 1. DANH SÁCH VOUCHER ==================
        public async Task<IActionResult> Index(string? q, bool? active, string? time, string? usage, int page = 1, int pageSize = 20)
        {
            var now = DateTime.Now.Date;

            var query = _db.Vouchers
                .Include(v => v.VoucherThuongHieus).ThenInclude(x => x.ThuongHieu)
                .Include(v => v.VoucherDanhMucs).ThenInclude(x => x.DanhMuc)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(v => v.Code.Contains(q) || v.Ten.Contains(q));
            }

            if (active.HasValue)
            {
                query = query.Where(v => v.IsActive == active.Value);
            }

            // Lọc theo thời gian
            if (time == "valid")
            {
                query = query.Where(v => now >= v.NgayBatDau && now <= v.NgayHetHan);
            }
            else if (time == "expired")
            {
                query = query.Where(v => now > v.NgayHetHan);
            }
            else if (time == "soon")
            {
                var soon = now.AddDays(3);
                query = query.Where(v => now <= v.NgayHetHan && v.NgayHetHan <= soon);
            }

            // Lọc theo số lần sử dụng
            if (usage == "available")
            {
                query = query.Where(v => v.SoLanSuDungToiDa == 0 || v.SoLanDaSuDung < v.SoLanSuDungToiDa);
            }
            else if (usage == "exhausted")
            {
                query = query.Where(v => v.SoLanSuDungToiDa > 0 && v.SoLanDaSuDung >= v.SoLanSuDungToiDa);
            }

            if (page < 1) page = 1;
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Q = q;
            ViewBag.Active = active;
            ViewBag.Time = time;
            ViewBag.Usage = usage;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;

            return View(items);
        }

        // ================== 2. TẠO VOUCHER THƯỜNG ==================
        public async Task<IActionResult> Create()
        {
            var vm = new VoucherCreateViewModel
            {
                NgayBatDau = DateTime.Now,
                NgayHetHan = DateTime.Now.AddDays(7),
                IsActive = true,
                IsFlashSale = false,
                AvailableBrands = await _db.ThuongHieus.Where(x => x.HienThi).ToListAsync(),
                AvailableCategories = await _db.DanhMucs.Where(x => x.HienThi).ToListAsync()
            };

            ViewBag.Action = "Create";
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VoucherCreateViewModel model)
        {
            // Check trùng code
            if (await _db.Vouchers.AnyAsync(x => x.Code == model.Code))
            {
                ModelState.AddModelError(nameof(model.Code), "Mã voucher này đã tồn tại.");
            }

            ValidateVoucherCore(model);

            // Flash sale thì không cho gán brand/category (chỉ theo sản phẩm)
            if (model.IsFlashSale)
            {
                model.SelectedBrandIds = null;
                model.SelectedCategoryIds = null;
            }

            if (ModelState.IsValid)
            {
                var voucher = new Voucher
                {
                    Code = model.Code?.Trim().ToUpper() ?? "",
                    Ten = model.Ten,
                    PhanTramGiam = model.PhanTramGiam,
                    GiamTrucTiep = model.GiamTrucTiep,
                    GiamToiDa = model.GiamToiDa,
                    NgayBatDau = model.NgayBatDau,
                    NgayHetHan = model.NgayHetHan,
                    SoLanSuDungToiDa = model.SoLanSuDungToiDa,
                    IsActive = model.IsActive,
                    IsFlashSale = model.IsFlashSale,
                    SoLanDaSuDung = 0
                };

                _db.Vouchers.Add(voucher);
                await _db.SaveChangesAsync();

                // Gán brand/category cho voucher thường
                if (!voucher.IsFlashSale)
                {
                    if (model.SelectedBrandIds != null)
                    {
                        foreach (var id in model.SelectedBrandIds)
                        {
                            _db.VoucherThuongHieus.Add(new VoucherThuongHieu
                            {
                                VoucherId = voucher.Id,
                                ThuongHieuId = id
                            });
                        }
                    }

                    if (model.SelectedCategoryIds != null)
                    {
                        foreach (var id in model.SelectedCategoryIds)
                        {
                            _db.VoucherDanhMucs.Add(new VoucherDanhMuc
                            {
                                VoucherId = voucher.Id,
                                DanhMucId = id
                            });
                        }
                    }

                    await _db.SaveChangesAsync();
                }

                if (voucher.IsFlashSale)
                {
                    // Flash sale -> chuyển sang trang gán sản phẩm
                    return RedirectToAction(nameof(AddProducts), new { id = voucher.Id });
                }

                TempData["success"] = "Tạo voucher thành công.";
                return RedirectToAction(nameof(Index));
            }

            model.AvailableBrands = await _db.ThuongHieus.Where(x => x.HienThi).ToListAsync();
            model.AvailableCategories = await _db.DanhMucs.Where(x => x.HienThi).ToListAsync();
            ViewBag.Action = "Create";
            return View(model);
        }

        // ================== 3. TẠO FLASH SALE RIÊNG ==================
        public IActionResult CreateFlashSale()
        {
            var vm = new VoucherCreateViewModel
            {
                Code = "FLASH_" + DateTime.Now.ToString("ddHHmm"),
                Ten = "Flash Sale " + DateTime.Now.ToString("dd/MM"),
                NgayBatDau = DateTime.Now,
                NgayHetHan = DateTime.Now.AddDays(1),
                IsFlashSale = true,
                IsActive = true,
                SoLanSuDungToiDa = 1000,
                AvailableBrands = new List<ThuongHieu>(),
                AvailableCategories = new List<DanhMuc>()
            };

            ViewBag.Action = "Create";
            return View("CreateFlashSale", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFlashSale(VoucherCreateViewModel model)
        {
            if (await _db.Vouchers.AnyAsync(x => x.Code == model.Code))
            {
                ModelState.AddModelError(nameof(model.Code), "Mã này đã tồn tại.");
            }

            model.IsFlashSale = true; // ép là flash sale
            ValidateVoucherCore(model);

            if (ModelState.IsValid)
            {
                var voucher = new Voucher
                {
                    Code = model.Code?.Trim().ToUpper() ?? "",
                    Ten = model.Ten,
                    PhanTramGiam = model.PhanTramGiam,
                    GiamTrucTiep = model.GiamTrucTiep,
                    GiamToiDa = model.GiamToiDa,
                    NgayBatDau = model.NgayBatDau,
                    NgayHetHan = model.NgayHetHan,
                    SoLanSuDungToiDa = model.SoLanSuDungToiDa,
                    IsActive = model.IsActive,
                    IsFlashSale = true,
                    SoLanDaSuDung = 0
                };

                _db.Vouchers.Add(voucher);
                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(AddProducts), new { id = voucher.Id });
            }

            return View("CreateFlashSale", model);
        }

        // ================== 4. EDIT VOUCHER ==================
        public async Task<IActionResult> Edit(int id)
        {
            var v = await _db.Vouchers
                .Include(x => x.VoucherThuongHieus)
                .Include(x => x.VoucherDanhMucs)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (v == null) return NotFound();

            var vm = new VoucherCreateViewModel
            {
                Id = v.Id,
                Code = v.Code,
                Ten = v.Ten,
                PhanTramGiam = v.PhanTramGiam,
                GiamTrucTiep = v.GiamTrucTiep,
                GiamToiDa = v.GiamToiDa,
                NgayBatDau = v.NgayBatDau,
                NgayHetHan = v.NgayHetHan,
                SoLanSuDungToiDa = v.SoLanSuDungToiDa,
                IsActive = v.IsActive,
                IsFlashSale = v.IsFlashSale,
                RowVersion = v.RowVersion,
                AvailableBrands = await _db.ThuongHieus.Where(x => x.HienThi).ToListAsync(),
                AvailableCategories = await _db.DanhMucs.Where(x => x.HienThi).ToListAsync(),
                SelectedBrandIds = v.VoucherThuongHieus.Select(x => x.ThuongHieuId).ToList(),
                SelectedCategoryIds = v.VoucherDanhMucs.Select(x => x.DanhMucId).ToList()
            };

            ViewBag.Action = "Edit";

            if (v.IsFlashSale)
            {
                return View("CreateFlashSale", vm);
            }

            return View("Create", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VoucherCreateViewModel model)
        {
            if (await _db.Vouchers.AnyAsync(x => x.Code == model.Code && x.Id != model.Id))
            {
                ModelState.AddModelError(nameof(model.Code), "Mã này đã tồn tại.");
            }

            ValidateVoucherCore(model);

            var v = await _db.Vouchers
                .Include(x => x.VoucherThuongHieus)
                .Include(x => x.VoucherDanhMucs)
                .FirstOrDefaultAsync(x => x.Id == model.Id);

            if (v == null) return NotFound();

            // Flash sale vẫn được validate như bình thường, nhưng không cho sửa cờ IsFlashSale
            if (ModelState.IsValid)
            {
                v.Code = model.Code?.Trim().ToUpper() ?? "";
                v.Ten = model.Ten;
                v.PhanTramGiam = model.PhanTramGiam;
                v.GiamTrucTiep = model.GiamTrucTiep;
                v.GiamToiDa = model.GiamToiDa;
                v.NgayBatDau = model.NgayBatDau;
                v.NgayHetHan = model.NgayHetHan;
                v.SoLanSuDungToiDa = model.SoLanSuDungToiDa;
                v.IsActive = model.IsActive;

                if (model.RowVersion != null)
                {
                    _db.Entry(v).Property(x => x.RowVersion).OriginalValue = model.RowVersion;
                }

                // Voucher thường: update brand/category
                if (!v.IsFlashSale)
                {
                    _db.VoucherThuongHieus.RemoveRange(v.VoucherThuongHieus);
                    _db.VoucherDanhMucs.RemoveRange(v.VoucherDanhMucs);

                    if (model.SelectedBrandIds != null)
                    {
                        foreach (var id in model.SelectedBrandIds)
                        {
                            _db.VoucherThuongHieus.Add(new VoucherThuongHieu
                            {
                                VoucherId = v.Id,
                                ThuongHieuId = id
                            });
                        }
                    }

                    if (model.SelectedCategoryIds != null)
                    {
                        foreach (var id in model.SelectedCategoryIds)
                        {
                            _db.VoucherDanhMucs.Add(new VoucherDanhMuc
                            {
                                VoucherId = v.Id,
                                DanhMucId = id
                            });
                        }
                    }
                }

                try
                {
                    await _db.SaveChangesAsync();
                    TempData["success"] = "Cập nhật voucher thành công.";

                    if (v.IsFlashSale)
                    {
                        return RedirectToAction(nameof(AddProducts), new { id = v.Id });
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Dữ liệu đã thay đổi, vui lòng tải lại trang và thử lại.");
                }
            }

            model.AvailableBrands = await _db.ThuongHieus.Where(x => x.HienThi).ToListAsync();
            model.AvailableCategories = await _db.DanhMucs.Where(x => x.HienThi).ToListAsync();
            ViewBag.Action = "Edit";

            return View(model.IsFlashSale ? "CreateFlashSale" : "Create", model);
        }

        // ================== 5. QUẢN LÝ SẢN PHẨM FLASH SALE ==================
        public async Task<IActionResult> AddProducts(int id)
        {
            var v = await _db.Vouchers.FindAsync(id);
            if (v == null || !v.IsFlashSale) return NotFound();

            var existIds = await _db.VoucherSanPhams
                .Where(vp => vp.VoucherId == id)
                .Select(vp => vp.SanPhamId)
                .ToListAsync();

            var products = await _db.SanPhams
                .Where(p => !existIds.Contains(p.Id) && p.Gia > 0)
                .Select(p => new
                {
                    p.Id,
                    Ten = p.Ten,
                    Gia = p.Gia,
                    TonKho = p.TonKho
                })
                .ToListAsync();

            ViewBag.Voucher = v;
            ViewBag.Products = products;

            var added = await _db.VoucherSanPhams
                .Include(vp => vp.SanPham).ThenInclude(p => p.Anhs)
                .Where(vp => vp.VoucherId == id)
                .OrderByDescending(vp => vp.VoucherId)
                .ToListAsync();

            return View(added);
        }

        [HttpPost]
        public async Task<IActionResult> AddProductToVoucher(int voucherId, int productId, int quantity, decimal? salePrice)
        {
            try
            {
                var voucher = await _db.Vouchers.FindAsync(voucherId);
                if (voucher == null || !voucher.IsFlashSale)
                    return Json(new { success = false, message = "Voucher không hợp lệ hoặc không phải flash sale." });

                var product = await _db.SanPhams
                    .Include(x => x.ChiTietSanPhams)
                    .FirstOrDefaultAsync(x => x.Id == productId);

                if (product == null)
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });

                if (quantity <= 0)
                    return Json(new { success = false, message = "Số lượng phải lớn hơn 0." });

                if (product.TonKho > 0 && quantity > product.TonKho)
                    return Json(new { success = false, message = "Số lượng flash sale không được vượt quá tồn kho." });

                // Check sản phẩm đã tồn tại trong voucher này chưa
                bool existInThisVoucher = await _db.VoucherSanPhams
                    .AnyAsync(x => x.VoucherId == voucherId && x.SanPhamId == productId);
                if (existInThisVoucher)
                    return Json(new { success = false, message = "Sản phẩm này đã có trong flash sale." });

                // Check sản phẩm có đang nằm trong flash sale khác trùng thời gian
                bool inAnotherFlash = await _db.VoucherSanPhams
                    .Include(vp => vp.Voucher)
                    .AnyAsync(vp =>
                        vp.SanPhamId == productId &&
                        vp.VoucherId != voucherId &&
                        vp.Voucher.IsFlashSale &&
                        vp.Voucher.IsActive &&
                        vp.Voucher.NgayBatDau <= voucher.NgayHetHan &&
                        voucher.NgayBatDau <= vp.Voucher.NgayHetHan
                    );

                if (inAnotherFlash)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm đang tham gia một Flash Sale khác trùng thời gian."
                    });
                }

                // Xác định giá gốc
                decimal originalPrice = product.Gia > 0
                    ? product.Gia
                    : (product.ChiTietSanPhams.Any()
                        ? product.ChiTietSanPhams.Min(c => c.Gia)
                        : 0);

                if (originalPrice <= 0)
                {
                    return Json(new { success = false, message = "Không xác định được giá gốc của sản phẩm." });
                }

                decimal finalPrice;

                if (salePrice.HasValue && salePrice.Value > 0)
                {
                    // Admin nhập tay
                    finalPrice = salePrice.Value;
                }
                else
                {
                    // Tính tự động theo voucher
                    decimal discountAmount = 0;
                    if (voucher.PhanTramGiam > 0)
                    {
                        decimal percent = (decimal)(voucher.PhanTramGiam ?? 0);
                        discountAmount = originalPrice * percent / 100m;
                    }
                    else if (voucher.GiamTrucTiep > 0)
                    {
                        discountAmount = (decimal)(voucher.GiamTrucTiep ?? 0);
                    }

                    // Giảm tối đa
                    if (voucher.GiamToiDa > 0 && discountAmount > voucher.GiamToiDa)
                    {
                        discountAmount = (decimal)voucher.GiamToiDa;
                    }

                    finalPrice = originalPrice - discountAmount;
                }

                // Không cho giá sale > giá gốc
                if (finalPrice > originalPrice)
                {
                    finalPrice = originalPrice;
                }

                if (finalPrice < 0)
                {
                    finalPrice = 0;
                }

                var item = new VoucherSanPham
                {
                    VoucherId = voucherId,
                    SanPhamId = productId,
                    SoLuongPhanBo = quantity,
                    SoLuongDaBan = 0,
                    GiaGiam = finalPrice
                };

                _db.VoucherSanPhams.Add(item);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Thêm sản phẩm vào Flash Sale thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveProductFromFlashSale(int voucherId, int productId)
        {
            try
            {
                var item = await _db.VoucherSanPhams
                    .FirstOrDefaultAsync(x => x.VoucherId == voucherId && x.SanPhamId == productId);

                if (item == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong Flash Sale." });
                }

                _db.VoucherSanPhams.Remove(item);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xoá sản phẩm khỏi Flash Sale." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ================== 6. DELETE / TOGGLE / BULK TOGGLE ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var v = await _db.Vouchers.FindAsync(id);
            if (v == null) return RedirectToAction(nameof(Index));

            bool inUse = await _db.DonHangs.AnyAsync(d => d.VoucherId == id);
            if (inUse)
            {
                TempData["Err"] = "Voucher đã được sử dụng, không thể xoá. Hãy tắt kích hoạt thay vì xoá.";
                return RedirectToAction(nameof(Index));
            }

            _db.Vouchers.Remove(v);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Đã xoá voucher.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var v = await _db.Vouchers.FindAsync(id);
            if (v != null)
            {
                var now = DateTime.Now.Date;

                if (v.IsActive)
                {
                    v.IsActive = false;
                    TempData["Ok"] = "Đã tắt voucher.";
                }
                else
                {
                    if (now > v.NgayHetHan.Date)
                    {
                        TempData["Err"] = "Voucher đã hết hạn, không thể bật lại.";
                    }
                    else
                    {
                        v.IsActive = true;
                        TempData["Ok"] = "Đã bật voucher.";
                    }
                }

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkToggle([FromForm] int[] ids, [FromForm] bool enable)
        {
            if (ids != null && ids.Length > 0)
            {
                var now = DateTime.Now.Date;
                var items = await _db.Vouchers.Where(v => ids.Contains(v.Id)).ToListAsync();

                foreach (var v in items)
                {
                    if (enable)
                    {
                        if (now <= v.NgayHetHan.Date)
                        {
                            v.IsActive = true;
                        }
                    }
                    else
                    {
                        v.IsActive = false;
                    }
                }

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ================== 7. XEM LỊCH SỬ SỬ DỤNG VOUCHER ==================
        public async Task<IActionResult> Usage(int id, int page = 1, int pageSize = 20)
        {
            var v = await _db.Vouchers.FindAsync(id);
            if (v == null) return NotFound();

            var q = _db.DonHangs
                .Where(d => d.VoucherId == id)
                .OrderByDescending(d => d.NgayDat);

            if (page < 1) page = 1;

            var total = await q.CountAsync();
            var orders = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;

            var vm = new VoucherUsageVM
            {
                Voucher = v,
                Orders = orders
            };

            return View(vm);
        }
    }
}
