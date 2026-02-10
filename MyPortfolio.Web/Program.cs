using Microsoft.EntityFrameworkCore;
using MyPortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using MyPortfolio.Web.Hubs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. KHU VỰC ĐĂNG KÝ DỊCH VỤ (Services)
// (Tất cả code builder.Services... phải nằm ở đây)
// ==========================================

// 1.1. Đăng ký Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("MyPortfolio.Infrastructure")));

// 1.2. Đăng ký Identity (Quản lý User)
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// 1.3. Cấu hình Cookie & Upload
builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/Login";
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
});

// 1.4. Đăng ký SignalR & Razor Pages
builder.Services.AddSignalR();
builder.Services.AddRazorPages();

// 1.5. Đăng ký Google Authentication (ĐÃ CHUYỂN LÊN ĐÂY)
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        IConfigurationSection googleAuthNSection = builder.Configuration.GetSection("Authentication:Google");
        options.ClientId = googleAuthNSection["ClientId"];
        options.ClientSecret = googleAuthNSection["ClientSecret"];
    });

// ==========================================
// 2. BUILD ỨNG DỤNG (Chốt sổ Services)
// ==========================================
var app = builder.Build();


// ==========================================
// 3. KHU VỰC PIPELINE (Middleware)
// (Thứ tự các dòng lệnh ở đây RẤT QUAN TRỌNG)
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapHub<MusicHub>("/musicHub");

app.Run();