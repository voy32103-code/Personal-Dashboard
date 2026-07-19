using Microsoft.EntityFrameworkCore;
using MyPortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using MyPortfolio.Web.Hubs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using FluentValidation;
using FluentValidation.AspNetCore;
using QuestPDF.Infrastructure;
using MyPortfolio.Web.Infrastructure;

// M-5: Set QuestPDF license 1 lần lúc startup — KHÔNG set trong Page Model constructor
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. KHU VỰC ĐĂNG KÝ DỊCH VỤ (Services)
// 
// ==========================================

// 1.1. Đăng ký Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("MyPortfolio.Infrastructure")));

// 1.2. Đăng ký Identity (Quản lý User)
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // Password Policy — đủ mạnh cho production
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    // Lockout — khóa tài khoản sau 5 lần nhập sai
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
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

// 1.4. Đăng ký SignalR, Razor Pages, Web API & Swagger
builder.Services.AddSignalR();
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1.5. Đăng ký FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// 1.6. Đăng ký Application Services
// H-3: IFileUploadService — tập trung logic upload, validate, xóa file, tránh copy-paste
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

// 1.7. Đăng ký Google Authentication
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        IConfigurationSection googleAuthNSection = builder.Configuration.GetSection("Authentication:Google");
        options.ClientId = googleAuthNSection["ClientId"];
        options.ClientSecret = googleAuthNSection["ClientSecret"];

        //  THÊM ĐOẠN NÀY ĐỂ ÉP GOOGLE LUÔN HỎI LẠI TÀI KHOẢN 
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            // Thêm tham số prompt=select_account vào URL gửi đi Google
            context.Response.Redirect(context.RedirectUri + "&prompt=select_account");
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OwnerOnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim(System.Security.Claims.ClaimTypes.Email, "voy32103@gmail.com"));
});


// 1.6. CẤU HÌNH REDIS (Phải nằm ở đây, TRƯỚC khi Build) 
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
// --- THÊM ĐOẠN NÀY ĐỂ XỬ LÝ PROXY CỦA RENDER ---
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Xóa các proxy mặc định để cho phép Render load balancer truyền header vào
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    options.ForwardLimit = 1; // Chỉ tin tưởng 1 hop từ Render Load Balancer
});
builder.Services.AddResponseCaching();
builder.Services.AddAntiforgery(options => {
    options.Cookie.Name = "XSRF-TOKEN";
    options.HeaderName = "RequestVerificationToken";
});
// -----------------------------------------------

// 1.7. CẤU HÌNH OPENTELEMETRY (Metrics & Tracing)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("MyPortfolio.Web"))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddRuntimeInstrumentation();
        metrics.AddPrometheusExporter(); // Khôi phục dòng này
    });

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
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseResponseCaching();

// Cấu hình Swagger UI ở môi trường Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map Prometheus scraping endpoint
app.MapPrometheusScrapingEndpoint();

// Xác thực & Phân quyền phải nằm sau Routing
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers(); // Map các Controller của Web API
app.MapHub<MusicHub>("/musicHub");

// Tự động Migrate Database + Seed Profile User
// C-3: Seed logic tập trung ở đây — KHÔNG seed trong OnGetAsync của Profile page
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<MyPortfolio.Infrastructure.Data.ApplicationDbContext>();
        context.Database.Migrate();

        // Seed profile user nếu chưa tồn tại (chạy 1 lần duy nhất)
        if (!context.Users.Any())
        {
            var config = services.GetRequiredService<IConfiguration>();
            context.Users.Add(new MyPortfolio.Core.Entities.User
            {
                Name = config["Profile:Name"] ?? "VÕ HƯNG YÊN",
                Email = config["Profile:Email"] ?? string.Empty,
                Phone = config["Profile:Phone"] ?? string.Empty,
                Summary = config["Profile:Summary"] ?? "SE Student @ HUFLIT • .NET Full-Stack Developer",
                AvatarPath = config["Profile:AvatarPath"] ?? "/images/no-image.png",
            });
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded default profile user.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Lỗi khi khởi tạo Database.");
    }
}

app.Run();