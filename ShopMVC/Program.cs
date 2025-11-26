using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // Thêm dòng này
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Models;
using ShopMVC.Services;
using ShopMVC.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// DÒNG BỊ LỖI ĐÃ BỊ XÓA Ở ĐÂY
builder.Services.AddSignalR();
// Identity + Roles (Tất cả cấu hình gộp lại trong khối này)
builder.Services
    .AddIdentity<NguoiDung, IdentityRole>(opt => // <-- ĐĂNG KÝ USER VÀ ROLE CÙNG LÚC
    {
        // Cấu hình Password
        opt.Password.RequireDigit = false;
        opt.Password.RequireLowercase = false;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequiredLength = 6;

        // Cấu hình User
        opt.User.RequireUniqueEmail = true;

        // Tùy chọn Sign In (được chuyển từ AddDefaultIdentity xuống)
        opt.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppDbContext>() // <== ĐĂNG KÝ STORE CHO CẢ USER VÀ ROLE
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.Cookie.Name = ".ShopMVC.Session";
    opt.IdleTimeout = TimeSpan.FromHours(2);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapHub<ShopMVC.Hubs.ChatHub>("/chatHub");
app.UseSession();

// Routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ====== SEED ROLES + GÁN ADMIN ======
using (var scope = app.Services.CreateScope())
{
    var sv = scope.ServiceProvider;

    // 1) migrate + seed dữ liệu cũ (nếu m đang dùng)
    await DbSeeder.SeedAsync(sv);

    // 2) seed roles + add admin
    var roleMgr = sv.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = sv.GetRequiredService<UserManager<NguoiDung>>();

    // Đổi Admin thành QuanTri (để đồng bộ với DbSeeder) nếu cần, hoặc ngược lại
    string[] roles = new[] { "QuanTri", "Staff" }; // Hoặc "Admin" tùy vào hệ thống bạn dùng

    foreach (var r in roles)
    {
        if (!await roleMgr.RoleExistsAsync(r))
            await roleMgr.CreateAsync(new IdentityRole(r));
    }

    var adminEmail = "admin@shopmvc.local";
    var adminUser = await userMgr.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new NguoiDung
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            // Thêm HoTen để đồng bộ với DbSeeder
            HoTen = "Quan Tri Vien"
        };
        var create = await userMgr.CreateAsync(adminUser, "Admin@123");
        if (!create.Succeeded)
        {
            throw new Exception("Tạo tài khoản admin thất bại: " +
                string.Join("; ", create.Errors.Select(e => e.Description)));
        }
    }

    // Thêm role Admin (hoặc QuanTri) nếu chưa có
    if (!await userMgr.IsInRoleAsync(adminUser, "QuanTri")) // <== Sử dụng tên role đã tạo
        await userMgr.AddToRoleAsync(adminUser, "QuanTri");
}
// ====== END SEED ======

app.Run();