using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Models;
using ShopMVC.Models.ViewModels;

namespace ShopMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class VoucherController : Controller
    {
        private readonly AppDbContext _db;
        public VoucherController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(
    string? q,                 // tìm theo Code/Tên
    bool? active,              // chỉ voucher đang bật/tắt
    string? time,              // "valid" | "expired" | "soon"
    string? usage,             // "available" | "exhausted"
    int page = 1, int pageSize = 20)
        {
            var now = DateTime.Now.Date;

            var query = _db.Vouchers
                .Include(v => v.VoucherThuongHieus).ThenInclude(x => x.ThuongHieu)
                .Include(v => v.VoucherDanhMucs).ThenInclude(x => x.DanhMuc)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(v => v.Code.Contains(q) || v.Ten.Contains(q));
            }

            if (active.HasValue)
                query = query.Where(v => v.IsActive == active.Value);

            // time filter
            if (!string.IsNullOrEmpty(time))
            {
                if (time == "valid") query = query.Where(v => now >= v.NgayBatDau && now <= v.NgayHetHan);
                if (time == "expired") query = query.Where(v => now > v.NgayHetHan);
                if (time == "soon")    // sắp hết hạn: còn <= 3 ngày
                {
                    var soon = now.AddDays(3);
                    query = query.Where(v => now <= v.NgayHetHan && v.NgayHetHan <= soon);
                }
            }

            // usage filter
            if (!string.IsNullOrEmpty(usage))
            {
                if (usage == "available") query = query.Where(v => v.SoLanSuDungToiDa == 0 || v.SoLanDaSuDung < v.SoLanSuDungToiDa);
                if (usage == "exhausted") query = query.Where(v => v.SoLanSuDungToiDa > 0 && v.SoLanDaSuDung >= v.SoLanSuDungToiDa);
            }

            var total = await query.CountAsync();
            if (page < 1) page = 1;
            var items = await query
                .OrderByDescending(v => v.NgayBatDau)
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
        public async Task<IActionResult> Create()
        {
            var today = DateTime.Today;
            var vm = new VoucherEditVM
            {
                Voucher = new Voucher
                {
                    NgayBatDau = today,
                    NgayHetHan = today.AddDays(7),
                    IsActive = true
                },
                AllThuongHieus = await _db.ThuongHieus.OrderBy(x => x.Ten).ToListAsync(),
                AllDanhMucs = await _db.DanhMucs.OrderBy(x => x.Ten).ToListAsync()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VoucherEditVM vm)
        {
            if (!ModelState.IsValid)
            {
                // debug
                Console.WriteLine("LỖI MODELSTATE:");
                foreach (var err in ModelState)
                    Console.WriteLine($"{err.Key}: {string.Join(", ", err.Value.Errors.Select(e => e.ErrorMessage))}");
            }
            vm.Voucher.Code = vm.Voucher.Code?.Trim().ToUpper() ?? "";

            if (await _db.Vouchers.AnyAsync(x => x.Code == vm.Voucher.Code))
                ModelState.AddModelError("Voucher.Code", "Mã này đã tồn tại.");

            if (!ModelState.IsValid)
            {
                // QUAN TRỌNG: nạp lại danh sách để View hiển thị
                vm.AllThuongHieus = await _db.ThuongHieus.OrderBy(x => x.Ten).ToListAsync();
                vm.AllDanhMucs = await _db.DanhMucs.OrderBy(x => x.Ten).ToListAsync();
                return View(vm);
            }

            _db.Vouchers.Add(vm.Voucher);
            await _db.SaveChangesAsync();

            if (vm.SelectedThuongHieuIds?.Any() == true)
                _db.VoucherThuongHieus.AddRange(
                    vm.SelectedThuongHieuIds.Select(id => new VoucherThuongHieu { VoucherId = vm.Voucher.Id, ThuongHieuId = id }));

            if (vm.SelectedDanhMucIds?.Any() == true)
                _db.VoucherDanhMucs.AddRange(
                    vm.SelectedDanhMucIds.Select(id => new VoucherDanhMuc { VoucherId = vm.Voucher.Id, DanhMucId = id }));

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Edit(int id)
        {
            var v = await _db.Vouchers
                .Include(x => x.VoucherThuongHieus)
                .Include(x => x.VoucherDanhMucs)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return NotFound();

            var vm = new VoucherEditVM
            {
                Voucher = v,
                SelectedThuongHieuIds = v.VoucherThuongHieus.Select(x => x.ThuongHieuId).ToList(),
                SelectedDanhMucIds = v.VoucherDanhMucs.Select(x => x.DanhMucId).ToList(),
                AllThuongHieus = await _db.ThuongHieus.OrderBy(x => x.Ten).ToListAsync(),
                AllDanhMucs = await _db.DanhMucs.OrderBy(x => x.Ten).ToListAsync()
            };
            var hasOrders = await _db.DonHangs.AnyAsync(d => d.VoucherId == id);
            ViewBag.HasOrders = hasOrders;
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VoucherEditVM vm)
        {
            vm.Voucher.Code = vm.Voucher.Code?.Trim().ToUpper() ?? "";
            var dup = await _db.Vouchers.AnyAsync(x => x.Code == vm.Voucher.Code && x.Id != vm.Voucher.Id);
            if (dup) ModelState.AddModelError("Voucher.Code", "Mã này đã tồn tại.");

            if (!ModelState.IsValid)
            {
                vm.AllThuongHieus = await _db.ThuongHieus.OrderBy(x => x.Ten).ToListAsync();
                vm.AllDanhMucs = await _db.DanhMucs.OrderBy(x => x.Ten).ToListAsync();
                return View(vm);
            }

            // 1) Nạp voucher hiện có + navs
            var v = await _db.Vouchers
                .Include(x => x.VoucherThuongHieus)
                .Include(x => x.VoucherDanhMucs)
                .FirstOrDefaultAsync(x => x.Id == vm.Voucher.Id);
            if (v == null) return NotFound();

            // 2) Map scalar
            v.Code = (vm.Voucher.Code ?? v.Code).Trim().ToUpper();
            v.Ten = vm.Voucher.Ten ?? v.Ten;
            v.PhanTramGiam = vm.Voucher.PhanTramGiam;
            v.GiamTrucTiep = vm.Voucher.GiamTrucTiep;
            v.GiamToiDa = vm.Voucher.GiamToiDa;
            v.NgayBatDau = vm.Voucher.NgayBatDau;
            v.NgayHetHan = vm.Voucher.NgayHetHan;
            v.SoLanSuDungToiDa = vm.Voucher.SoLanSuDungToiDa;
            v.IsActive = vm.Voucher.IsActive;

            // 3) Gắn RowVersion gốc để EF kiểm concurrency
            if (vm.Voucher.RowVersion != null)
                _db.Entry(v).Property(x => x.RowVersion).OriginalValue = vm.Voucher.RowVersion;

            // 4) Cập nhật bảng liên kết
            var newBrandIds = vm.SelectedThuongHieuIds?.ToHashSet() ?? new HashSet<int>();
            var newCateIds = vm.SelectedDanhMucIds?.ToHashSet() ?? new HashSet<int>();

            // --- BRAND ---
            var removeBrands = v.VoucherThuongHieus
                .Where(x => !newBrandIds.Contains(x.ThuongHieuId))
                .ToList();                               // materialize để không sửa khi đang duyệt
            foreach (var rb in removeBrands)
                v.VoucherThuongHieus.Remove(rb);

            var addBrandIds = newBrandIds
                .Where(id => !v.VoucherThuongHieus.Any(x => x.ThuongHieuId == id));
            foreach (var id in addBrandIds)
                v.VoucherThuongHieus.Add(new VoucherThuongHieu { VoucherId = v.Id, ThuongHieuId = id });

            // --- CATEGORY ---
            var removeCates = v.VoucherDanhMucs
                .Where(x => !newCateIds.Contains(x.DanhMucId))
                .ToList();
            foreach (var rc in removeCates)
                v.VoucherDanhMucs.Remove(rc);

            var addCateIds = newCateIds
                .Where(id => !v.VoucherDanhMucs.Any(x => x.DanhMucId == id));
            foreach (var id in addCateIds)
                v.VoucherDanhMucs.Add(new VoucherDanhMuc { VoucherId = v.Id, DanhMucId = id });


            // 5) Lưu
            try
            {
                await _db.SaveChangesAsync();
                TempData["ok"] = "Đã cập nhật voucher.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "Voucher đã bị thay đổi bởi người khác. Vui lòng tải lại và thử lại.");
                vm.AllThuongHieus = await _db.ThuongHieus.OrderBy(x => x.Ten).ToListAsync();
                vm.AllDanhMucs = await _db.DanhMucs.OrderBy(x => x.Ten).ToListAsync();
                return View(vm);
            }
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var v = await _db.Vouchers.FindAsync(id);
            if (v == null) return NotFound();
            v.IsActive = !v.IsActive;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkToggle([FromForm] int[] ids, [FromForm] bool enable)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["err"] = "Chưa chọn voucher nào.";
                return RedirectToAction(nameof(Index));
            }

            var items = await _db.Vouchers.Where(v => ids.Contains(v.Id)).ToListAsync();
            foreach (var v in items) v.IsActive = enable;

            await _db.SaveChangesAsync();
            TempData["ok"] = enable ? "Đã bật các voucher đã chọn." : "Đã tắt các voucher đã chọn.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Usage(int id, int page = 1, int pageSize = 20)
        {
            var v = await _db.Vouchers.FindAsync(id);
            if (v == null) return NotFound();

            var q = _db.DonHangs
                .Where(d => d.VoucherId == id)
                .OrderByDescending(d => d.NgayDat);

            var total = await q.CountAsync();
            var orders = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Total = total; ViewBag.Page = page; ViewBag.PageSize = pageSize;

            var vm = new VoucherUsageVM { Voucher = v, Orders = orders };
            return View(vm);
        }


    }
}
