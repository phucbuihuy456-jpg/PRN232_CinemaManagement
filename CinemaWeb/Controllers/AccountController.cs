using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CinemaWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace CinemaWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5027";
            _logger = logger;
        }

        // ==========================================
        // 1. ĐĂNG KÝ (REGISTER)
        // ==========================================
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var requestBody = new
            {
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                PasswordHash = model.Password // Backend API sẽ băm bằng BCrypt
            };

            var client = _httpClientFactory.CreateClient();
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync($"{_apiBaseUrl}/api/Auth/register", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    try
                    {
                        using var doc = JsonDocument.Parse(errorResponse);
                        if (doc.RootElement.TryGetProperty("message", out var msg))
                        {
                            ModelState.AddModelError(string.Empty, msg.GetString() ?? "Đăng ký thất bại.");
                        }
                        else if (doc.RootElement.TryGetProperty("detail", out var detail))
                        {
                            ModelState.AddModelError(string.Empty, detail.GetString() ?? "Đăng ký thất bại.");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Đăng ký thất bại. Email có thể đã tồn tại.");
                        }
                    }
                    catch
                    {
                        ModelState.AddModelError(string.Empty, "Đăng ký không thành công. Vui lòng thử lại!");
                    }

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API Đăng ký");
                ModelState.AddModelError(string.Empty, "Không thể kết nối đến máy chủ API. Vui lòng kiểm tra lại dịch vụ Backend.");
                return View(model);
            }
        }

        // ==========================================
        // 2. ĐĂNG NHẬP (LOGIN)
        // ==========================================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var requestBody = new
            {
                Email = model.Email,
                Password = model.Password
            };

            var client = _httpClientFactory.CreateClient();
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync($"{_apiBaseUrl}/api/Auth/login", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    var root = doc.RootElement;

                    string token = root.GetProperty("token").GetString() ?? "";
                    string userId = root.GetProperty("userId").GetInt32().ToString();
                    string fullName = root.GetProperty("fullName").GetString() ?? "";
                    string email = root.GetProperty("email").GetString() ?? "";
                    string role = root.GetProperty("role").GetString() ?? "Customer";

                    // 1. Tạo danh sách Claims định danh User
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId),
                        new Claim(ClaimTypes.Name, fullName),
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.Role, role),
                        new Claim("JwtToken", token)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
                    };

                    // 2. Ghi Cookie xác thực vào trình duyệt
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties
                    );

                    // 3. Chuyển hướng
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }

                    if (role == "Admin" || role == "Staff")
                    {
                        return RedirectToAction("Index", "Admin");
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        using var doc = JsonDocument.Parse(errorContent);
                        if (doc.RootElement.TryGetProperty("message", out var msg))
                        {
                            ModelState.AddModelError(string.Empty, msg.GetString() ?? "Đăng nhập thất bại.");
                        }
                        else if (doc.RootElement.TryGetProperty("detail", out var detail))
                        {
                            ModelState.AddModelError(string.Empty, detail.GetString() ?? "Đăng nhập thất bại.");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác!");
                        }
                    }
                    catch
                    {
                        ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác!");
                    }

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API Đăng nhập");
                ModelState.AddModelError(string.Empty, "Không thể kết nối đến máy chủ API.");
                return View(model);
            }
        }

        // ==========================================
        // 3. ĐĂNG XUẤT (LOGOUT)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // 4. TỪ CHỐI TRUY CẬP (ACCESS DENIED)
        // ==========================================
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==========================================
        // 5. QUÊN MẬT KHẨU - GỬI MÃ OTP (FORGOT PASSWORD)
        // ==========================================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(new { email = model.Email }),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync($"{_apiBaseUrl}/api/Auth/forgot-password", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Mã xác thực OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư!";
                    return RedirectToAction("ResetPassword", new { email = model.Email });
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    try
                    {
                        using var doc = JsonDocument.Parse(errorResponse);
                        if (doc.RootElement.TryGetProperty("message", out var msg))
                        {
                            ModelState.AddModelError(string.Empty, msg.GetString() ?? "Không thể gửi mã OTP.");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Email không tồn tại trong hệ thống!");
                        }
                    }
                    catch
                    {
                        ModelState.AddModelError(string.Empty, "Không thể gửi mã OTP. Vui lòng thử lại!");
                    }
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API Quên mật khẩu");
                ModelState.AddModelError(string.Empty, "Không thể kết nối đến máy chủ. Vui lòng thử lại sau!");
                return View(model);
            }
        }

        // ==========================================
        // 6. XÁC THỰC OTP & ĐẶT LẠI MẬT KHẨU (RESET PASSWORD)
        // ==========================================
        [HttpGet]
        public IActionResult ResetPassword(string? email)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            return View(new ResetPasswordOtpViewModel { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var requestBody = new
                {
                    email = model.Email,
                    otp = model.Otp.Trim(),
                    newPassword = model.NewPassword,
                    confirmPassword = model.ConfirmPassword
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync($"{_apiBaseUrl}/api/Auth/reset-password", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập ngay bây giờ.";
                    return RedirectToAction("Login");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    try
                    {
                        using var doc = JsonDocument.Parse(errorResponse);
                        if (doc.RootElement.TryGetProperty("message", out var msg))
                        {
                            ModelState.AddModelError(string.Empty, msg.GetString() ?? "Đặt lại mật khẩu thất bại.");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Mã OTP không đúng hoặc đã hết hạn.");
                        }
                    }
                    catch
                    {
                        ModelState.AddModelError(string.Empty, "Đặt lại mật khẩu không thành công. Vui lòng kiểm tra lại OTP.");
                    }
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API Đặt lại mật khẩu");
                ModelState.AddModelError(string.Empty, "Không thể kết nối đến máy chủ. Vui lòng thử lại sau!");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(new { email }),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync($"{_apiBaseUrl}/api/Auth/forgot-password", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Mã OTP mới đã được gửi vào email của bạn!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể gửi lại mã OTP. Vui lòng thử lại sau ít phút.";
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi kết nối máy chủ. Vui lòng thử lại!";
            }

            return RedirectToAction("ResetPassword", new { email });
        }
    }
}
