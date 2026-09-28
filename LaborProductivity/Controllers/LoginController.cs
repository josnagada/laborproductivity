using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using LaborProductivity.Data;
using LaborProductivity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaborProductivity.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoginController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Menampilkan halaman login
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Ambil setting aplikasi dari database.
            var setting = await _context.Settings
                .FirstOrDefaultAsync();

            // Kirim data setting ke halaman Login.
            ViewBag.Setting = setting;

            return View();
        }

        // Memproses data login yang dikirim dari form
        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            // Memeriksa apakah input form valid
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Mencari user berdasarkan email dan role
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == model.Email &&
                    u.Role == model.Role);

            // Jika user tidak ditemukan
            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Email atau password salah.");

                return View(model);
            }

            // Memeriksa apakah akun user masih aktif
            if (user.Status != "active")
            {
                ModelState.AddModelError(
                    "",
                    "Akun Anda tidak aktif. Silakan hubungi Admin.");

                return View(model);
            }

            // Membuat password hasher
            var passwordHasher = new PasswordHasher<User>();

            // Memeriksa password dengan hash yang tersimpan di database
            var hasilVerifikasi = passwordHasher.VerifyHashedPassword(
                user,
                user.Password,
                model.Password);

            // Jika password tidak cocok
            if (hasilVerifikasi == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "",
                    "Email atau password salah.");

                return View(model);
            }

            // Membuat informasi identitas user yang berhasil login
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new Claim(
                    ClaimTypes.Email,
                    user.Email),

                new Claim(
                    ClaimTypes.Role,
                    user.Role)
            };

            // Membuat identitas user berdasarkan claim
            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            // Membuat principal untuk user yang login
            var principal = new ClaimsPrincipal(identity);

            // Membuat cookie authentication
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    // Jika Remember Me dicentang,
                    // cookie akan dibuat persisten
                    IsPersistent = model.RememberMe
                });

            // Menyimpan pesan login berhasil untuk ditampilkan
            // setelah user diarahkan ke dashboard.
            TempData["LoginSuccess"] = $"Selamat datang, {user.Email}!";

            // Mengarahkan user ke dashboard sesuai role masing-masing
            switch (user.Role)
            {
                case "admin":
                    return RedirectToAction("Index", "Admin");

                case "ci":
                    return RedirectToAction("Index", "Ci");

                case "hr":
                    return RedirectToAction("Index", "Hr");

                case "production":
                    return RedirectToAction("Index", "Production");

                default:
                    return RedirectToAction("Index", "Home");
            }
        }

        // Mengeluarkan user dari sistem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Menghapus cookie autentikasi user
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            // Mengarahkan user kembali ke halaman login
            return RedirectToAction(
                "Index",
                "Login");
        }
    }
}