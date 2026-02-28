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

// 1.5. Đăng ký Google Authentication
// 1.5. Đăng ký Google Authentication
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        IConfigurationSection googleAuthNSection = builder.Configuration.GetSection("Authentication:Google");
        options.ClientId = googleAuthNSection["ClientId"];
        options.ClientSecret = googleAuthNSection["ClientSecret"];

        // 🔥 THÊM ĐOẠN NÀY ĐỂ ÉP GOOGLE LUÔN HỎI LẠI TÀI KHOẢN 🔥
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            // Thêm tham số prompt=select_account vào URL gửi đi Google
            context.Response.Redirect(context.RedirectUri + "&prompt=select_account");
            return Task.CompletedTask;
        };
    });

// 1.6. CẤU HÌNH REDIS (Phải nằm ở đây, TRƯỚC khi Build) 🔥
var redisConnection = builder.Configuration.GetConnectionString("RedisConnection")
                      ?? Environment.GetEnvironmentVariable("RedisConnection");

if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "MyPortfolio_"; // Tiền tố để không bị trùng key
    });
}
else
{
    // Nếu chưa có Redis thì chạy tạm bộ nhớ trong (để không bị lỗi khi dev)
    builder.Services.AddDistributedMemoryCache();
}

// ==========================================
// 2. BUILD ỨNG DỤNG (Chốt sổ Services)
// ==========================================
var app = builder.Build();


// ==========================================
// 3. KHU VỰC PIPELINE (Middleware)
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Xác thực & Phân quyền phải nằm sau Routing
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<MusicHub>("/musicHub");

// Tự động Migrate Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<MyPortfolio.Infrastructure.Data.ApplicationDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi khởi tạo Database.");
    }
}

app.Run();