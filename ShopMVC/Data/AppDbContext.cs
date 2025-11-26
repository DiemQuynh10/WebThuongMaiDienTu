using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Models;
using System.Reflection.Emit;

namespace ShopMVC.Data
{
    public class AppDbContext : IdentityDbContext<NguoiDung>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets
        public DbSet<DanhMuc> DanhMucs => Set<DanhMuc>();
        public DbSet<ThuongHieu> ThuongHieus => Set<ThuongHieu>();
        public DbSet<SanPham> SanPhams => Set<SanPham>();
        public DbSet<AnhSanPham> AnhSanPhams => Set<AnhSanPham>();
        public DbSet<DonHang> DonHangs => Set<DonHang>();
        public DbSet<DonHangChiTiet> DonHangChiTiets => Set<DonHangChiTiet>();
        public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
        public DbSet<DonHangNote> DonHangNotes => Set<DonHangNote>();
        public DbSet<DonHangLog> DonHangLogs => Set<DonHangLog>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public DbSet<VoucherThuongHieu> VoucherThuongHieus => Set<VoucherThuongHieu>();
        public DbSet<VoucherDanhMuc> VoucherDanhMucs => Set<VoucherDanhMuc>();
        public DbSet<VoucherSanPham> VoucherSanPhams => Set<VoucherSanPham>();

        // ------------------------------------
        // ... các DbSet khác
        public DbSet<DanhGia> DanhGias { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChiTietSanPham> ChiTietSanPhams { get; set; }
        public DbSet<Banner> Banners { get; set; }
        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // ====== CONFIG / INDEX ======
            b.Entity<DanhMuc>().HasIndex(x => x.Slug).IsUnique(false);
            b.Entity<ThuongHieu>().HasIndex(x => x.Slug).IsUnique(false);

            b.Entity<SanPham>().HasIndex(x => x.Ten);
            b.Entity<SanPham>().HasIndex(x => x.LaNoiBat);
            b.Entity<SanPham>().HasIndex(x => new { x.IdDanhMuc, x.IdThuongHieu });
            // index phục vụ nhóm biến thể
            b.Entity<SanPham>().HasIndex(x => new { x.ParentId, x.Mau, x.ThuocTinh2 });
            // (optional) ràng buộc độ dài nếu chưa có
            b.Entity<SanPham>().Property(x => x.Mau).HasMaxLength(64);
            b.Entity<SanPham>().Property(x => x.ThuocTinh2).HasMaxLength(64);

            // ====== PRECISION TIỀN ======
            b.Entity<SanPham>().Property(x => x.Gia).HasColumnType("decimal(18,2)");
            b.Entity<SanPham>().Property(x => x.GiaKhuyenMai).HasColumnType("decimal(18,2)");
            b.Entity<DonHang>().Property(x => x.PhiVanChuyen).HasColumnType("decimal(18,2)");
            b.Entity<DonHang>().Property(x => x.TienGiam).HasColumnType("decimal(18,2)");
            b.Entity<DonHang>().Property(x => x.TongTruocGiam).HasColumnType("decimal(18,2)");
            b.Entity<DonHang>().Property(x => x.TongThanhToan).HasColumnType("decimal(18,2)");
            b.Entity<DonHangChiTiet>().Property(x => x.DonGia).HasColumnType("decimal(18,2)");
            b.Entity<DonHangChiTiet>().Property(x => x.ThanhTien).HasColumnType("decimal(18,2)");
            b.Entity<Voucher>().Property(v => v.GiamTrucTiep).HasColumnType("decimal(18,2)");
            b.Entity<Voucher>().Property(v => v.GiamToiDa).HasColumnType("decimal(18,2)");

            // ====== DEFAULT TIME ======
            b.Entity<SanPham>().Property(x => x.NgayTao).HasDefaultValueSql("GETUTCDATE()");
            b.Entity<SanPham>().Property(x => x.NgayCapNhat).HasDefaultValueSql("GETUTCDATE()");
            b.Entity<DonHang>().Property(x => x.NgayDat).HasDefaultValueSql("GETUTCDATE()");
            b.Entity<DonHang>().Property(x => x.NgayCapNhat).HasDefaultValueSql("GETUTCDATE()");

            // ====== QUAN HỆ ======
       
            b.Entity<SanPham>()
                .HasOne(x => x.DanhMuc)
                .WithMany(dm => dm.SanPhams)
                .HasForeignKey(x => x.IdDanhMuc)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<SanPham>()
                .HasOne(x => x.ThuongHieu)
                .WithMany(th => th.SanPhams)
                .HasForeignKey(x => x.IdThuongHieu)
                .OnDelete(DeleteBehavior.Restrict);

            // self-FK cho ParentId (không cần navigation cũng được)
            b.Entity<SanPham>()
     .HasOne(p => p.Parent)
     .WithMany(p => p.Children)
     .HasForeignKey(p => p.ParentId)
     .OnDelete(DeleteBehavior.Restrict);

            b.Entity<AnhSanPham>()
                .HasOne(a => a.SanPham)
                .WithMany(p => p.Anhs)
                .HasForeignKey(a => a.IdSanPham)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<DonHangChiTiet>()
                .HasOne(ct => ct.DonHang)
                .WithMany(d => d.ChiTiets)
                .HasForeignKey(ct => ct.IdDonHang)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<DonHangChiTiet>()
                .HasOne(ct => ct.SanPham)
                .WithMany()
                .HasForeignKey(ct => ct.IdSanPham)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<DonHang>()
                .HasOne<Voucher>()
                .WithMany()
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.SetNull);

