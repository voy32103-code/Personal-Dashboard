using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MyPortfolio.Web.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        // Danh sách các nút đăng nhập ngoài (Google, Facebook...)
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ErrorMessage { get; set; } = "";
        public string ReturnUrl { get; set; }

        // --- 1. KHI VÀO TRANG (GET) ---
        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            // Xóa cookie cũ để login sạch sẽ
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Lấy danh sách provider (Google) để hiển thị nút bấm bên View
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // Tự động tạo Admin nếu chưa có (Code cũ của bạn)
            var adminEmail = "admin@myportfolio.com";
            if (await _userManager.FindByEmailAsync(adminEmail) == null)
            {
                var newAdmin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                await _userManager.CreateAsync(newAdmin, "Admin123");
            }
        }

        // --- 2. ĐĂNG NHẬP BẰNG MẬT KHẨU (POST) ---
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            // Load lại danh sách nút Google nếu login thất bại để không bị mất nút
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            var result = await _signInManager.PasswordSignInAsync(Email, Password, isPersistent: true, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }

            ErrorMessage = "Sai email hoặc mật khẩu rồi đại ca ơi!";
            return Page();
        }

        // --- 3. BẮT ĐẦU GỌI SANG GOOGLE (POST) ---
        public IActionResult OnPostExternalLogin(string provider, string returnUrl = null)
        {
            // Yêu cầu chuyển hướng về hàm OnGetCallbackAsync sau khi Google xử lý xong
            var redirectUrl = Url.Page("./Login", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        // --- 4. NHẬN KẾT QUẢ TỪ GOOGLE TRẢ VỀ (CALLBACK) ---
        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            // Nếu Google báo lỗi
            if (remoteError != null)
            {
                ErrorMessage = $"Lỗi từ Google: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Lấy thông tin User từ Google
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Không lấy được thông tin từ Google.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Thử đăng nhập (nếu User này đã từng đăng nhập trước đó)
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }

            // Nếu chưa có tài khoản -> TỰ ĐỘNG TẠO MỚI (Auto Provisioning)
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                // Lấy Email từ Google
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (email != null)
                {
                    var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };

                    // Tạo User mới trong Database (Không cần mật khẩu vì dùng Google)
                    var resultCreate = await _userManager.CreateAsync(user);

                    if (resultCreate.Succeeded)
                    {
                        // Liên kết User mới này với Google Login
                        resultCreate = await _userManager.AddLoginAsync(user, info);
                        if (resultCreate.Succeeded)
                        {
                            // Đăng nhập luôn
                            await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                            return LocalRedirect(returnUrl);
                        }
                    }
                    else
                    {
                        // Nếu lỗi (ví dụ: mật khẩu không đủ mạnh - dù ở đây ko set pass), in lỗi ra
                        var errors = string.Join(", ", resultCreate.Errors.Select(e => e.Description));
                        ErrorMessage = $"Lỗi tạo User: {errors}";
                        return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                    }
                }

                ErrorMessage = "Không tìm thấy Email từ Google.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
        }

        // --- 5. ĐĂNG XUẤT ---
        public async Task<IActionResult> OnGetLogoutAsync()
        {
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Index");
        }
    }
}