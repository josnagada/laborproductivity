using LaborProductivity.Data;
using Microsoft.AspNetCore.Authorization;
using LaborProductivity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace LaborProductivity.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Mengambil pengaturan aplikasi dari database
            var setting = await _context.Settings.FirstOrDefaultAsync();

            // Landing page dapat diakses tanpa login
            return View(setting);
        }

        // Menguji apakah aplikasi dapat terhubung ke database
        public async Task<IActionResult> TestDatabase()
        {
            var berhasil = await _context.Database.CanConnectAsync();

            if (berhasil)
            {
                return Content("Koneksi ke database labtivity_db BERHASIL!");
            }

            return Content("Koneksi ke database labtivity_db GAGAL.");
        }

        // Menguji apakah EF Core dapat membaca data dari tabel User
        public async Task<IActionResult> TestUser()
        {
            var jumlahUser = await _context.Users.CountAsync();

            return Content($"Jumlah user di database: {jumlahUser}");
        }

        // Membuat user Admin pertama ke database
        public async Task<IActionResult> CreateFirstAdmin()
        {
            // Memeriksa apakah sudah ada user dengan role Admin
            var adminSudahAda = await _context.Users
                .AnyAsync(user => user.Role == "admin");

            if (adminSudahAda)
            {
                return Content("Admin sudah ada di database.");
            }

            // Membuat objek User baru
            var admin = new User
            {
                Email = "admin@gmail.com",
                Role = "admin",
                Status = "active"
            };

            // Membuat password hash menggunakan PasswordHasher
            var passwordHasher = new PasswordHasher<User>();

            admin.Password = passwordHasher.HashPassword(
                admin,
                "Admin123!"
            );

            // Menambahkan Admin ke database
            _context.Users.Add(admin);

            await _context.SaveChangesAsync();

            return Content("Admin pertama berhasil dibuat.");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }

}