            b.Entity<OrderStatusHistory>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Order)
                 .WithMany()
                 .HasForeignKey(x => x.OrderId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.Property(x => x.ReasonCode).HasMaxLength(64);
                e.Property(x => x.ChangedByUserId).HasMaxLength(64);
            });

            // Voucher mapping M-N
            b.Entity<Voucher>().HasIndex(v => v.Code).IsUnique();
            b.Entity<VoucherThuongHieu>().HasKey(x => new { x.VoucherId, x.ThuongHieuId });
            b.Entity<VoucherThuongHieu>()
                .HasOne(x => x.Voucher).WithMany(v => v.VoucherThuongHieus).HasForeignKey(x => x.VoucherId);
            b.Entity<VoucherThuongHieu>()
                .HasOne(x => x.ThuongHieu).WithMany().HasForeignKey(x => x.ThuongHieuId);

            b.Entity<VoucherDanhMuc>().HasKey(x => new { x.VoucherId, x.DanhMucId });
            b.Entity<VoucherDanhMuc>()
                .HasOne(x => x.Voucher).WithMany(v => v.VoucherDanhMucs).HasForeignKey(x => x.VoucherId);
            b.Entity<VoucherDanhMuc>()
                .HasOne(x => x.DanhMuc).WithMany().HasForeignKey(x => x.DanhMucId);

            b.Entity<VoucherSanPham>().HasKey(x => new { x.VoucherId, x.SanPhamId });

            b.Entity<VoucherSanPham>()
                .HasOne(x => x.Voucher)
                .WithMany(v => v.VoucherSanPhams)
                .HasForeignKey(x => x.VoucherId);

            b.Entity<VoucherSanPham>()
                .HasOne(x => x.SanPham)
                .WithMany()
                .HasForeignKey(x => x.SanPhamId);

            b.Entity<VoucherSanPham>().Property(x => x.GiaGiam).HasColumnType("decimal(18,2)");



            // ====== SEED nền tảng (Category/Brand) – giữ như cũ ======
            b.Entity<DanhMuc>().HasData(
                new DanhMuc { Id = 1, Ten = "Dien thoai", Slug = "dien-thoai", HienThi = true, ThuTu = 1 },
                new DanhMuc { Id = 2, Ten = "Laptop", Slug = "laptop", HienThi = true, ThuTu = 2 },
                new DanhMuc { Id = 3, Ten = "Thoi trang", Slug = "thoi-trang", HienThi = true, ThuTu = 3 },
                new DanhMuc { Id = 4, Ten = "Giay dep", Slug = "giay-dep", HienThi = true, ThuTu = 4 },
                new DanhMuc { Id = 5, Ten = "Phu kien", Slug = "phu-kien", HienThi = true, ThuTu = 5 },
                new DanhMuc { Id = 6, Ten = "My pham", Slug = "my-pham", HienThi = true, ThuTu = 6 }
            );

            b.Entity<ThuongHieu>().HasData(
                new ThuongHieu { Id = 1, Ten = "Apple", Slug = "apple", HienThi = true },
                new ThuongHieu { Id = 2, Ten = "Samsung", Slug = "samsung", HienThi = true },
                new ThuongHieu { Id = 3, Ten = "Xiaomi", Slug = "xiaomi", HienThi = true },
                new ThuongHieu { Id = 4, Ten = "Lenovo", Slug = "lenovo", HienThi = true },
                new ThuongHieu { Id = 5, Ten = "ASUS", Slug = "asus", HienThi = true },
                new ThuongHieu { Id = 6, Ten = "Sony", Slug = "sony", HienThi = true },
                new ThuongHieu { Id = 7, Ten = "Owen", Slug = "owen", HienThi = true },
                new ThuongHieu { Id = 8, Ten = "BasicWear", Slug = "basicwear", HienThi = true },
                new ThuongHieu { Id = 9, Ten = "UrbanFit", Slug = "urbanfit", HienThi = true },
                new ThuongHieu { Id = 10, Ten = "SneakPeak", Slug = "sneakpeak", HienThi = true },
                new ThuongHieu { Id = 11, Ten = "Anker", Slug = "anker", HienThi = true },
                new ThuongHieu { Id = 12, Ten = "Logitech", Slug = "logitech", HienThi = true },
                new ThuongHieu { Id = 13, Ten = "L'Lovely", Slug = "llovely", HienThi = true },
                new ThuongHieu { Id = 14, Ten = "Maybelline", Slug = "maybelline", HienThi = true }
            );

            // KHÔNG HasData cho SanPham/AnhSanPham ở đây.
        }
    }
}
