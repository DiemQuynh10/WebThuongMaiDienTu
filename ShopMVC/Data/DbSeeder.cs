using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShopMVC.Models;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;

namespace ShopMVC.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await ctx.Database.MigrateAsync();

            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<NguoiDung>>();

            // ===== 0) Roles + Admin
            foreach (var r in new[] { "QuanTri", "Khach" })
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new IdentityRole(r));

            var adminEmail = "admin@shopmvc.local";
            var admin = await userMgr.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
            if (admin == null)
            {
                admin = new NguoiDung { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, HoTen = "Quan Tri Vien" };
                await userMgr.CreateAsync(admin, "Admin@123");
                await userMgr.AddToRoleAsync(admin, "QuanTri");
            }

            // ===== 1) Ensure Category/Brand
            var ensureCats = new (string Ten, string Slug)[]
            {
                ("Dien thoai","dien-thoai"),
                ("Laptop","laptop"),
                ("Thoi trang","thoi-trang"),
                ("Giay dep","giay-dep"),
                ("Phu kien","phu-kien"),
                ("My pham","my-pham")
            };
            foreach (var (ten, slug) in ensureCats)
                if (!await ctx.DanhMucs.AnyAsync(x => x.Slug == slug))
                    ctx.DanhMucs.Add(new DanhMuc { Ten = ten, Slug = slug, HienThi = true, ThuTu = 1 });

            var ensureBrands = new (string Ten, string Slug)[]
            {
                ("Apple","apple"), ("Samsung","samsung"), ("Xiaomi","xiaomi"),
                ("Lenovo","lenovo"), ("ASUS","asus"), ("Sony","sony"),
                ("Owen","owen"), ("BasicWear","basicwear"), ("UrbanFit","urbanfit"),
                ("SneakPeak","sneakpeak"), ("Anker","anker"), ("Logitech","logitech"),
                ("L'Lovely","llovely"), ("Maybelline","maybelline")
            };
            foreach (var (ten, slug) in ensureBrands)
                if (!await ctx.ThuongHieus.AnyAsync(x => x.Slug == slug))
                    ctx.ThuongHieus.Add(new ThuongHieu { Ten = ten, Slug = slug, HienThi = true });

            await ctx.SaveChangesAsync();

            // ===== 2) SEED SẢN PHẨM
            var rnd = new Random();
            string Slug(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "";

                // lowercase + chuẩn hoá Unicode sau đó bỏ toàn bộ dấu
                s = s.ToLowerInvariant()
                     .Replace('đ', 'd')     // riêng 'đ'
                     .Replace('Đ', 'd')
                     .Normalize(NormalizationForm.FormD);

                var sb = new StringBuilder();
                foreach (var ch in s)
                {
                    var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                    if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
                }
                s = sb.ToString().Normalize(NormalizationForm.FormC);

                // thay mọi ký tự không phải a-z0-9 bằng dấu gạch
                s = Regex.Replace(s, @"[^a-z0-9]+", "-").Trim('-');

                // nén bớt nhiều dấu '-' liên tiếp
                s = Regex.Replace(s, "-{2,}", "-");

                return s;
            }

            var catMap = await ctx.DanhMucs.ToDictionaryAsync(x => x.Slug!, x => x.Id);
            var brandMap = await ctx.ThuongHieus.ToDictionaryAsync(x => x.Slug!, x => x.Id);

            // ------ Helper: tạo 1 nhóm biến thể (parent + children) — idempotent theo tên child ------
            async Task SeedVariantGroupAsync(
                string groupName, string catSlug, string brandSlug,
                params (string mau, string? tt2, decimal gia, int tonKho, string displaySuffix)[] variants)
            {
                // Nếu đã có 1 child nào của group -> coi như đã seed (tránh nhân đôi)
                foreach (var v in variants)
                {
                    var fullName = $"{groupName} {v.displaySuffix}".Trim();
                    if (await ctx.SanPhams.AnyAsync(p => p.Ten == fullName))
                        return;
                }

                if (!catMap.TryGetValue(catSlug, out var catId)) return;
                if (!brandMap.TryGetValue(brandSlug, out var brandId)) return;

                // Parent (không bán)
                var parent = new SanPham
                {
                    Ten = groupName,
                    IdDanhMuc = catId,
                    IdThuongHieu = brandId,
                    Gia = 0,
                    GiaKhuyenMai = null,
                    TonKho = 0,
                    TrangThai = TrangThaiHienThi.Hien,
                    IsActive = false,
                    LaNoiBat = rnd.NextDouble() < 0.2,
                    MoTaNgan = $"{groupName} – nhóm biến thể"
                };
                ctx.SanPhams.Add(parent);
                await ctx.SaveChangesAsync();

                // Children (bán được)
                var children = new List<SanPham>();

                foreach (var v in variants)
                {
                    var fullName = $"{groupName} {v.displaySuffix}".Trim();

                    // ---- TÍNH GIÁ KHUYẾN MẠI AN TOÀN CHO TỪNG BIẾN THỂ ----
                    decimal? km = null;
                    if (rnd.NextDouble() < 0.45) // tỉ lệ có KM ~45%
                    {
                        // max giảm = min(15% giá, 300k, giá - 5k để không về 0)
                        int maxDisc = (int)Math.Min(v.gia * 0.15m, 300_000m);
                        maxDisc = Math.Min(maxDisc, (int)(v.gia - 5_000m));

                        // min giảm: 50k nhưng không vượt quá max; sàn 5k
                        int minDisc = Math.Min(50_000, Math.Max(5_000, maxDisc));

                        if (maxDisc >= minDisc && maxDisc > 0)
                        {
                            int disc = rnd.Next(minDisc, maxDisc + 1);
                            km = v.gia - disc;
                        }
                    }
                    // --------------------------------------------------------

                    children.Add(new SanPham
                    {
                        ParentId = parent.Id,
                        Ten = fullName,
                        Mau = v.mau,
                        ThuocTinh2 = v.tt2,
                        IdDanhMuc = catId,
                        IdThuongHieu = brandId,
                        Gia = v.gia,
                        GiaKhuyenMai = km,              // <== dùng km vừa tính
                        TonKho = v.tonKho,
                        TrangThai = TrangThaiHienThi.Hien,
                        IsActive = true,
                        LaNoiBat = rnd.NextDouble() < 0.25,
                        MoTaNgan = $"{fullName} – biến thể của {groupName}"
                    });
                }
                ctx.SanPhams.AddRange(children);
                await ctx.SaveChangesAsync();

                // Ảnh demo cho từng child
                foreach (var c in children)
                {
                    var baseSlug = Slug(c.Ten);
                    int count = rnd.Next(2, 4);
                    for (int i = 1; i <= count; i++)
                    {
                        ctx.AnhSanPhams.Add(new AnhSanPham
                        {
                            IdSanPham = c.Id,
                            Url = $"/images/sp/{baseSlug}-{i}.jpg",
                            LaAnhChinh = (i == 1),
                            ThuTu = i
                        });
                    }
                }
                await ctx.SaveChangesAsync();
            }

            // ------ Các nhóm ĐIỆN THOẠI ------
            await SeedVariantGroupAsync("iPhone 15", "dien-thoai", "apple",
                ("Đen", "128GB", rnd.Next(18_500_000, 22_000_001), rnd.Next(8, 15), "Đen 128GB"),
                ("Hồng", "128GB", rnd.Next(18_500_000, 22_000_001), rnd.Next(5, 12), "Hồng 128GB"),
                ("Đen", "256GB", rnd.Next(20_500_000, 26_000_001), rnd.Next(3, 10), "Đen 256GB")
            );
            await SeedVariantGroupAsync("iPhone 15 Plus", "dien-thoai", "apple",
                ("Đen", "128GB", rnd.Next(21_000_000, 26_000_001), rnd.Next(6, 12), "Đen 128GB"),
                ("Xanh", "256GB", rnd.Next(23_000_000, 28_000_001), rnd.Next(3, 9), "Xanh 256GB")
            );
            await SeedVariantGroupAsync("iPhone 15 Pro", "dien-thoai", "apple",
                ("Titan", "256GB", rnd.Next(26_000_000, 33_000_001), rnd.Next(4, 9), "Titan 256GB"),
                ("Đen", "512GB", rnd.Next(30_000_000, 38_000_001), rnd.Next(2, 7), "Đen 512GB")
            );
            await SeedVariantGroupAsync("Samsung Galaxy S24", "dien-thoai", "samsung",
                ("Đen", "256GB", rnd.Next(15_000_000, 20_000_001), rnd.Next(6, 12), "Đen 256GB"),
                ("Kem", "256GB", rnd.Next(15_000_000, 20_000_001), rnd.Next(6, 12), "Kem 256GB"),
                ("Tím", "512GB", rnd.Next(18_000_000, 23_000_001), rnd.Next(3, 8), "Tím 512GB")
            );
            await SeedVariantGroupAsync("Xiaomi Redmi Note 13 Pro", "dien-thoai", "xiaomi",
                ("Đen", "256GB", rnd.Next(7_000_000, 10_000_001), rnd.Next(8, 16), "Đen 256GB"),
                ("Xanh", "256GB", rnd.Next(7_000_000, 10_000_001), rnd.Next(6, 12), "Xanh 256GB")
            );
            await SeedVariantGroupAsync("Xiaomi 14T Pro", "dien-thoai", "xiaomi",
                ("Đen", "256GB", rnd.Next(12_000_000, 17_000_001), rnd.Next(5, 10), "Đen 256GB"),
                ("Xanh", "512GB", rnd.Next(14_000_000, 19_000_001), rnd.Next(3, 8), "Xanh 512GB")
            );

            // ------ Các nhóm LAPTOP ------
            await SeedVariantGroupAsync("MacBook Air M2 13", "laptop", "apple",
                ("Bạc", "8GB/256GB", rnd.Next(23_000_000, 30_000_001), rnd.Next(6, 12), "Bạc 8GB/256GB"),
                ("Xám", "16GB/512GB", rnd.Next(28_000_000, 36_000_001), rnd.Next(3, 8), "Xám 16GB/512GB")
            );
            await SeedVariantGroupAsync("MacBook Pro M3 14", "laptop", "apple",
                ("Bạc", "16GB/512GB", rnd.Next(42_000_000, 56_000_001), rnd.Next(2, 6), "Bạc 16GB/512GB")
            );
            await SeedVariantGroupAsync("Lenovo ThinkPad X1 Carbon Gen 11", "laptop", "lenovo",
                ("Đen", "16GB/512GB", rnd.Next(28_000_000, 38_000_001), rnd.Next(3, 7), "Đen 16GB/512GB")
            );
            await SeedVariantGroupAsync("ASUS Vivobook 15", "laptop", "asus",
                ("Bạc", "8GB/256GB", rnd.Next(15_000_000, 22_000_001), rnd.Next(4, 10), "Bạc 8GB/256GB"),
                ("Xám", "16GB/512GB", rnd.Next(18_000_000, 26_000_001), rnd.Next(2, 7), "Xám 16GB/512GB")
            );
            await SeedVariantGroupAsync("ASUS ROG Zephyrus G14", "laptop", "asus",
                ("Đen", "16GB/512GB", rnd.Next(36_000_000, 48_000_001), rnd.Next(2, 6), "Đen 16GB/512GB")
            );

            // ------ Các nhóm THỜI TRANG ------
            await SeedVariantGroupAsync("Áo thun Basic Cotton 220G", "thoi-trang", "basicwear",
                ("Trắng", "M", rnd.Next(99_000, 159_001), rnd.Next(8, 20), "Trắng M"),
                ("Trắng", "L", rnd.Next(99_000, 159_001), rnd.Next(6, 16), "Trắng L"),
                ("Đen", "M", rnd.Next(99_000, 159_001), rnd.Next(6, 16), "Đen M")
            );
            await SeedVariantGroupAsync("Áo sơ mi Oxford Slim", "thoi-trang", "urbanfit",
                ("Trắng", "M", rnd.Next(199_000, 299_001), rnd.Next(10, 20), "Trắng M"),
                ("Trắng", "L", rnd.Next(199_000, 299_001), rnd.Next(6, 12), "Trắng L"),
                ("Xanh", "M", rnd.Next(199_000, 299_001), rnd.Next(8, 16), "Xanh M")
            );
            await SeedVariantGroupAsync("Quần jean straight", "thoi-trang", "urbanfit",
                ("Xanh đậm", "29", rnd.Next(299_000, 499_001), rnd.Next(5, 12), "Xanh đậm 29"),
                ("Xanh đậm", "30", rnd.Next(299_000, 499_001), rnd.Next(5, 12), "Xanh đậm 30")
            );
            await SeedVariantGroupAsync("Áo polo Pique", "thoi-trang", "basicwear",
                ("Trắng", "M", rnd.Next(179_000, 279_001), rnd.Next(8, 16), "Trắng M"),
                ("Đen", "L", rnd.Next(179_000, 279_001), rnd.Next(6, 14), "Đen L")
            );
            await SeedVariantGroupAsync("Áo khoác windbreaker", "thoi-trang", "urbanfit",
                ("Xanh", "M", rnd.Next(399_000, 699_001), rnd.Next(4, 10), "Xanh M"),
                ("Đen", "L", rnd.Next(399_000, 699_001), rnd.Next(4, 10), "Đen L")
            );

            // ------ Các nhóm GIÀY ------
            await SeedVariantGroupAsync("Sneaker Daily Run", "giay-dep", "sneakpeak",
                ("Trắng", "39", rnd.Next(499_000, 899_001), rnd.Next(4, 10), "Trắng 39"),
                ("Trắng", "41", rnd.Next(499_000, 899_001), rnd.Next(4, 10), "Trắng 41"),
                ("Đen", "40", rnd.Next(499_000, 899_001), rnd.Next(4, 10), "Đen 40")
            );
            await SeedVariantGroupAsync("Giày lười canvas", "giay-dep", "sneakpeak",
                ("Đen", "40", rnd.Next(399_000, 699_001), rnd.Next(4, 10), "Đen 40"),
                ("Nâu", "41", rnd.Next(399_000, 699_001), rnd.Next(4, 10), "Nâu 41")
            );

            // ------ PHỤ KIỆN / MỸ PHẨM: seed đơn chiếc (không cần nhóm) ------
            async Task SeedSingleAsync(string name, string catSlug, string brandSlug, int min, int max)
            {
                if (await ctx.SanPhams.AnyAsync(p => p.Ten == name)) return;
                if (!catMap.TryGetValue(catSlug, out var catId)) return;
                if (!brandMap.TryGetValue(brandSlug, out var brandId)) return;

                var gia = rnd.Next(min, max + 1);
                decimal? km = (rnd.NextDouble() < 0.5)
                    ? gia - rnd.Next(20_000, 120_001)
                    : null;

                ctx.SanPhams.Add(new SanPham
                {
                    Ten = name,
                    IdDanhMuc = catId,
                    IdThuongHieu = brandId,
                    Gia = gia,
                    GiaKhuyenMai = km,
                    TonKho = rnd.Next(10, 60),
                    TrangThai = TrangThaiHienThi.Hien,
                    IsActive = true,
                    LaNoiBat = rnd.NextDouble() < 0.15,
                    MoTaNgan = $"{name} – {brandSlug} thuộc {catSlug}"
                });
                await ctx.SaveChangesAsync();
            }

            await SeedSingleAsync("Tai nghe Sony WH-CH520", "phu-kien", "sony", 1_200_000, 1_800_000);
            await SeedSingleAsync("Chuột không dây Silent", "phu-kien", "logitech", 180_000, 390_000);
            await SeedSingleAsync("Bàn phím cơ 87 phím", "phu-kien", "logitech", 850_000, 1_600_000);
            await SeedSingleAsync("Sạc nhanh 35W USB-C", "phu-kien", "anker", 250_000, 450_000);

            await SeedSingleAsync("Son lì L'Lovely Matte", "my-pham", "llovely", 150_000, 280_000);
            await SeedSingleAsync("Kem chống nắng SPF50+", "my-pham", "maybelline", 180_000, 320_000);
            await SeedSingleAsync("Sữa rửa mặt dịu nhẹ", "my-pham", "maybelline", 120_000, 220_000);

            // ===== 3) Ảnh còn thiếu -> bơm thêm
            var noImg = await ctx.SanPhams
                .Where(p => !ctx.AnhSanPhams.Any(a => a.IdSanPham == p.Id))
                .Select(p => new { p.Id, p.Ten })
                .ToListAsync();

            var newImgs = new List<AnhSanPham>();
            foreach (var p in noImg)
            {
                var baseSlug = Slug(p.Ten);
                int count = rnd.Next(2, 4);
                for (int i = 1; i <= count; i++)
                {
                    newImgs.Add(new AnhSanPham
                    {
                        IdSanPham = p.Id,
                        Url = $"/images/sp/{baseSlug}-{i}.jpg",
                        LaAnhChinh = (i == 1),
                        ThuTu = i
                    });
                }
            }
            if (newImgs.Count > 0)
            {
                await ctx.AnhSanPhams.AddRangeAsync(newImgs);
                await ctx.SaveChangesAsync();
            }

            // ===== 4) Voucher mẫu
            if (!await ctx.Vouchers.AnyAsync())
            {
                var today = DateTime.Today;
                ctx.Vouchers.AddRange(
                    new Voucher { Code = "SALE10", Ten = "Giảm 10% toàn bộ đơn hàng", PhanTramGiam = 10, GiamToiDa = 100000, NgayBatDau = today, NgayHetHan = today.AddMonths(1), IsActive = true },
                    new Voucher { Code = "LESS50K", Ten = "Giảm trực tiếp 50.000đ", GiamTrucTiep = 50000, NgayBatDau = today, NgayHetHan = today.AddMonths(1), SoLanSuDungToiDa = 100, IsActive = true }
                );
                await ctx.SaveChangesAsync();
            }

            // ===== 5) Log
            Console.WriteLine($"[Seeder] Done. Products: {await ctx.SanPhams.CountAsync()}, Images: {await ctx.AnhSanPhams.CountAsync()}");
        }
    }
}
