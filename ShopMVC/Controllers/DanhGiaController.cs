using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // Giả sử bạn dùng Identity
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Models;
using ShopMVC.Models.ViewModels;
using System.Security.Claims; // Để lấy UserId

namespace ShopMVC.Controllers
{
    [Authorize]
    public class DanhGiaController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _hostEnv;
        // Giả sử bạn dùng Identity
        // private readonly UserManager<ApplicationUser> _userManager;

        public DanhGiaController(AppDbContext db, IWebHostEnvironment hostEnv)
        {
            _db = db;
            _hostEnv = hostEnv;
        }

        // ===== GET: HIỂN THỊ FORM ĐÁNH GIÁ =====
        [HttpGet]
        public async Task<IActionResult> Tao(int idSanPham, int idDonHang)
        {
            // Lấy UserId của người đang đăng nhập
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Kiểm tra xem user này có thực sự mua sản phẩm này trong đơn hàng này không
            var chiTietDon = await _db.DonHangChiTiets
                .FirstOrDefaultAsync(ct => ct.IdDonHang == idDonHang
                                        && ct.IdSanPham == idSanPham
                                        && ct.DonHang.UserId == userId); // Quan trọng

            if (chiTietDon == null)
            {
                TempData["error"] = "Không tìm thấy sản phẩm trong đơn hàng của bạn.";
                return RedirectToAction("CuaToi", "DonHang");
            }

            // 2. Kiểm tra xem đã đánh giá sản phẩm này cho đơn hàng này chưa
            var daDanhGia = await _db.DanhGias
                .AnyAsync(d => d.IdDonHang == idDonHang
                            && d.IdSanPham == idSanPham
                            && d.UserId == userId);

            if (daDanhGia)
            {
                TempData["error"] = "Bạn đã đánh giá sản phẩm này cho đơn hàng này rồi.";
                return RedirectToAction("ChiTiet", "DonHang", new { id = idDonHang });
            }

            // 3. Lấy thông tin sản phẩm để hiển thị
            var sanPham = await _db.SanPhams
                .Include(p => p.Anhs)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == idSanPham);

            if (sanPham == null) return NotFound();

            var vm = new DanhGiaVM
            {
                IdSanPham = idSanPham,
                IdDonHang = idDonHang,
                TenSanPham = sanPham.TenDayDu,
                AnhSanPham = sanPham.Anhs
                    .OrderByDescending(a => a.LaAnhChinh)
                    .ThenBy(a => a.ThuTu)
                    .Select(a => a.Url)
                    .FirstOrDefault()
            };

            return View(vm);
        }


        // ===== POST: LƯU ĐÁNH GIÁ =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tao(DanhGiaVM vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kiểm tra lại quyền sở hữu
            var chiTietDon = await _db.DonHangChiTiets
                .FirstOrDefaultAsync(ct => ct.IdDonHang == vm.IdDonHang
                                        && ct.IdSanPham == vm.IdSanPham
                                        && ct.DonHang.UserId == userId);
            if (chiTietDon == null)
            {
                return Forbid(); // Không có quyền
            }

            if (!ModelState.IsValid)
            {
                // Nếu lỗi, cần load lại thông tin sản phẩm để hiển thị form
                var sanPham = await _db.SanPhams
                    .Include(p => p.Anhs)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == vm.IdSanPham);

                if (sanPham != null)
                {
                    vm.TenSanPham = sanPham.TenDayDu;
                    vm.AnhSanPham = sanPham.Anhs
                       .OrderByDescending(a => a.LaAnhChinh)
                       .ThenBy(a => a.ThuTu)
                       .Select(a => a.Url)
                       .FirstOrDefault();
                }
                return View(vm);
            }

            var danhGia = new DanhGia
            {
                IdSanPham = vm.IdSanPham,
                IdDonHang = vm.IdDonHang,
                UserId = userId,
                SoSao = vm.SoSao.Value,
                NoiDung = vm.NoiDung,
                HienThiTen = vm.HienThiTen,
                TrangThai = TrangThaiDanhGia.ChoDuyet, // Mặc định chờ duyệt
                NgayTao = DateTime.Now
            };

            // Xử lý upload hình ảnh
            if (vm.FileHinhAnh != null && vm.FileHinhAnh.Length > 0)
            {
                string uploadsDir = Path.Combine(_hostEnv.WebRootPath, "uploads/reviews");
                Directory.CreateDirectory(uploadsDir); // Tạo thư mục nếu chưa có

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + vm.FileHinhAnh.FileName;
                string filePath = Path.Combine(uploadsDir, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.FileHinhAnh.CopyToAsync(fileStream);
                }
                danhGia.HinhAnh = "/uploads/reviews/" + uniqueFileName; // Lưu đường dẫn web
            }

            _db.DanhGias.Add(danhGia);
            await _db.SaveChangesAsync();

            TempData["success"] = "Gửi đánh giá thành công! Đánh giá của bạn đang chờ duyệt.";
            return RedirectToAction("ChiTiet", "DonHang", new { id = vm.IdDonHang });
        }
    }
}