using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShopMVC.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DiaChiMacDinh = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Banners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenBanner = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HinhAnh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    HienThi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DanhMucs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DanhMucChaId = table.Column<int>(type: "int", nullable: true),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    HienThi = table.Column<bool>(type: "bit", nullable: false),
                    IconUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhMucs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DanhMucs_DanhMucs_DanhMucChaId",
                        column: x => x.DanhMucChaId,
                        principalTable: "DanhMucs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ThuongHieus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HienThi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThuongHieus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vouchers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhanTramGiam = table.Column<double>(type: "float", nullable: true),
                    GiamToiDa = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GiamTrucTiep = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayHetHan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoLanSuDungToiDa = table.Column<int>(type: "int", nullable: false),
                    SoLanDaSuDung = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFlashSale = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vouchers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SanPhams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DisplaySuffix = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    MoTaNgan = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    MoTaChiTiet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GiaKhuyenMai = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TonKho = table.Column<int>(type: "int", nullable: false),
                    LaNoiBat = table.Column<bool>(type: "bit", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Mau = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ThuocTinh2 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SKU = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IdDanhMuc = table.Column<int>(type: "int", nullable: false),
                    IdThuongHieu = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanPhams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SanPhams_DanhMucs_IdDanhMuc",
                        column: x => x.IdDanhMuc,
                        principalTable: "DanhMucs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SanPhams_SanPhams_ParentId",
                        column: x => x.ParentId,
                        principalTable: "SanPhams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SanPhams_ThuongHieus_IdThuongHieu",
                        column: x => x.IdThuongHieu,
                        principalTable: "ThuongHieus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonHangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDon = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HoTenNhan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DienThoaiNhan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiaChiNhan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PhiVanChuyen = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienGiam = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongTruocGiam = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongThanhToan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PhuongThucThanhToan = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NgayDat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    VoucherId = table.Column<int>(type: "int", nullable: true),
                    VoucherCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonHangs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonHangs_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VoucherDanhMucs",
                columns: table => new
                {
                    VoucherId = table.Column<int>(type: "int", nullable: false),
                    DanhMucId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoucherDanhMucs", x => new { x.VoucherId, x.DanhMucId });
                    table.ForeignKey(
                        name: "FK_VoucherDanhMucs_DanhMucs_DanhMucId",
                        column: x => x.DanhMucId,
                        principalTable: "DanhMucs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoucherDanhMucs_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoucherThuongHieus",
                columns: table => new
                {
                    VoucherId = table.Column<int>(type: "int", nullable: false),
                    ThuongHieuId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoucherThuongHieus", x => new { x.VoucherId, x.ThuongHieuId });
                    table.ForeignKey(
                        name: "FK_VoucherThuongHieus_ThuongHieus_ThuongHieuId",
                        column: x => x.ThuongHieuId,
                        principalTable: "ThuongHieus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoucherThuongHieus_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnhSanPhams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdSanPham = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LaAnhChinh = table.Column<bool>(type: "bit", nullable: false),
                    ThuTu = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnhSanPhams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnhSanPhams_SanPhams_IdSanPham",
                        column: x => x.IdSanPham,
                        principalTable: "SanPhams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserConnectionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ThoiGianTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaDong = table.Column<bool>(type: "bit", nullable: false),
                    SanPhamId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChatSessions_SanPhams_SanPhamId",
                        column: x => x.SanPhamId,
                        principalTable: "SanPhams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChiTietSanPhams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SanPhamId = table.Column<int>(type: "int", nullable: false),
                    TenChiTiet = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoLuongKho = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietSanPhams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietSanPhams_SanPhams_SanPhamId",
                        column: x => x.SanPhamId,
                        principalTable: "SanPhams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoucherSanPhams",
                columns: table => new
                {
                    VoucherId = table.Column<int>(type: "int", nullable: false),
                    SanPhamId = table.Column<int>(type: "int", nullable: false),
                    GiaGiam = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SoLuongPhanBo = table.Column<int>(type: "int", nullable: false),
                    SoLuongDaBan = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoucherSanPhams", x => new { x.VoucherId, x.SanPhamId });
                    table.ForeignKey(
                        name: "FK_VoucherSanPhams_SanPhams_SanPhamId",
                        column: x => x.SanPhamId,
                        principalTable: "SanPhams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoucherSanPhams_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DanhGias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdSanPham = table.Column<int>(type: "int", nullable: false),
                    IdDonHang = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoSao = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HinhAnh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HienThiTen = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DanhGias_DonHangs_IdDonHang",
                        column: x => x.IdDonHang,
                        principalTable: "DonHangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DanhGias_SanPhams_IdSanPham",
                        column: x => x.IdSanPham,
                        principalTable: "SanPhams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DonHangChiTiets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdDonHang = table.Column<int>(type: "int", nullable: false),
                    IdSanPham = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonHangChiTiets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonHangChiTiets_DonHangs_IdDonHang",
                        column: x => x.IdDonHang,
                        principalTable: "DonHangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DonHangChiTiets_SanPhams_IdSanPham",
                        column: x => x.IdSanPham,
                        principalTable: "SanPhams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonHangLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDH = table.Column<int>(type: "int", nullable: false),
                    From = table.Column<int>(type: "int", nullable: false),
                    To = table.Column<int>(type: "int", nullable: false),
                    ByUser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonHangId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonHangLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonHangLogs_DonHangs_DonHangId",
                        column: x => x.DonHangId,
                        principalTable: "DonHangs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DonHangNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDH = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonHangId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonHangNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonHangNotes_DonHangs_DonHangId",
                        column: x => x.DonHangId,
                        principalTable: "DonHangs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrderStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsOverride = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusHistories_DonHangs_OrderId",
                        column: x => x.OrderId,
                        principalTable: "DonHangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatSessionId = table.Column<int>(type: "int", nullable: false),
                    Sender = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DanhMucs",
                columns: new[] { "Id", "DanhMucChaId", "HienThi", "IconUrl", "MoTa", "Slug", "Ten", "ThuTu" },
                values: new object[,]
                {
                    { 1, null, true, null, null, "dien-thoai", "Dien thoai", 1 },
                    { 2, null, true, null, null, "laptop", "Laptop", 2 },
                    { 3, null, true, null, null, "thoi-trang", "Thoi trang", 3 },
                    { 4, null, true, null, null, "giay-dep", "Giay dep", 4 },
                    { 5, null, true, null, null, "phu-kien", "Phu kien", 5 },
                    { 6, null, true, null, null, "my-pham", "My pham", 6 }
                });

            migrationBuilder.InsertData(
                table: "ThuongHieus",
                columns: new[] { "Id", "HienThi", "LogoUrl", "MoTa", "Slug", "Ten" },
                values: new object[,]
                {
                    { 1, true, null, null, "apple", "Apple" },
                    { 2, true, null, null, "samsung", "Samsung" },
                    { 3, true, null, null, "xiaomi", "Xiaomi" },
                    { 4, true, null, null, "lenovo", "Lenovo" },
                    { 5, true, null, null, "asus", "ASUS" },
                    { 6, true, null, null, "sony", "Sony" },
                    { 7, true, null, null, "owen", "Owen" },
                    { 8, true, null, null, "basicwear", "BasicWear" },
                    { 9, true, null, null, "urbanfit", "UrbanFit" },
                    { 10, true, null, null, "sneakpeak", "SneakPeak" },
                    { 11, true, null, null, "anker", "Anker" },
                    { 12, true, null, null, "logitech", "Logitech" },
                    { 13, true, null, null, "llovely", "L'Lovely" },
                    { 14, true, null, null, "maybelline", "Maybelline" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnhSanPhams_IdSanPham",
                table: "AnhSanPhams",
                column: "IdSanPham");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatSessionId",
                table: "ChatMessages",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_SanPhamId",
                table: "ChatSessions",
                column: "SanPhamId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_UserId",
                table: "ChatSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietSanPhams_SanPhamId",
                table: "ChiTietSanPhams",
                column: "SanPhamId");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGias_IdDonHang",
                table: "DanhGias",
                column: "IdDonHang");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGias_IdSanPham",
                table: "DanhGias",
                column: "IdSanPham");

            migrationBuilder.CreateIndex(
                name: "IX_DanhMucs_DanhMucChaId",
                table: "DanhMucs",
                column: "DanhMucChaId");

            migrationBuilder.CreateIndex(
                name: "IX_DanhMucs_Slug",
                table: "DanhMucs",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangChiTiets_IdDonHang",
                table: "DonHangChiTiets",
                column: "IdDonHang");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangChiTiets_IdSanPham",
                table: "DonHangChiTiets",
                column: "IdSanPham");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangLogs_DonHangId",
                table: "DonHangLogs",
                column: "DonHangId");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangNotes_DonHangId",
                table: "DonHangNotes",
                column: "DonHangId");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangs_VoucherId",
                table: "DonHangs",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_OrderId",
                table: "OrderStatusHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_IdDanhMuc",
                table: "SanPhams",
                column: "IdDanhMuc");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_IdDanhMuc_IdThuongHieu",
                table: "SanPhams",
                columns: new[] { "IdDanhMuc", "IdThuongHieu" });

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_IdThuongHieu",
                table: "SanPhams",
                column: "IdThuongHieu");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_LaNoiBat",
                table: "SanPhams",
                column: "LaNoiBat");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_ParentId",
                table: "SanPhams",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_ParentId_Mau_ThuocTinh2",
                table: "SanPhams",
                columns: new[] { "ParentId", "Mau", "ThuocTinh2" });

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_Ten",
                table: "SanPhams",
                column: "Ten");

            migrationBuilder.CreateIndex(
                name: "IX_ThuongHieus_Slug",
                table: "ThuongHieus",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_VoucherDanhMucs_DanhMucId",
                table: "VoucherDanhMucs",
                column: "DanhMucId");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Code",
                table: "Vouchers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoucherSanPhams_SanPhamId",
                table: "VoucherSanPhams",
                column: "SanPhamId");

            migrationBuilder.CreateIndex(
                name: "IX_VoucherThuongHieus_ThuongHieuId",
                table: "VoucherThuongHieus",
                column: "ThuongHieuId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnhSanPhams");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Banners");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ChiTietSanPhams");

            migrationBuilder.DropTable(
                name: "DanhGias");

            migrationBuilder.DropTable(
                name: "DonHangChiTiets");

            migrationBuilder.DropTable(
                name: "DonHangLogs");

            migrationBuilder.DropTable(
                name: "DonHangNotes");

            migrationBuilder.DropTable(
                name: "OrderStatusHistories");

            migrationBuilder.DropTable(
                name: "VoucherDanhMucs");

            migrationBuilder.DropTable(
                name: "VoucherSanPhams");

            migrationBuilder.DropTable(
                name: "VoucherThuongHieus");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "ChatSessions");

            migrationBuilder.DropTable(
                name: "DonHangs");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "SanPhams");

            migrationBuilder.DropTable(
                name: "Vouchers");

            migrationBuilder.DropTable(
                name: "DanhMucs");

            migrationBuilder.DropTable(
                name: "ThuongHieus");
        }
    }
}
