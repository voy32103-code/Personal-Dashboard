using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MyPortfolio.Web.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginModel> _logger; 

        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        // Validate Format & Required
        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
        public string Email { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        public string Password { get; set; } = "";

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ErrorMessage { get; set; } = "";
        public string ReturnUrl { get; set; }

   
        //: Ngăn chặn Open Redirect (Phishing)
        // Chỉ cho phép chuyển hướng trong nội bộ website
 
        private string ValidateReturnUrl(string returnUrl)
        {
            return (Url.IsLocalUrl(returnUrl)) ? returnUrl : Url.Content("~/");
        }

        // --- 1. KHI VÀO TRANG (GET) ---
        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = ValidateReturnUrl(returnUrl);

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            //  Đã xóa đoạn Query Database tạo Admin ở đây.
            // Việc khởi tạo Admin (Seeding) nên được đưa vào Program.cs chạy 1 lần lúc startup.
        }

        // --- 2. ĐĂNG NHẬP BẰNG MẬT KHẨU (POST) ---
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = ValidateReturnUrl(returnUrl);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
            {
                return Page(); // Trả về lỗi Validation (BUG 2)
            }

            try
            {
                // Trim và Lowercase
                var normalizedEmail = Email.Trim().ToLower();

                // lockoutOnFailure = true (Khóa account nếu nhập sai 5 lần)
                var result = await _signInManager.PasswordSignInAsync(
                    normalizedEmail,
                    Password,
                    isPersistent: true,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} logged in successfully.", normalizedEmail);
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = true });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Account locked out for {Email}", normalizedEmail);
                    ErrorMessage = "Tài khoản đã bị khóa do nhập sai nhiều lần. Vui lòng thử lại sau 5 phút.";
                    return Page();
                }

                ErrorMessage = "Sai email hoặc mật khẩu.";
                return Page();
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ, tránh app crash
                _logger.LogError(ex, "Lỗi hệ thống khi user {Email} đăng nhập.", Email);
                ErrorMessage = "Hệ thống đang bận. Vui lòng thử lại sau.";
                return Page();
            }
        }

        // --- 3. BẮT ĐẦU GỌI SANG GOOGLE (POST) ---
        public IActionResult OnPostExternalLogin(string provider, string returnUrl = null)
        {
            returnUrl = ValidateReturnUrl(returnUrl);
            var redirectUrl = Url.Page("./Login", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        // --- 4. NHẬN KẾT QUẢ TỪ GOOGLE TRẢ VỀ (CALLBACK) ---
        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl = ValidateReturnUrl(returnUrl);

            if (remoteError != null)
            {
                ErrorMessage = $"Lỗi từ Google: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            try
            {
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    ErrorMessage = "Không lấy được thông tin từ Google.";
                    return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                }

                //  bypassTwoFactor = false để tôn trọng cài đặt 2FA
                var result = await _signInManager.ExternalLoginSignInAsync(
                    info.LoginProvider,
                    info.ProviderKey,
                    isPersistent: false,
                    bypassTwoFactor: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in with {Provider}.", info.LoginProvider);
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl });
                }
                if (result.IsLockedOut)
                {
                    ErrorMessage = "Tài khoản đã bị khóa.";
                    return Page();
                }

                // NẾU CHƯA CÓ TÀI KHOẢN -> TẠO MỚI / LIÊN KẾT

                // Xử lý trường hợp Google không trả về Email
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrWhiteSpace(email))
                {
                    // Tạo Pseudo-email: username_providerid@provider-auth.internal
                    email = $"{info.ProviderKey}@{info.LoginProvider.ToLower()}-auth.internal";
                    _logger.LogWarning("Google did not provide email. Using pseudo-email: {Email}", email);
                }

                // Double check (Race Condition) - Tránh tạo trùng lặp
                var existingUser = await _userManager.FindByEmailAsync(email);

                if (existingUser != null)
                {
                    // User đã tồn tại, chỉ cần liên kết Google vào tài khoản này
                    var linkResult = await _userManager.AddLoginAsync(existingUser, info);
                    if (linkResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(existingUser, isPersistent: false, info.LoginProvider);
                        return LocalRedirect(returnUrl);
                    }
                }
                else
                {
                    // Tạo hoàn toàn mới
                    var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                    var resultCreate = await _userManager.CreateAsync(user);

                    if (resultCreate.Succeeded)
                    {
                        resultCreate = await _userManager.AddLoginAsync(user, info);
                        if (resultCreate.Succeeded)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                            _logger.LogInformation("Created new account from {Provider}.", info.LoginProvider);
                            return LocalRedirect(returnUrl);
                        }
                    }

                    var errors = string.Join(", ", resultCreate.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create user from external login: {Errors}", errors);
                    ErrorMessage = $"Lỗi tạo User: {errors}";
                }

                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            catch (Exception ex)
            {
                //  Bắt mọi lỗi từ network/API
                _logger.LogError(ex, "Lỗi hệ thống khi xử lý Callback từ Google.");
                ErrorMessage = "Đã xảy ra lỗi khi kết nối với máy chủ đăng nhập.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
        }

        // --- 5. ĐĂNG XUẤT ---
        public async Task<IActionResult> OnGetLogoutAsync()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToPage("/Index");
        }
    }
}