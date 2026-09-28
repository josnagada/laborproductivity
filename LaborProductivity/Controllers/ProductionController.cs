using LaborProductivity.Data;
using LaborProductivity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaborProductivity.Controllers
{
    // Hanya user dengan role production yang boleh mengakses controller ini
    [Authorize(Roles = "production")]
    public class ProductionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Mengambil Data Master yang boleh diakses oleh
        // user Production yang sedang login.
        //
        // Scope user ditentukan berdasarkan:
        // Department + Cost Center + Product Group.
        //
        // Product Line tidak menjadi bagian dari scope,
        // sehingga semua Product Line dalam scope tersebut
        // dapat digunakan oleh user.
        private async Task<List<int>> GetAllowedDataMasterIdsAsync()
        {
            // =====================================================
            // 1. Ambil ID user yang sedang login
            // =====================================================

            var userIdClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim == null ||
                !int.TryParse(userIdClaim.Value, out int userId))
            {
                return new List<int>();
            }

            // =====================================================
            // 2. Ambil Production Scope milik user
            // =====================================================

            var scopes = await _context.UserProductionScopes
                .Where(scope => scope.UserId == userId)
                .ToListAsync();

            if (!scopes.Any())
            {
                return new List<int>();
            }

            // =====================================================
            // 3. Ambil Data Master
            // =====================================================

            var dataMasters = await _context.DataMasters
                .ToListAsync();

            // =====================================================
            // 4. Filter Data Master berdasarkan scope
            //
            // Dilakukan setelah data diambil ke memory agar
            // tidak menggunakan ekspresi LINQ yang gagal
            // diterjemahkan oleh EF Core.
            // =====================================================

            var allowedDataMasterIds = dataMasters
                .Where(dm => scopes.Any(scope =>
                    scope.DepartmentId == dm.DepartmentId &&
                    scope.CostCenter == dm.CostCenter &&
                    scope.ProductGroup == dm.ProductGroup))
                .Select(dm => dm.DataMasterId)
                .ToList();

            return allowedDataMasterIds;
        }

        // Menampilkan halaman utama Dashboard Production
        public async Task<IActionResult> Index()
        {
            // Ambil ID user yang sedang login dari Claim
            var userIdClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier);

            // Jika ID user tidak ditemukan, kembalikan halaman
            // tanpa informasi scope Production.
            if (userIdClaim == null ||
                !int.TryParse(userIdClaim.Value, out int userId))
            {
                return View();
            }

            // Ambil scope Production milik user yang sedang login.
            var scopes = await _context.UserProductionScopes
                .Where(scope => scope.UserId == userId)
                .ToListAsync();

            // Jika user belum memiliki scope Production,
            // tampilkan dashboard tanpa data scope.
            if (!scopes.Any())
            {
                return View();
            }

            // Ambil Data Master yang sesuai dengan scope user.
            //
            // Scope ditentukan berdasarkan:
            // Department + Cost Center + Product Group.
            //
            // Product Line tidak menjadi bagian dari scope,
            // sehingga semua Product Line dalam scope akan diambil.
            var dataMasters = await _context.DataMasters
                .Include(d => d.Department)
                .ToListAsync();

            var allowedDataMasters = dataMasters
                .Where(dm => scopes.Any(scope =>
                    scope.DepartmentId == dm.DepartmentId &&
                    scope.CostCenter == dm.CostCenter &&
                    scope.ProductGroup == dm.ProductGroup))
                .OrderBy(dm => dm.ProductLine)
                .ToList();

            // Jika tidak ada Data Master yang sesuai dengan scope user.
            if (!allowedDataMasters.Any())
            {
                return View();
            }

            // Ambil Data Master pertama sebagai identitas utama
            // untuk Cost Center, Department, dan Product Group.
            var firstDataMaster = allowedDataMasters.First();

            // Data yang akan dikirim ke View.
            ViewBag.CostCenter = firstDataMaster.CostCenter;

            ViewBag.DepartmentName =
                firstDataMaster.Department?.DepartmentName
                ?? string.Empty;

            ViewBag.ProductGroup =
                firstDataMaster.ProductGroup;

            // Ambil seluruh Product Line yang tersedia
            // dalam scope user.
            ViewBag.ProductLines = allowedDataMasters
                .Select(d => d.ProductLine)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            return View();
        }

        // Menampilkan data Production Input
        public async Task<IActionResult> ProductionInput()
        {
            // Mengambil Data Master yang boleh diakses
            // oleh user Production yang sedang login.
            var allowedDataMasterIds =
                await GetAllowedDataMasterIdsAsync();

            // Hanya tampilkan Production Output
            // yang Data Master-nya termasuk dalam scope user.
            var productionInputs = await _context.ProductionOutputs
                .Where(p => allowedDataMasterIds.Contains(p.DataMasterId))
                .Include(p => p.DataMaster)
                .ThenInclude(d => d!.Department)
                .OrderByDescending(p => p.FinishDate)
                .ThenBy(p => p.DataMaster!.CostCenter)
                .ToListAsync();

            return View(productionInputs);
        }

        // Menampilkan data Production Output
        public async Task<IActionResult> ProductionOutput()
        {
            // Mengambil Data Master yang boleh diakses
            // oleh user Production yang sedang login.
            var allowedDataMasterIds =
                await GetAllowedDataMasterIdsAsync();

            // Hanya tampilkan Production Output
            // yang Data Master-nya termasuk dalam scope user.
            var productionOutputs = await _context.ProductionOutputs
                .Where(p => allowedDataMasterIds.Contains(p.DataMasterId))
                .Include(p => p.DataMaster)
                .ThenInclude(d => d!.Department)
                .OrderByDescending(p => p.FinishDate)
                .ThenBy(p => p.DataMaster!.CostCenter)
                .ToListAsync();

            return View(productionOutputs);
        }

        // Menampilkan form untuk menambahkan Production Output
        [HttpGet]
        public async Task<IActionResult> CreateProductionOutput()
        {
            // Ambil Data Master yang boleh diakses oleh user Production.
            // Scope ditentukan dari Department + Cost Center + Product Group.
            // Product Line tidak menjadi bagian dari scope.
            var allowedDataMasterIds =
                await GetAllowedDataMasterIdsAsync();

            // Ambil Fiscal Year yang tersedia.
            var fiscalYears = await _context.FiscalCalendars
                .Select(f => f.FiscalYear)
                .Distinct()
                .OrderByDescending(f => f)
                .ToListAsync();

            ViewBag.FiscalYears = fiscalYears;

            return View();
        }

        // Menyimpan data Production Output ke database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProductionOutput(
    int DataMasterId,
    int FiscalYear,
    int WeekNo,
    decimal QualityInput,
    decimal QualityOutput)
        {
            // Ambil Data Master yang boleh diakses oleh user Production.
            var allowedDataMasterIds =
                await GetAllowedDataMasterIdsAsync();

            // Pastikan Data Master yang dikirim benar-benar termasuk
            // dalam scope user yang sedang login.
            if (!allowedDataMasterIds.Contains(DataMasterId))
            {
                ModelState.AddModelError(
                    "",
                    "Anda tidak memiliki akses untuk menggunakan Cost Center / Product Group / Product Line tersebut."
                );
            }

            // Ambil Data Master berdasarkan ID.
            var dataMaster = await _context.DataMasters
                .FirstOrDefaultAsync(d =>
                    d.DataMasterId == DataMasterId);

            if (dataMaster == null)
            {
                ModelState.AddModelError(
                    "",
                    "Data Cost Center / Product Group / Product Line tidak ditemukan."
                );
            }

            // Ambil Fiscal Calendar berdasarkan Fiscal Year dan Fiscal Week.
            var fiscalCalendar = await _context.FiscalCalendars
                .FirstOrDefaultAsync(f =>
                    f.FiscalYear == FiscalYear &&
                    f.WeekNo == WeekNo);

            if (fiscalCalendar == null)
            {
                ModelState.AddModelError(
                    "",
                    "Fiscal Year dan Fiscal Week tidak valid."
                );
            }

            // Validasi Quality Input.
            if (QualityInput < 0)
            {
                ModelState.AddModelError(
                    nameof(QualityInput),
                    "Quality Input tidak boleh kurang dari 0."
                );
            }

            // Validasi Quality Output.
            if (QualityOutput < 0)
            {
                ModelState.AddModelError(
                    nameof(QualityOutput),
                    "Quality Output tidak boleh kurang dari 0."
                );
            }

            // Jika ada validasi yang gagal, tampilkan kembali form.
            if (!ModelState.IsValid)
            {
                var fiscalYears = await _context.FiscalCalendars
                    .Select(f => f.FiscalYear)
                    .Distinct()
                    .OrderByDescending(f => f)
                    .ToListAsync();

                ViewBag.FiscalYears = fiscalYears;

                return View();
            }

            // Work Date menggunakan Start Date dari Fiscal Calendar.
            var finishDate = fiscalCalendar!.StartDate;

            // Cek apakah data untuk Data Master dan Fiscal Week tersebut
            // sudah pernah dimasukkan.
            var existingData = await _context.ProductionOutputs
                .AnyAsync(p =>
                    p.DataMasterId == DataMasterId &&
                    p.WeekNo == WeekNo &&
                    p.FinishDate == finishDate);

            if (existingData)
            {
                ModelState.AddModelError(
                    "",
                    "Data Production untuk Product Line dan Fiscal Week tersebut sudah ada."
                );

                var fiscalYears = await _context.FiscalCalendars
                    .Select(f => f.FiscalYear)
                    .Distinct()
                    .OrderByDescending(f => f)
                    .ToListAsync();

                ViewBag.FiscalYears = fiscalYears;

                return View();
            }

            // Buat data Production Output baru.
            var productionOutput = new ProductionOutput
            {
                FinishDate = finishDate,
                WeekNo = WeekNo,
                DataMasterId = DataMasterId,
                QualityInput = QualityInput,
                QualityOutput = QualityOutput
            };

            _context.ProductionOutputs.Add(productionOutput);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Data Production berhasil disimpan.";

            return RedirectToAction(nameof(ProductionOutput));
        }

        // Menampilkan form Edit Production Output.
        [HttpGet]
        public async Task<IActionResult> EditProductionOutput(int id)
        {
            // Ambil semua Data Master yang boleh diakses
            // oleh user Production yang sedang login.
            var allowedDataMasterIds =
                await GetAllowedDataMasterIdsAsync();

            // Ambil data Production Output berdasarkan ID.
            var productionOutput = await _context.ProductionOutputs
                .Include(p => p.DataMaster)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(p =>
                    p.ProductionOutputId == id);

            if (productionOutput == null)
            {
                return NotFound();
            }

            // Pastikan data tersebut termasuk dalam scope
            // user Production yang sedang login.
            if (!allowedDataMasterIds.Contains(
                    productionOutput.DataMasterId))
            {
                return Forbid();
            }

            return View(productionOutput);
        }

        // Menghapus data Production Output.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProductionOutput(int id)
        {
            // =====================================================
            // 1. Ambil Data Master yang boleh diakses user
            // =====================================================

            var allowedDataMasterIds =
                await GetAllowedDataMasterIdsAsync();

            // =====================================================
            // 2. Cari data Production Output berdasarkan ID
            // =====================================================

            var productionOutput = await _context.ProductionOutputs
                .FirstOrDefaultAsync(p =>
                    p.ProductionOutputId == id);

            if (productionOutput == null)
            {
                return NotFound();
            }

            // =====================================================
            // 3. Pastikan data tersebut termasuk dalam scope user
            // =====================================================

            if (!allowedDataMasterIds.Contains(
                    productionOutput.DataMasterId))
            {
                return Forbid();
            }

            // =====================================================
            // 4. Hapus data
            // =====================================================

            _context.ProductionOutputs.Remove(productionOutput);

            await _context.SaveChangesAsync();

            // =====================================================
            // 5. Tampilkan pesan berhasil
            // =====================================================

            TempData["SuccessMessage"] =
                "Data Production Output berhasil dihapus.";

            return RedirectToAction(
                nameof(ProductionOutput));
        }

        // Menampilkan Production Output Report.
        //
        // Setiap Product Line ditampilkan sebagai baris terpisah
        // (bukan digabung jadi satu baris CC), karena satu akun
        // Production bisa memiliki lebih dari satu Product Line
        // dalam satu Product Group yang sama.
        [HttpGet]
        public async Task<IActionResult> ProductionReport(
      int? year,
      int? fiscalWeek)
        {
            // Mengambil Data Master yang boleh diakses
            // berdasarkan scope user Production.
            var allowedDataMasterIds =
                await GetAllowedDataMasterIdsAsync();

            var dataMasters = await _context.DataMasters
                .Include(d => d.Department)
                .Where(d =>
                    allowedDataMasterIds.Contains(d.DataMasterId))
                .OrderBy(d => d.CostCenter)
                .ThenBy(d => d.ProductGroup)
                .ThenBy(d => d.ProductLine)
                .ToListAsync();

            if (!dataMasters.Any())
            {
                return View(new ProductionReportViewModel());
            }

            // Mengambil seluruh Fiscal Year dari Fiscal Calendar.
            var fiscalYears = await _context.FiscalCalendars
                .Select(f => f.FiscalYear)
                .Distinct()
                .OrderByDescending(f => f)
                .ToListAsync();

            if (!fiscalYears.Any())
            {
                fiscalYears.Add(DateTime.Now.Year);
            }

            // Menentukan Fiscal Year yang dipilih.
            var selectedYear = year ?? fiscalYears.First();

            if (!fiscalYears.Contains(selectedYear))
            {
                selectedYear = fiscalYears.First();
            }

            // Mengambil Fiscal Week berdasarkan Fiscal Year yang dipilih.
            var fiscalWeeks = await _context.FiscalCalendars
                .Where(f =>
                    f.FiscalYear == selectedYear)
                .OrderBy(f => f.WeekNo)
                .Select(f => new FiscalWeekReportItem
                {
                    WeekNo = f.WeekNo,
                    StartDate = f.StartDate,
                    EndDate = f.EndDate
                })
                .ToListAsync();

            // Memastikan Fiscal Week yang dipilih memang
            // tersedia pada Fiscal Year tersebut.
            int? selectedWeek = fiscalWeek;

            if (selectedWeek.HasValue &&
                !fiscalWeeks.Any(f =>
                    f.WeekNo == selectedWeek.Value))
            {
                selectedWeek = null;
            }

            /*
             * ==========================================================
             * MENENTUKAN DATA PRODUCTION BERDASARKAN FISCAL CALENDAR
             * ==========================================================
             *
             * Jangan menggunakan:
             *
             *     p.FinishDate.Year == selectedYear
             *
             * karena Fiscal Year tidak selalu sama dengan tahun kalender.
             *
             * Contoh:
             *
             * Fiscal Year 2024
             * Week 1 = 29 Dec 2023 - 04 Jan 2024
             *
             * Jadi tanggal 29 Dec 2023 tetap termasuk Fiscal Year 2024.
             *
             * Kita juga join ke DataMasters di sini supaya tahu
             * ProductLine dari setiap Production Output — inilah
             * kunci pembeda baris pada report yang baru.
             */

            var productionQuery =
                from production in _context.ProductionOutputs

                join calendar in _context.FiscalCalendars
                    on production.WeekNo equals calendar.WeekNo

                join dataMaster in _context.DataMasters
                    on production.DataMasterId equals dataMaster.DataMasterId

                where
                    allowedDataMasterIds.Contains(
                        production.DataMasterId)

                    && calendar.FiscalYear == selectedYear

                    && production.FinishDate >= calendar.StartDate

                    && production.FinishDate <= calendar.EndDate

                select new
                {
                    production,
                    calendar,
                    productLine = dataMaster.ProductLine
                };

            // Jika user memilih Fiscal Week tertentu,
            // batasi data hanya pada minggu tersebut.
            if (selectedWeek.HasValue)
            {
                productionQuery = productionQuery
                    .Where(x =>
                        x.calendar.WeekNo == selectedWeek.Value);
            }

            var productionOutputs =
                await productionQuery.ToListAsync();

            // =====================================================
            // Tentukan kolom minggu (header) yang benar-benar
            // memiliki data. Kolom ini dipakai bersama oleh
            // seluruh Product Line di bawah, supaya setiap baris
            // punya jumlah kolom yang sama dan tetap sejajar.
            // =====================================================

            var reportWeeks = productionOutputs
                .Select(x => new FiscalWeekReportItem
                {
                    WeekNo = x.calendar.WeekNo,
                    StartDate = x.calendar.StartDate,
                    EndDate = x.calendar.EndDate
                })
                .GroupBy(w => w.WeekNo)
                .Select(g => g.First())
                .OrderBy(w => w.WeekNo)
                .ToList();

            // =====================================================
            // Group data production per Product Line, lalu per
            // minggu di dalamnya.
            // =====================================================

            var productLines = productionOutputs
                .GroupBy(x => x.productLine)
                .Select(productLineGroup =>
                {
                    // Jumlahkan Quality Input/Output per minggu
                    // untuk Product Line ini.
                    var weekValues = productLineGroup
                        .GroupBy(x => x.calendar.WeekNo)
                        .ToDictionary(
                            g => g.Key,
                            g => new ProductionReportWeek
                            {
                                WeekNo = g.Key,
                                StartDate = g.First().calendar.StartDate,
                                EndDate = g.First().calendar.EndDate,

                                QualityInput = g.Sum(x =>
                                    x.production.QualityInput),

                                QualityOutput = g.Sum(x =>
                                    x.production.QualityOutput)
                            });

                    // Susun ulang mengikuti urutan reportWeeks.
                    // Jika Product Line ini tidak punya data pada
                    // minggu tertentu, isi dengan nilai 0 supaya
                    // kolomnya tetap ada (tidak hilang/geser).
                    var weeks = reportWeeks
                        .Select(reportWeek =>
                            weekValues.TryGetValue(
                                reportWeek.WeekNo,
                                out var weekValue)
                                ? weekValue
                                : new ProductionReportWeek
                                {
                                    WeekNo = reportWeek.WeekNo,
                                    StartDate = reportWeek.StartDate,
                                    EndDate = reportWeek.EndDate,
                                    QualityInput = 0,
                                    QualityOutput = 0
                                })
                        .ToList();

                    return new ProductLineReport
                    {
                        ProductLine = productLineGroup.Key,
                        Weeks = weeks,

                        TotalQualityInput =
                            weeks.Sum(w => w.QualityInput),

                        TotalQualityOutput =
                            weeks.Sum(w => w.QualityOutput)
                    };
                })
                .OrderBy(pl => pl.ProductLine)
                .ToList();

            // Mengambil Data Master pertama untuk informasi
            // Cost Center, Department, dan Product Group.
            var firstDataMaster = dataMasters.First();

            var viewModel = new ProductionReportViewModel
            {
                CostCenter =
                    firstDataMaster.CostCenter,

                DepartmentName =
                    firstDataMaster.Department?.DepartmentName
                    ?? string.Empty,

                ProductGroup =
                    firstDataMaster.ProductGroup,

                SelectedYear =
                    selectedYear,

                SelectedWeek =
                    selectedWeek,

                FiscalYears =
                    fiscalYears,

                FiscalWeeks =
                    fiscalWeeks,

                ReportWeeks =
                    reportWeeks,

                ProductLines =
                    productLines,

                TotalQualityInput =
                    productLines.Sum(pl => pl.TotalQualityInput),

                TotalQualityOutput =
                    productLines.Sum(pl => pl.TotalQualityOutput)
            };

            return View(viewModel);
        }

        // Menyimpan perubahan Production Output.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProductionOutput(
            int id,
            decimal QualityInput,
            decimal QualityOutput)
        {
            // Ambil semua Data Master yang boleh diakses
            // oleh user Production yang sedang login.
            var allowedDataMasterIds =
                await GetAllowedDataMasterIdsAsync();

            // Ambil data yang akan diedit.
            var productionOutput = await _context.ProductionOutputs
                .FirstOrDefaultAsync(p =>
                    p.ProductionOutputId == id);

            if (productionOutput == null)
            {
                return NotFound();
            }

            // Pastikan data tersebut termasuk dalam scope
            // user Production yang sedang login.
            if (!allowedDataMasterIds.Contains(
                    productionOutput.DataMasterId))
            {
                return Forbid();
            }

            // Validasi Quality Input.
            if (QualityInput < 0)
            {
                ModelState.AddModelError(
                    nameof(QualityInput),
                    "Quality Input tidak boleh kurang dari 0."
                );
            }

            // Validasi Quality Output.
            if (QualityOutput < 0)
            {
                ModelState.AddModelError(
                    nameof(QualityOutput),
                    "Quality Output tidak boleh kurang dari 0."
                );
            }

            if (!ModelState.IsValid)
            {
                // Ambil kembali Data Master untuk ditampilkan
                // pada halaman Edit.
                productionOutput.DataMaster =
                    await _context.DataMasters
                        .Include(d => d.Department)
                        .FirstOrDefaultAsync(d =>
                            d.DataMasterId ==
                            productionOutput.DataMasterId);

                return View(productionOutput);
            }

            // Update hanya nilai yang memang boleh diedit.
            productionOutput.QualityInput = QualityInput;
            productionOutput.QualityOutput = QualityOutput;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Data Production Output berhasil diperbarui.";

            return RedirectToAction(
                nameof(ProductionOutput));
        }

        // Mengambil Cost Center / Product Group / Product Line
        // berdasarkan scope user Production yang sedang login.
        [HttpGet]
        public async Task<IActionResult> GetAllowedDataMasters()
        {
            // Ambil semua Data Master ID yang termasuk dalam scope
            // user Production yang sedang login.
            var allowedDataMasterIds =
                await GetAllowedDataMasterIdsAsync();

            if (!allowedDataMasterIds.Any())
            {
                return Json(new List<object>());
            }

            // Ambil semua Data Master yang diperbolehkan.
            // Product Line tetap ditampilkan karena user akan memilih
            // Product Line secara langsung pada form.
            var dataMasters = await _context.DataMasters
                .Where(d => allowedDataMasterIds.Contains(d.DataMasterId))
                .OrderBy(d => d.CostCenter)
                .ThenBy(d => d.ProductGroup)
                .ThenBy(d => d.ProductLine)
                .Select(d => new
                {
                    id = d.DataMasterId,
                    costCenter = d.CostCenter,
                    productGroup = d.ProductGroup,
                    productLine = d.ProductLine
                })
                .ToListAsync();

            return Json(dataMasters);
        }
        // Mengambil Fiscal Week berdasarkan Fiscal Year
        [HttpGet]
        public async Task<IActionResult> GetFiscalWeeks(
    int fiscalYear,
    int dataMasterId)
        {
            // Pastikan Data Master yang dipilih termasuk
            // dalam scope user Production yang sedang login.
            var allowedDataMasterIds =
                await GetAllowedDataMasterIdsAsync();

            if (!allowedDataMasterIds.Contains(dataMasterId))
            {
                return Json(new List<object>());
            }

            // Ambil semua Fiscal Week untuk Fiscal Year yang dipilih.
            var fiscalWeeks = await _context.FiscalCalendars
                .Where(f => f.FiscalYear == fiscalYear)
                .OrderBy(f => f.WeekNo)
                .Select(f => new
                {
                    weekNo = f.WeekNo,
                    startDate = f.StartDate,
                    endDate = f.EndDate
                })
                .ToListAsync();

            // Ambil tanggal mulai Fiscal Week yang sudah digunakan
            // oleh Data Master yang dipilih.
            var usedStartDates = await _context.ProductionOutputs
                .Where(p => p.DataMasterId == dataMasterId)
                .Select(p => p.FinishDate)
                .ToListAsync();

            // Hilangkan Fiscal Week yang tanggal mulainya
            // sudah digunakan oleh Data Master tersebut.
            var availableWeeks = fiscalWeeks
                .Where(f => !usedStartDates.Contains(f.startDate))
                .ToList();

            return Json(availableWeeks);
        }
    }
}