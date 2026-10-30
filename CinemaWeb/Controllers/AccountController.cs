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
    }
}
