using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaWeb.Controllers
{
    /// <summary>
    /// Controller điều hướng chính cho phân hệ Quản trị (Admin Console).
    /// Áp dụng bảo vệ [Authorize(Roles = "Admin,Staff")] theo yêu cầu Task 13.
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    public class AdminController : Controller
    {
        // 1. Dashboard (Bảng điều khiển chung)
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // 2. Quản lý Phim (Dành cho Admin)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Movies()
        {
            return View();
        }

        // 3. Quản lý Suất chiếu (Admin & Staff)
        [HttpGet]
        public IActionResult Showtimes()
        {
            return View();
        }

        // 4. Quản lý Combo bắp nước (Admin)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Combos()
        {
            return View();
        }

        // 5. Quản lý Thể loại phim (Admin)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Genres()
        {
            return View();
        }

        // 6. Quản lý Người dùng & Phân quyền (Admin duy nhất)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Users()
        {
            return View();
        }
    }
}
