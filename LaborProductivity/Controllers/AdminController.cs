using LaborProductivity.Data;
using LaborProductivity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace LaborProductivity.Controllers
{
    // Hanya user dengan role admin yang boleh mengakses controller ini
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Menampilkan halaman utama Admin Panel
        public async Task<IActionResult> Index()
        {
            // Menghitung jumlah seluruh user yang ada di database
            var totalUsers = await _context.Users.CountAsync();

            // Mengirim jumlah user ke halaman Dashboard
            ViewBag.TotalUsers = totalUsers;

            return View();
        }

        // Menampilkan halaman Web Setting
        [HttpGet]
        public async Task<IActionResult> WebSetting()
        {
            // Karena tbl_settings hanya memiliki satu data setting,
            // kita mengambil data pertama dari tabel.
            var setting = await _context.Settings
                .FirstOrDefaultAsync();

            // Jika data setting belum ada,
            // kembalikan halaman Not Found.
            if (setting == null)
            {
                return NotFound();
            }

            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WebSetting(
    Setting model,
    IFormFile? logoFile,
    IFormFile? faviconFile,
    IFormFile? loginIconFile,
    IFormFile? carousel1File,
    IFormFile? carousel2File,
    IFormFile? carousel3File)

        {
            // Ambil data setting yang sudah tersimpan di database.
            var setting = await _context.Settings.FirstOrDefaultAsync();

            // Jika data setting belum ada, kembalikan ke halaman Web Setting.
            if (setting == null)
            {
                TempData["ErrorMessage"] = "Data Web Setting tidak ditemukan.";
                return RedirectToAction(nameof(WebSetting));
            }

            // Update data teks dari form.
            setting.AppName = model.AppName;
            setting.Color = model.Color;
            setting.FooterText = model.FooterText;

            // Folder penyimpanan file di dalam wwwroot.
            var uploadFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "settings");

            // Jika folder belum ada, buat folder tersebut.
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // =========================================================
            // UPLOAD LOGO
            // =========================================================

            if (logoFile != null && logoFile.Length > 0)
            {
                // Ambil ekstensi file logo.
                var extension = Path.GetExtension(logoFile.FileName)
                    .ToLowerInvariant();

                // Format logo yang diperbolehkan.
                var allowedExtensions = new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

                // Periksa ekstensi file.
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] =
                        "Format logo tidak diperbolehkan. Gunakan JPG, JPEG, PNG, atau WEBP.";

                    return RedirectToAction(nameof(WebSetting));
                }

                // Buat nama file baru agar tidak bentrok.
                var fileName = Guid.NewGuid().ToString() + extension;

                // Lokasi lengkap file baru.
                var filePath = Path.Combine(uploadFolder, fileName);

                // Simpan file logo.
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }

                // Hapus logo lama jika masih ada.
                if (!string.IsNullOrEmpty(setting.Logo))
                {
                    var oldLogoName = Path.GetFileName(setting.Logo);
                    var oldLogoPath = Path.Combine(uploadFolder, oldLogoName);

                    if (System.IO.File.Exists(oldLogoPath))
                    {
                        System.IO.File.Delete(oldLogoPath);
                    }
                }

                // Simpan nama file logo baru ke database.
                setting.Logo = fileName;
            }

            // =========================================================
            // UPLOAD FAVICON
            // =========================================================

            if (faviconFile != null && faviconFile.Length > 0)
            {
                // Ambil ekstensi file favicon.
                var extension = Path.GetExtension(faviconFile.FileName)
                    .ToLowerInvariant();

                // Format favicon yang diperbolehkan.
                var allowedExtensions = new[]
                {
        ".ico",
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

                // Periksa ekstensi file.
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] =
                        "Format favicon tidak diperbolehkan. Gunakan ICO, PNG, JPG, JPEG, atau WEBP.";

                    return RedirectToAction(nameof(WebSetting));
                }

                // =====================================================
                // Simpan favicon dengan nama tetap favicon.ico
                // =====================================================

                var faviconFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "upload");

                // Jika folder upload belum ada, buat folder tersebut.
                if (!Directory.Exists(faviconFolder))
                {
                    Directory.CreateDirectory(faviconFolder);
                }

                // Lokasi file favicon yang digunakan browser.
                var faviconPath = Path.Combine(
                    faviconFolder,
                    "favicon.ico");

                // Simpan file favicon baru.
                using (var stream = new FileStream(
                    faviconPath,
                    FileMode.Create))
                {
                    await faviconFile.CopyToAsync(stream);
                }

                // Simpan nama file favicon ke database.
                // Database tetap menyimpan nama file yang di-upload.
                setting.Favicon = faviconFile.FileName;
            }

            // =========================================================
            // UPLOAD LOGIN ICON
            // =========================================================

            if (loginIconFile != null && loginIconFile.Length > 0)
            {
                // Ambil ekstensi file login icon.
                var extension = Path.GetExtension(loginIconFile.FileName)
                    .ToLowerInvariant();

                // Format login icon yang diperbolehkan.
                var allowedExtensions = new[]
                {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

                // Periksa ekstensi file.
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] =
                        "Format login icon tidak diperbolehkan. Gunakan JPG, JPEG, PNG, atau WEBP.";

                    return RedirectToAction(nameof(WebSetting));
                }

                // Buat nama file baru agar tidak bentrok.
                var fileName = Guid.NewGuid().ToString() + extension;

                // Lokasi lengkap file baru.
                var filePath = Path.Combine(uploadFolder, fileName);

                // Simpan file login icon.
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await loginIconFile.CopyToAsync(stream);
                }

                // Hapus login icon lama jika masih ada.
                if (!string.IsNullOrEmpty(setting.LoginIcon))
                {
                    var oldLoginIconName = Path.GetFileName(setting.LoginIcon);
                    var oldLoginIconPath = Path.Combine(
                        uploadFolder,
                        oldLoginIconName);

                    if (System.IO.File.Exists(oldLoginIconPath))
                    {
                        System.IO.File.Delete(oldLoginIconPath);
                    }
                }

                // Simpan nama file login icon baru ke database.
                setting.LoginIcon = fileName;
            }

            // =========================================================
            // UPLOAD CAROUSEL 1
            // =========================================================

            if (carousel1File != null && carousel1File.Length > 0)
            {
                // Ambil ekstensi file.
                var extension = Path.GetExtension(carousel1File.FileName)
                    .ToLowerInvariant();

                // Format gambar yang diperbolehkan.
                var allowedExtensions = new[]
                {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

                // Periksa ekstensi file.
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] =
                        "Format Carousel 1 tidak diperbolehkan. Gunakan JPG, JPEG, PNG, atau WEBP.";

                    return RedirectToAction(nameof(WebSetting));
                }

                // Buat nama file baru agar tidak bentrok.
                var fileName = Guid.NewGuid().ToString() + extension;

                // Lokasi file.
                var filePath = Path.Combine(uploadFolder, fileName);

                // Simpan gambar Carousel 1.
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await carousel1File.CopyToAsync(stream);
                }

                // Hapus gambar Carousel 1 lama.
                if (!string.IsNullOrEmpty(setting.Carousel1))
                {
                    var oldCarouselName = Path.GetFileName(setting.Carousel1);
                    var oldCarouselPath = Path.Combine(
                        uploadFolder,
                        oldCarouselName);

                    if (System.IO.File.Exists(oldCarouselPath))
                    {
                        System.IO.File.Delete(oldCarouselPath);
                    }
                }

                // Simpan nama file baru ke database.
                setting.Carousel1 = fileName;
            }

            // =========================================================
            // UPLOAD CAROUSEL 2
            // =========================================================

            if (carousel2File != null && carousel2File.Length > 0)
            {
                // Ambil ekstensi file.
                var extension = Path.GetExtension(carousel2File.FileName)
                    .ToLowerInvariant();

                // Format gambar yang diperbolehkan.
                var allowedExtensions = new[]
                {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

                // Periksa ekstensi file.
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] =
                        "Format Carousel 2 tidak diperbolehkan. Gunakan JPG, JPEG, PNG, atau WEBP.";

                    return RedirectToAction(nameof(WebSetting));
                }

                // Buat nama file baru agar tidak bentrok.
                var fileName = Guid.NewGuid().ToString() + extension;

                // Lokasi file.
                var filePath = Path.Combine(uploadFolder, fileName);

                // Simpan gambar Carousel 2.
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await carousel2File.CopyToAsync(stream);
                }

                // Hapus gambar Carousel 2 lama.
                if (!string.IsNullOrEmpty(setting.Carousel2))
                {
                    var oldCarouselName = Path.GetFileName(setting.Carousel2);
                    var oldCarouselPath = Path.Combine(
                        uploadFolder,
                        oldCarouselName);

                    if (System.IO.File.Exists(oldCarouselPath))
                    {
                        System.IO.File.Delete(oldCarouselPath);
                    }
                }

                // Simpan nama file baru ke database.
                setting.Carousel2 = fileName;
            }

            // =========================================================
            // UPLOAD CAROUSEL 3
            // =========================================================

            if (carousel3File != null && carousel3File.Length > 0)
            {
                // Ambil ekstensi file.
                var extension = Path.GetExtension(carousel3File.FileName)
                    .ToLowerInvariant();

                // Format gambar yang diperbolehkan.
                var allowedExtensions = new[]
                {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

                // Periksa ekstensi file.
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] =
                        "Format Carousel 3 tidak diperbolehkan. Gunakan JPG, JPEG, PNG, atau WEBP.";

                    return RedirectToAction(nameof(WebSetting));
                }

                // Buat nama file baru agar tidak bentrok.
                var fileName = Guid.NewGuid().ToString() + extension;

                // Lokasi file.
                var filePath = Path.Combine(uploadFolder, fileName);

                // Simpan gambar Carousel 3.
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await carousel3File.CopyToAsync(stream);
                }

                // Hapus gambar Carousel 3 lama.
                if (!string.IsNullOrEmpty(setting.Carousel3))
                {
                    var oldCarouselName = Path.GetFileName(setting.Carousel3);
                    var oldCarouselPath = Path.Combine(
                        uploadFolder,
                        oldCarouselName);

                    if (System.IO.File.Exists(oldCarouselPath))
                    {
                        System.IO.File.Delete(oldCarouselPath);
                    }
                }

                // Simpan nama file baru ke database.
                setting.Carousel3 = fileName;
            }

            // Simpan seluruh perubahan ke database.
            await _context.SaveChangesAsync();

            // Pesan berhasil.
            TempData["SuccessMessage"] =
                "Web Setting berhasil diperbarui.";

            // Kembali ke halaman Web Setting.
            return RedirectToAction(nameof(WebSetting));
        }


        public async Task<IActionResult> Users()
        {
            // Mengambil seluruh user dari database.
            var users = await _context.Users
                .OrderBy(user => user.Id)
                .ToListAsync();

            // Mengambil seluruh department untuk dropdown
            // pada form Tambah User.
            var departments = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            // Mengambil seluruh Data Master untuk dropdown
            // CC / Product Group pada form Tambah User.
            var dataMasters = await _context.DataMasters
                .Include(dm => dm.Department)
                .OrderBy(dm => dm.CostCenter)
                .ThenBy(dm => dm.ProductGroup)
                .ThenBy(dm => dm.ProductLine)
                .ToListAsync();

            // Mengirim data dropdown ke halaman Users.
            ViewBag.Departments = departments;
            ViewBag.DataMasters = dataMasters;

            return View(users);
        }


        // Menampilkan form untuk menambah user baru.
        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            // Mengambil seluruh department dari database.
            var departments = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            // Mengambil seluruh Data Master.
            // Data ini akan digunakan untuk membentuk pilihan
            // Cost Center + Product Group.
            var dataMasters = await _context.DataMasters
                .Include(dm => dm.Department)
                .OrderBy(dm => dm.CostCenter)
                .ThenBy(dm => dm.ProductGroup)
                .ThenBy(dm => dm.ProductLine)
                .ToListAsync();

            // Membuat ViewModel untuk halaman Create User.
            var model = new CreateUserViewModel
            {
                Departments = departments,
                DataMasters = dataMasters
            };

            return View(model);
        }

        // Menampilkan form untuk mengedit user
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            // Mengambil data user yang akan diedit.
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Mengambil seluruh Department untuk dropdown.
            var departments = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            // Mengambil seluruh Data Master.
            // Data ini digunakan untuk pilihan
            // Cost Center + Product Group.
            var dataMasters = await _context.DataMasters
                .Include(dm => dm.Department)
                .OrderBy(dm => dm.CostCenter)
                .ThenBy(dm => dm.ProductGroup)
                .ThenBy(dm => dm.ProductLine)
                .ToListAsync();

            // Mengambil scope Production milik user.
            // Satu user dapat memiliki satu atau lebih scope.
            var productionScope = await _context.UserProductionScopes
                .FirstOrDefaultAsync(scope => scope.UserId == user.Id);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                Status = user.Status,

                Departments = departments,
                DataMasters = dataMasters
            };

            // Jika user mempunyai Production Scope,
            // masukkan datanya ke ViewModel.
            if (productionScope != null)
            {
                model.DepartmentId = productionScope.DepartmentId;
                model.CostCenter = productionScope.CostCenter;
                model.ProductGroup = productionScope.ProductGroup;
            }

            return View(model);
        }

        // Memproses perubahan data user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            // Jika role Production, maka Department,
            // Cost Center, dan Product Group wajib diisi.
            if (model.Role == "production")
            {
                if (!model.DepartmentId.HasValue)
                {
                    ModelState.AddModelError(
                        "DepartmentId",
                        "Department wajib dipilih untuk user Production.");
                }

                if (string.IsNullOrWhiteSpace(model.CostCenter))
                {
                    ModelState.AddModelError(
                        "CostCenter",
                        "CC / Product Group wajib dipilih untuk user Production.");
                }

                if (string.IsNullOrWhiteSpace(model.ProductGroup))
                {
                    ModelState.AddModelError(
                        "ProductGroup",
                        "Product Group wajib dipilih untuk user Production.");
                }
            }

            // Jika ada error validasi,
            // isi kembali dropdown sebelum menampilkan form.
            if (!ModelState.IsValid)
            {
                model.Departments = await _context.Departments
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                model.DataMasters = await _context.DataMasters
                    .Include(dm => dm.Department)
                    .OrderBy(dm => dm.CostCenter)
                    .ThenBy(dm => dm.ProductGroup)
                    .ThenBy(dm => dm.ProductLine)
                    .ToListAsync();

                return RedirectToAction(nameof(Users));
            }

            // Mengambil user yang akan diedit.
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == model.Id);

            if (user == null)
            {
                return NotFound();
            }

            // Cek apakah email digunakan oleh user lain.
            var emailSudahDigunakan = await _context.Users
                .AnyAsync(u =>
                    u.Email == model.Email &&
                    u.Id != model.Id);

            if (emailSudahDigunakan)
            {
                ModelState.AddModelError(
                    "Email",
                    "Email tersebut sudah digunakan oleh user lain.");

                model.Departments = await _context.Departments
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                model.DataMasters = await _context.DataMasters
                    .Include(dm => dm.Department)
                    .OrderBy(dm => dm.CostCenter)
                    .ThenBy(dm => dm.ProductGroup)
                    .ThenBy(dm => dm.ProductLine)
                    .ToListAsync();

                return View(model);
            }

            // Jika role Production,
            // pastikan kombinasi Department + CC + Product Group
            // memang tersedia di Data Master.
            if (model.Role == "production")
            {
                var scopeValid = await _context.DataMasters
                    .AnyAsync(dm =>
                        dm.DepartmentId == model.DepartmentId!.Value &&
                        dm.CostCenter == model.CostCenter &&
                        dm.ProductGroup == model.ProductGroup);

                if (!scopeValid)
                {
                    ModelState.AddModelError(
                        "",
                        "Kombinasi Department, CC, dan Product Group tidak ditemukan.");

                    model.Departments = await _context.Departments
                        .OrderBy(d => d.DepartmentName)
                        .ToListAsync();

                    model.DataMasters = await _context.DataMasters
                        .Include(dm => dm.Department)
                        .OrderBy(dm => dm.CostCenter)
                        .ThenBy(dm => dm.ProductGroup)
                        .ThenBy(dm => dm.ProductLine)
                        .ToListAsync();

                    return View(model);
                }
            }

            // Update informasi utama user.
            user.Email = model.Email;
            user.Role = model.Role;
            user.Status = model.Status;

            // Ambil Production Scope yang sudah dimiliki user.
            var productionScope = await _context.UserProductionScopes
                .FirstOrDefaultAsync(scope => scope.UserId == user.Id);

            if (model.Role == "production")
            {
                if (productionScope == null)
                {
                    // Jika sebelumnya bukan Production,
                    // buat scope baru.
                    productionScope = new UserProductionScope
                    {
                        UserId = user.Id,
                        DepartmentId = model.DepartmentId!.Value,
                        CostCenter = model.CostCenter!,
                        ProductGroup = model.ProductGroup!
                    };

                    _context.UserProductionScopes.Add(productionScope);
                }
                else
                {
                    // Jika scope sudah ada,
                    // update scope yang lama.
                    productionScope.DepartmentId =
                        model.DepartmentId!.Value;

                    productionScope.CostCenter =
                        model.CostCenter!;

                    productionScope.ProductGroup =
                        model.ProductGroup!;
                }
            }
            else
            {
                // Jika role diubah dari Production menjadi
                // HR atau CI, Production Scope tidak diperlukan lagi.
                if (productionScope != null)
                {
                    _context.UserProductionScopes.Remove(productionScope);
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "User berhasil diperbarui.";

            return RedirectToAction(nameof(Users));
        }

        // Menghapus user dari database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // Mengambil user yang akan dihapus.
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Akun Admin tidak boleh dihapus.
            if (user.Role == "admin")
            {
                return BadRequest("Akun Admin tidak dapat dihapus.");
            }

            // Mengambil seluruh Production Scope milik user.
            // Walaupun saat ini satu user hanya menggunakan satu scope,
            // kita tetap menggunakan ToListAsync() karena struktur tabel
            // memungkinkan satu user memiliki beberapa scope.
            var productionScopes = await _context.UserProductionScopes
                .Where(scope => scope.UserId == user.Id)
                .ToListAsync();

            // Hapus Production Scope terlebih dahulu
            // agar tidak melanggar Foreign Key ke tabel User.
            if (productionScopes.Any())
            {
                _context.UserProductionScopes.RemoveRange(productionScopes);
            }

            // Setelah scope dihapus, user dapat dihapus.
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "User berhasil dihapus.";

            return RedirectToAction(nameof(Users));
        }

        // Memproses form untuk menambah user baru
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(
            CreateUserViewModel model,
            bool fromModal = false)
        {
            // Jika role Production, maka Department, Cost Center,
            // dan Product Group wajib diisi.
            if (model.Role == "production")
            {
                if (!model.DepartmentId.HasValue)
                {
                    ModelState.AddModelError(
                        "DepartmentId",
                        "Department wajib dipilih untuk user Production.");
                }

                if (string.IsNullOrWhiteSpace(model.CostCenter))
                {
                    ModelState.AddModelError(
                        "CostCenter",
                        "CC / Product Group wajib dipilih untuk user Production.");
                }

                if (string.IsNullOrWhiteSpace(model.ProductGroup))
                {
                    ModelState.AddModelError(
                        "ProductGroup",
                        "Product Group wajib dipilih untuk user Production.");
                }
            }

            // Cek apakah email sudah digunakan.
            var emailSudahAda = await _context.Users
                .AnyAsync(user => user.Email == model.Email);

            if (emailSudahAda)
            {
                ModelState.AddModelError(
                    "Email",
                    "Email tersebut sudah digunakan.");
            }

            // Jika terdapat error validasi.
            if (!ModelState.IsValid)
            {
                // Request berasal dari modal -> balas JSON,
                // JANGAN return View() karena akan membuka
                // halaman CreateUser.cshtml yang lama.
                if (fromModal)
                {
                    var errors = ModelState
                        .Where(kvp => kvp.Value != null && kvp.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value!.Errors
                                .Select(e => e.ErrorMessage)
                                .ToArray()
                        );

                    return Json(new { success = false, errors });
                }

                // Request dari halaman full page (CreateUser.cshtml) -> tetap seperti semula.
                model.Departments = await _context.Departments
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                model.DataMasters = await _context.DataMasters
                    .Include(dm => dm.Department)
                    .OrderBy(dm => dm.CostCenter)
                    .ThenBy(dm => dm.ProductGroup)
                    .ThenBy(dm => dm.ProductLine)
                    .ToListAsync();

                return View(model);
            }

            // Jika Production, pastikan kombinasi
            // Department + Cost Center + Product Group
            // memang tersedia di Data Master.
            DataMaster? selectedDataMaster = null;

            if (model.Role == "production")
            {
                selectedDataMaster = await _context.DataMasters
                    .FirstOrDefaultAsync(dm =>
                        dm.DepartmentId == model.DepartmentId!.Value &&
                        dm.CostCenter == model.CostCenter &&
                        dm.ProductGroup == model.ProductGroup);

                if (selectedDataMaster == null)
                {
                    ModelState.AddModelError(
                        "",
                        "Kombinasi Department, CC, dan Product Group tidak ditemukan.");

                    if (fromModal)
                    {
                        var errors = ModelState
                            .Where(kvp => kvp.Value != null && kvp.Value.Errors.Count > 0)
                            .ToDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Value!.Errors
                                    .Select(e => e.ErrorMessage)
                                    .ToArray()
                            );

                        return Json(new { success = false, errors });
                    }

                    model.Departments = await _context.Departments
                        .OrderBy(d => d.DepartmentName)
                        .ToListAsync();

                    model.DataMasters = await _context.DataMasters
                        .Include(dm => dm.Department)
                        .OrderBy(dm => dm.CostCenter)
                        .ThenBy(dm => dm.ProductGroup)
                        .ThenBy(dm => dm.ProductLine)
                        .ToListAsync();

                    return View(model);
                }
            }

            // Membuat user baru.
            var userBaru = new User
            {
                Email = model.Email,
                Role = model.Role,
                Status = model.Status
            };

            // Hash password sebelum disimpan ke database.
            var passwordHasher = new PasswordHasher<User>();

            userBaru.Password = passwordHasher.HashPassword(
                userBaru,
                model.Password);

            _context.Users.Add(userBaru);

            // Simpan user terlebih dahulu agar userBaru.Id
            // mendapatkan ID dari database.
            await _context.SaveChangesAsync();

            // Jika user adalah Production,
            // simpan batas akses Production ke tabel scope.
            if (model.Role == "production")
            {
                var productionScope = new UserProductionScope
                {
                    UserId = userBaru.Id,
                    DepartmentId = model.DepartmentId!.Value,
                    CostCenter = model.CostCenter!,
                    ProductGroup = model.ProductGroup!
                };

                _context.UserProductionScopes.Add(productionScope);

                await _context.SaveChangesAsync();
            }

            // Pesan berhasil disimpan.
            TempData["SuccessMessage"] = "User berhasil ditambahkan.";

            // Jika request berasal dari modal,
            // kembalikan JSON agar JavaScript dapat melakukan redirect.
            if (fromModal)
            {
                return Json(new
                {
                    success = true
                });
            }

            // Jika request berasal dari halaman CreateUser biasa,
            // tetap gunakan redirect seperti sebelumnya.
            return RedirectToAction(nameof(Users));
        }
    }
}