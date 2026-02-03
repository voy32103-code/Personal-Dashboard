using Microsoft.EntityFrameworkCore;
using MyPortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
var builder = WebApplication.CreateBuilder(args);

// ==========================================
// KHU VỰC ĐĂNG KÝ DỊCH VỤ (Làm việc ở đây)
// ==========================================

// 1. Đăng ký Database (PostgreSQL)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("MyPortfolio.Infrastructure")));

// 2. Đăng ký Identity (Quản lý User)
builder.Services.AddDefaultIdentity<Microsoft.AspNetCore.Identity.IdentityUser>(options =>
{
    // Cấu hình password cho dễ chịu (bỏ yêu cầu ký tự đặc biệt)
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/Login";
});

// 2. Đăng ký Razor Pages
builder.Services.AddRazorPages();

// ==========================================
var app = builder.Build();
// ==========================================


// KHU VỰC CẤU HÌNH PIPELINE (HTTPS, Routing...)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // 1. Kiểm tra "Bạn là ai?"
app.UseAuthorization();  // 2. Kiểm tra "Bạn có quyền không?"
app.MapRazorPages();

app.Run();