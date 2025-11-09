using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopMVC.Data;
using ShopMVC.Models;
using ShopMVC.Services;
using ShopMVC.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity + Roles
builder.Services
    .AddIdentity<NguoiDung, IdentityRole>(opt =>
    {
        opt.Password.RequireDigit = false;
        opt.Password.RequireLowercase = false;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequiredLength = 6;
        opt.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
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

    string[] roles = new[] { "Admin", "Staff" };
    foreach (var r in roles)
    {
        if (!await roleMgr.RoleExistsAsync(r))
            await roleMgr.CreateAsync(new IdentityRole(r));
    }

    // ĐỔI email này thành tài khoản m đang đăng nhập
    var adminEmail = "admin@shopmvc.local";
    var adminUser = await userMgr.FindByEmailAsync(adminEmail);

    // Nếu chưa có user này thì tạo mới tạm thời (đổi mật khẩu sau)
    if (adminUser == null)
    {
        adminUser = new NguoiDung
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        var create = await userMgr.CreateAsync(adminUser, "Admin@123"); // mật khẩu thử
        if (!create.Succeeded)
        {
            throw new Exception("Tạo tài khoản admin thất bại: " +
                string.Join("; ", create.Errors.Select(e => e.Description)));
        }
    }

    // Thêm role Admin nếu chưa có
    if (!await userMgr.IsInRoleAsync(adminUser, "Admin"))
        await userMgr.AddToRoleAsync(adminUser, "Admin");
}
// ====== END SEED ======

app.Run();
