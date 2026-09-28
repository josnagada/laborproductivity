using LaborProductivity.Data;
using LaborProductivity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaborProductivity.Controllers
{
    [Authorize(Roles = "hr")]
    public class WorkHoursController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WorkHoursController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // HALAMAN UTAMA WORK HOURS
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new WorkHoursViewModel
            {
                Departments = await _context.Departments
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync(),

                FiscalYears = await _context.FiscalCalendars
                    .Select(fc => fc.FiscalYear)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToListAsync(),

                FiscalWeeks = new List<int>(),
                DataMasters = new List<DataMaster>(),

                // Mengambil semua data Work Hours yang sudah tersimpan.
                ExistingWorkHours = await _context.WorkHours
                    .Include(wh => wh.DataMaster)
                    .OrderByDescending(wh => wh.FiscalYear)
                    .ThenByDescending(wh => wh.WeekNo)
                    .ToListAsync()
            };

            return View("~/Views/Hr/WorkHours.cshtml", model);
        }

        // =====================================================
        // POST: WorkHours/Create
        // SIMPAN DATA WORK HOURS BARU
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkHoursViewModel model)
        {
            // =====================================================
            // VALIDASI DATA INPUT
            // =====================================================
            if (!ModelState.IsValid)
            {
                await ReloadDropdowns(model);
                return View("~/Views/Hr/WorkHours.cshtml", model);
            }

            // =====================================================
            // AMBIL WORK DATE DARI FISCAL CALENDAR
            // =====================================================
            var fiscalCalendar = await _context.FiscalCalendars
                .FirstOrDefaultAsync(fc =>
                    fc.FiscalYear == model.FiscalYear &&
                    fc.WeekNo == model.WeekNo);

            if (fiscalCalendar == null)
            {
                ModelState.AddModelError(
                    "",
                    "Fiscal Year dan Fiscal Week tidak ditemukan."
                );

                await ReloadDropdowns(model);
                return View("~/Views/Hr/WorkHours.cshtml", model);
            }

            // =====================================================
            // AMBIL DATA MASTER YANG DIPILIH
            // DataMasterId dari dropdown hanya sebagai ID perwakilan
            // dari kombinasi Cost Center + Product Group.
            // =====================================================
            var selectedDataMaster = await _context.DataMasters
                .FirstOrDefaultAsync(dm =>
                    dm.DataMasterId == model.DataMasterId);

            if (selectedDataMaster == null)
            {
                ModelState.AddModelError(
                    "",
                    "Data Master tidak ditemukan."
                );

                await ReloadDropdowns(model);
                return View("~/Views/Hr/WorkHours.cshtml", model);
            }

            // =====================================================
            // AMBIL SEMUA PRODUCT LINE
            // berdasarkan Department + Cost Center + Product Group
            // =====================================================
            var dataMasters = await _context.DataMasters
                .Where(dm =>
                    dm.DepartmentId == selectedDataMaster.DepartmentId &&
                    dm.CostCenter == selectedDataMaster.CostCenter &&
                    dm.ProductGroup == selectedDataMaster.ProductGroup)
                .OrderBy(dm => dm.ProductLine)
                .ToListAsync();

            if (!dataMasters.Any())
            {
                ModelState.AddModelError(
                    "",
                    "Product Line untuk Cost Center dan Product Group tersebut tidak ditemukan."
                );

                await ReloadDropdowns(model);
                return View("~/Views/Hr/WorkHours.cshtml", model);
            }

            // =====================================================
            // AMBIL ID SEMUA DATA MASTER
            // =====================================================
            var dataMasterIds = dataMasters
                .Select(dm => dm.DataMasterId)
                .ToList();

            // =====================================================
            // CEK DUPLICATE
            // Fiscal Year + Week + SEMUA Product Line
            // =====================================================
            var existingWorkHours = await _context.WorkHours
                .AnyAsync(w =>
                    w.FiscalYear == model.FiscalYear &&
                    w.WeekNo == model.WeekNo &&
                    dataMasterIds.Contains(w.DataMasterId));

            if (existingWorkHours)
            {
                ModelState.AddModelError(
                    "",
                    "Work Hours untuk Cost Center + Product Group dan Fiscal Week tersebut sudah ada. Gunakan Edit untuk memperbaiki data."
                );

                await ReloadDropdowns(model);
                return View("~/Views/Hr/WorkHours.cshtml", model);
            }

            // =====================================================
            // BUAT WORK HOURS UNTUK SEMUA PRODUCT LINE
            // =====================================================
            var workHoursList = dataMasters
                .Select(dm => new WorkHours
                {
                    FiscalYear = model.FiscalYear,
                    WorkDate = fiscalCalendar.StartDate,
                    WeekNo = model.WeekNo,
                    DataMasterId = dm.DataMasterId,
                    NormalHours = model.NormalHours,
                    WorkHoursValue = model.WorkHoursValue
                })
                .ToList();

            // =====================================================
            // SIMPAN SEMUA PRODUCT LINE SEKALIGUS
            // =====================================================
            _context.WorkHours.AddRange(workHoursList);

            await _context.SaveChangesAsync();

            // =====================================================
            // KEMBALI KE HALAMAN WORK HOURS
            // =====================================================
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // GET: WorkHours/Edit/5
        // Menampilkan data Work Hours yang akan diedit.
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var workHours = await _context.WorkHours
                .Include(wh => wh.DataMaster)
                .FirstOrDefaultAsync(wh => wh.WorkHoursId == id);

            // Jika data tidak ditemukan
            if (workHours == null)
            {
                return NotFound();
            }

            // Buat ViewModel berdasarkan data yang akan diedit
            var model = new WorkHoursViewModel
            {
                FiscalYear = workHours.FiscalYear,
                WeekNo = workHours.WeekNo,
                DataMasterId = workHours.DataMasterId,
                NormalHours = workHours.NormalHours,
                WorkHoursValue = workHours.WorkHoursValue,
                WorkDate = workHours.WorkDate
            };

            await ReloadEditDropdowns(model);

            return View("~/Views/Hr/EditWorkHours.cshtml", model);
        }

        // =========================================================
        // POST: WorkHours/Edit
        // Menyimpan perubahan data Work Hours.
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WorkHoursViewModel model)
        {
            // =========================================================
            // 1. Cari data berdasarkan ID
            // =========================================================
            var workHours = await _context.WorkHours
                .FirstOrDefaultAsync(wh => wh.WorkHoursId == id);

            if (workHours == null)
            {
                return NotFound();
            }

            // =========================================================
            // 2. Validasi Model
            // =========================================================
            if (!ModelState.IsValid)
            {
                await ReloadEditDropdowns(model);
                return View("~/Views/Hr/EditWorkHours.cshtml", model);
            }

            // =========================================================
            // 3. Cari Fiscal Calendar
            // =========================================================
            var fiscalCalendar = await _context.FiscalCalendars
                .FirstOrDefaultAsync(fc =>
                    fc.FiscalYear == model.FiscalYear &&
                    fc.WeekNo == model.WeekNo);

            if (fiscalCalendar == null)
            {
                ModelState.AddModelError(
                    "",
                    "Fiscal Year dan Fiscal Week tidak ditemukan."
                );

                await ReloadEditDropdowns(model);
                return View("~/Views/Hr/EditWorkHours.cshtml", model);
            }

            // =========================================================
            // 4. Cek duplicate
            // Data yang sedang diedit sendiri tidak dihitung
            // sebagai duplicate.
            // =========================================================
            var duplicate = await _context.WorkHours
                .AnyAsync(wh =>
                    wh.WorkHoursId != id &&
                    wh.FiscalYear == model.FiscalYear &&
                    wh.WeekNo == model.WeekNo &&
                    wh.DataMasterId == model.DataMasterId);

            if (duplicate)
            {
                ModelState.AddModelError(
                    "",
                    $"Data untuk Fiscal Year {model.FiscalYear}, " +
                    $"Week {model.WeekNo}, dan Data Master tersebut " +
                    $"sudah digunakan oleh record lain."
                );

                await ReloadEditDropdowns(model);
                return View("~/Views/Hr/EditWorkHours.cshtml", model);
            }

            // =========================================================
            // 5. Update data
            // =========================================================
            workHours.FiscalYear = model.FiscalYear;
            workHours.WeekNo = model.WeekNo;
            workHours.DataMasterId = model.DataMasterId;

            // Work Date tetap mengikuti Fiscal Calendar
            workHours.WorkDate = fiscalCalendar.StartDate;

            workHours.NormalHours = model.NormalHours;
            workHours.WorkHoursValue = model.WorkHoursValue;

            // =========================================================
            // 6. Simpan perubahan
            // =========================================================
            await _context.SaveChangesAsync();

            // =========================================================
            // 7. Kembali ke halaman Work Hours
            // =========================================================
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // POST: WorkHours/Delete
        // Menghapus data Work Hours.
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Cari data berdasarkan ID
            var workHours = await _context.WorkHours
                .FirstOrDefaultAsync(wh => wh.WorkHoursId == id);

            // Jika data tidak ditemukan
            if (workHours == null)
            {
                return NotFound();
            }

            // Hapus data
            _context.WorkHours.Remove(workHours);

            // Simpan perubahan ke database
            await _context.SaveChangesAsync();

            // Kembali ke halaman Work Hours
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // MENGAMBIL DATA MASTER BERDASARKAN DEPARTMENT
        // (di-group supaya CostCenter + ProductGroup yang sama
        //  tidak tampil berkali-kali walau ProductLine-nya beda)
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> GetDataMasters(int departmentId)
        {
            var dataMasters = await _context.DataMasters
                .Where(dm => dm.DepartmentId == departmentId)
                .GroupBy(dm => new { dm.CostCenter, dm.ProductGroup })
                .Select(g => new
                {
                    dataMasterId = g.Min(x => x.DataMasterId),
                    costCenter = g.Key.CostCenter,
                    productGroup = g.Key.ProductGroup
                })
                .OrderBy(x => x.costCenter)
                .ThenBy(x => x.productGroup)
                .ToListAsync();

            return Json(dataMasters);
        }

        // =====================================================
        // MENGAMBIL FISCAL WEEK + RENTANG TANGGAL
        // BERDASARKAN FISCAL YEAR, DIKURANGI WEEK YANG
        // SUDAH DIPAKAI OLEH DATA MASTER YANG DIPILIH
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> GetFiscalWeeks(int fiscalYear, int dataMasterId)
        {
            // Ambil week yang sudah punya Work Hours
            // untuk Data Master ini di Fiscal Year tersebut
            var usedWeeks = await _context.WorkHours
                .Where(wh =>
                    wh.FiscalYear == fiscalYear &&
                    wh.DataMasterId == dataMasterId)
                .Select(wh => wh.WeekNo)
                .ToListAsync();

            var fiscalWeeks = await _context.FiscalCalendars
                .Where(fc =>
                    fc.FiscalYear == fiscalYear &&
                    !usedWeeks.Contains(fc.WeekNo))
                .OrderBy(fc => fc.WeekNo)
                .Select(fc => new
                {
                    weekNo = fc.WeekNo,
                    startDate = fc.StartDate,
                    endDate = fc.EndDate
                })
                .ToListAsync();

            return Json(fiscalWeeks);
        }

        // =====================================================
        // HELPER: ISI ULANG DATA DROPDOWN UNTUK HALAMAN CREATE
        // (Data Master difilter berdasarkan Department)
        // =====================================================
        private async Task ReloadDropdowns(WorkHoursViewModel model)
        {
            model.Departments = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            model.FiscalYears = await _context.FiscalCalendars
                .Select(fc => fc.FiscalYear)
                .Distinct()
                .OrderByDescending(year => year)
                .ToListAsync();

            model.FiscalWeeks = await _context.FiscalCalendars
                .Where(fc => fc.FiscalYear == model.FiscalYear)
                .OrderBy(fc => fc.WeekNo)
                .Select(fc => fc.WeekNo)
                .ToListAsync();

            var departmentId = await _context.DataMasters
                .Where(x => x.DataMasterId == model.DataMasterId)
                .Select(x => x.DepartmentId)
                .FirstOrDefaultAsync();

            model.DataMasters = await _context.DataMasters
                .Where(dm => dm.DepartmentId == departmentId)
                .OrderBy(dm => dm.CostCenter)
                .ThenBy(dm => dm.ProductGroup)
                .ToListAsync();
        }

        // =====================================================
        // HELPER: ISI ULANG DATA DROPDOWN UNTUK HALAMAN EDIT
        // (semua Data Master ditampilkan lengkap dengan Department)
        // =====================================================
        private async Task ReloadEditDropdowns(WorkHoursViewModel model)
        {
            model.Departments = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            model.FiscalYears = await _context.FiscalCalendars
                .Select(fc => fc.FiscalYear)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            model.DataMasters = await _context.DataMasters
                .Include(dm => dm.Department)
                .OrderBy(dm => dm.CostCenter)
                .ThenBy(dm => dm.ProductGroup)
                .ToListAsync();
        }
    }
}