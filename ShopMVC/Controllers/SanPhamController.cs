using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Models;
using ShopMVC.Models.ViewModels;

namespace ShopMVC.Controllers
{
    public class SanPhamController : Controller
    {
        private readonly AppDbContext _db;
        public SanPhamController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(
             int? idDanhMuc, int? idThuongHieu,
             decimal? giaMin, decimal? giaMax,
             string? tuKhoa, string? sapXep,
             int page = 1, int pageSize = 12)
        {
            // 1) Base query + Include cho thẻ/ảnh
            var baseQuery = _db.SanPhams
                .Include(p => p.ThuongHieu)
                .Include(p => p.DanhMuc)
                .Include(p => p.Anhs)
                .Where(p => p.TrangThai == TrangThaiHienThi.Hien
                        && p.IsActive
                        && (p.ThuongHieu == null || p.ThuongHieu.HienThi))
                .AsQueryable();

            // 2) Bộ lọc
            if (idDanhMuc.HasValue)
                baseQuery = baseQuery.Where(p => p.IdDanhMuc == idDanhMuc.Value);

            if (idThuongHieu.HasValue)
                baseQuery = baseQuery.Where(p => p.IdThuongHieu == idThuongHieu.Value);

            if (giaMin.HasValue)
                baseQuery = baseQuery.Where(p => p.Gia >= giaMin.Value);

            if (giaMax.HasValue)
                baseQuery = baseQuery.Where(p => p.Gia <= giaMax.Value);

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var kw = tuKhoa.Trim().ToLower();
                baseQuery = baseQuery.Where(p => p.Ten.ToLower().Contains(kw)
                                              || (p.MoTaNgan ?? "").ToLower().Contains(kw));
            }

            // 3) Sắp xếp
            sapXep = sapXep?.ToLower();
            baseQuery = sapXep switch
            {
                "gia-asc" => baseQuery.OrderBy(p => p.Gia),
                "gia-desc" => baseQuery.OrderByDescending(p => p.Gia),
                "moi" => baseQuery.OrderByDescending(p => p.Id),
                _ => baseQuery.OrderBy(p => p.Id)
            };

            // 4) Gom nhóm
            var groupQuery = baseQuery
                .Select(p => new
                {
                    GroupId = p.ParentId ?? p.Id,
                    Prod = p
                })
                .GroupBy(x => x.GroupId);

            // 5) Đếm tổng
            var totalGroups = await groupQuery.CountAsync();

            // 6) Lấy Id đại diện
            var repIds = await groupQuery
                .Select(g => g
                    .OrderBy(x => x.Prod.Gia)
                    .Select(x => x.Prod.Id)
                    .First())
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 7) Lấy thực thể đại diện
            var repsDict = await _db.SanPhams
                .Include(p => p.ThuongHieu)
                .Include(p => p.DanhMuc)
                .Include(p => p.Anhs)
                .Where(p => repIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            var reps = repIds
                .Where(id => repsDict.ContainsKey(id))
                .Select(id => repsDict[id])
                .ToList();

            // 8) Lấy map biến thể
            var parentIds = reps.Select(r => r.ParentId ?? r.Id).Distinct().ToList();
            var siblingRaw = await _db.SanPhams
                .Include(x => x.Anhs)
                .Where(x => x.IsActive
                          && x.TrangThai == TrangThaiHienThi.Hien
                          && parentIds.Contains(x.ParentId ?? x.Id))
                .Select(x => new
                {
                    GroupId = x.ParentId ?? x.Id,
                    x.Id,
                    x.Mau,
                    x.ThuocTinh2,
                    FirstImg = x.Anhs.OrderByDescending(a => a.LaAnhChinh).ThenBy(a => a.ThuTu).Select(a => a.Url).FirstOrDefault()
                })
                .ToListAsync();

            var siblingsMap = siblingRaw
                .GroupBy(x => x.GroupId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(v => new
                    {
                        v.Id,
                        v.Mau,
                        v.ThuocTinh2,
                        v.FirstImg
                    }).ToList()
                );

            // =========================================================================
            // [CODE MỚI] LẤY DỮ LIỆU FLASH SALE ĐỂ HIỂN THỊ RA VIEW
            // =========================================================================
            var activeFlashSale = await _db.Vouchers
                .Where(v => v.IsFlashSale && v.IsActive && DateTime.Now >= v.NgayBatDau && DateTime.Now <= v.NgayHetHan)
                .OrderByDescending(v => v.NgayBatDau)
                .FirstOrDefaultAsync();

            // Dictionary để map: ProductID -> Thông tin Flash Sale của sản phẩm đó
            var flashSaleMap = new Dictionary<int, VoucherSanPham>();

            if (activeFlashSale != null)
            {
                // Lấy tất cả sản phẩm trong đợt Flash Sale này
                var fsItems = await _db.VoucherSanPhams
                    .Where(vp => vp.VoucherId == activeFlashSale.Id)
                    .ToListAsync();

                foreach (var item in fsItems)
                {
                    if (!flashSaleMap.ContainsKey(item.SanPhamId))
                    {
                        flashSaleMap[item.SanPhamId] = item;
                    }
                }
            }
            // Truyền qua ViewBag
            ViewBag.FlashSaleMap = flashSaleMap;
            // =========================================================================

            var vm = new SanPhamListVM
            {
                Items = reps,
                DanhMucs = await _db.DanhMucs.Where(x => x.HienThi).OrderBy(x => x.ThuTu).ToListAsync(),
                ThuongHieus = await _db.ThuongHieus.Where(x => x.HienThi).OrderBy(x => x.Ten).ToListAsync(),
                IdDanhMuc = idDanhMuc,
                IdThuongHieu = idThuongHieu,
                GiaMin = giaMin,
                GiaMax = giaMax,
                TuKhoa = tuKhoa,
                SapXep = sapXep,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalGroups
            };

            ViewBag.SiblingsMap = siblingsMap;

            return View(vm);
        }

