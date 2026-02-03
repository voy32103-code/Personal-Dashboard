using Microsoft.EntityFrameworkCore;
using MyPortfolio.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// KHU VỰC ĐĂNG KÝ DỊCH VỤ (Làm việc ở đây)
// ==========================================

// 1. Đăng ký Database (PostgreSQL)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("MyPortfolio.Infrastructure")));

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

app.UseAuthorization();

app.MapRazorPages();

app.Run();