        // ... (Các hàm ChiTiet, BienThe, RangeDayMonth giữ nguyên không đổi) ...
        static string RangeDayMonth(DateTime from, DateTime to)
        {
            string Th(int m) => "Th" + m;
            if (from.Month == to.Month) return $"{from.Day}–{to.Day} {Th(from.Month)}";
            return $"{from:dd/MM}–{to:dd/MM}";
        }

        public async Task<IActionResult> ChiTiet(int? id, string? slug, bool openChat = false)
        {
            var query = _db.SanPhams
                .Include(p => p.ThuongHieu)
                .Include(p => p.DanhMuc)
                .Include(p => p.Anhs)
                .AsQueryable();

            query = query.Where(p => p.TrangThai == TrangThaiHienThi.Hien
                                  && (p.ThuongHieu == null || p.ThuongHieu.HienThi)
                                  && p.IsActive);

            SanPham? sp = null;
            if (id.HasValue)
                sp = await query.FirstOrDefaultAsync(p => p.Id == id.Value);
            else if (!string.IsNullOrWhiteSpace(slug))
                sp = await query.FirstOrDefaultAsync(p => p.Ten.Replace(' ', '-').ToLower() == slug!.ToLower());

            if (sp == null) return NotFound();

            // Sắp xếp ảnh
            sp.Anhs = sp.Anhs
                .OrderByDescending(a => a.LaAnhChinh)
                .ThenBy(a => a.ThuTu)
                .ToList();

            // Lấy các biến thể (siblings)
            var parentId = sp.ParentId ?? sp.Id;
            var siblings = await _db.SanPhams
                .Include(x => x.Anhs)
                .Where(x => x.IsActive && x.TrangThai == TrangThaiHienThi.Hien
                          && (x.ParentId == parentId || x.Id == parentId))
                .OrderBy(x => x.Mau).ThenBy(x => x.ThuocTinh2)
                .ToListAsync();

            // Label thuộc tính 2 theo danh mục
            string? label2 = null;
            var slugDm = sp.DanhMuc?.Slug?.ToLower();
            if (slugDm == "thoi-trang") label2 = "Size";
            else if (slugDm == "dien-thoai") label2 = "Dung lượng";
            else if (slugDm == "laptop") label2 = "RAM/SSD";

            int leadMinDays = 2;
            int leadMaxDays = 4;
            var today = DateTime.Today;
            var etaFrom = today.AddDays(leadMinDays);
            var etaTo = today.AddDays(leadMaxDays);

            var vm = new ProductDetailsVM
            {
                Product = sp,
                Siblings = siblings,
                ShippingEtaText = RangeDayMonth(etaFrom, etaTo),
                ThuocTinh2Label = label2
            };

            // ================== THÊM ĐOẠN NÀY ==================
            // Lấy danh sách đánh giá cho sản phẩm này
            var danhGias = await _db.DanhGias
                .Where(d => d.IdSanPham == sp.Id)
                .OrderByDescending(d => d.NgayTao)
                .ToListAsync();

            ViewBag.DanhGiasSP = danhGias;
            // ===================================================

            ViewBag.OpenChat = openChat;
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> BienThe(int id)
        {
            var sp = await _db.SanPhams
                .Include(x => x.Anhs)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive && x.TrangThai == TrangThaiHienThi.Hien);
            if (sp == null) return NotFound();

            var imgs = sp.Anhs.OrderByDescending(a => a.LaAnhChinh).ThenBy(a => a.ThuTu).Select(a => a.Url).ToList();
            string GiaFmt(decimal v) => string.Format("{0:n0} đ", v);

            return Json(new
            {
                id = sp.Id,
                ten = sp.Ten,
                gia = sp.Gia,
                giaKhuyenMai = sp.GiaKhuyenMai,
                giaText = sp.GiaKhuyenMai.HasValue ? GiaFmt(sp.GiaKhuyenMai.Value) : GiaFmt(sp.Gia),
                giaGocText = sp.GiaKhuyenMai.HasValue ? GiaFmt(sp.Gia) : null,
                tonKho = sp.TonKho,
                moTaNgan = sp.MoTaNgan,
                images = imgs
            });
        }
    }
}