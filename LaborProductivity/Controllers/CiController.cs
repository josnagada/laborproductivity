using LaborProductivity.Data;
using LaborProductivity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace LaborProductivity.Controllers
{
    // Hanya user dengan role ci yang boleh mengakses controller ini
    [Authorize(Roles = "ci")]
    public class CiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // DASHBOARD CI
        // =========================================================

        // Menampilkan halaman utama Dashboard CI
        // Parameter "year" digunakan untuk filter Data Summary
        public async Task<IActionResult> Index(int? year)
        {
            // Ambil daftar tahun yang memang tersedia di tabel Baseline
            var baselineYears = await _context.Baselines
                .Select(b => b.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            // Jika belum ada data Baseline, gunakan tahun sekarang
            int currentYear = DateTime.Now.Year;

            if (!baselineYears.Any())
            {
                baselineYears.Add(currentYear);
            }

            // Jika user belum memilih tahun, gunakan tahun terbaru
            int selectedYear = year ?? baselineYears.First();

            // Pastikan tahun yang dipilih memang tersedia
            if (!baselineYears.Contains(selectedYear))
            {
                selectedYear = baselineYears.First();
            }

            ViewBag.SelectedYear = selectedYear;
            ViewBag.StatYears = baselineYears;

            // Ambil Baseline Tertinggi untuk tahun yang dipilih
            var highestBaseline = await _context.Baselines
                .Include(b => b.DataMaster)
                .Where(b => b.Year == selectedYear)
                .OrderByDescending(b => b.BaselineHourPerPiece)
                .FirstOrDefaultAsync();

            // Ambil Baseline Terendah untuk tahun yang dipilih
            var lowestBaseline = await _context.Baselines
                .Include(b => b.DataMaster)
                .Where(b => b.Year == selectedYear)
                .OrderBy(b => b.BaselineHourPerPiece)
                .FirstOrDefaultAsync();

            ViewBag.HighestBaseline = highestBaseline;
            ViewBag.LowestBaseline = lowestBaseline;

            // Ambil seluruh data Baseline untuk tabel
            var baselineRows = await _context.Baselines
                .Include(b => b.DataMaster)
                .OrderBy(b => b.DataMaster!.CostCenter)
                .ThenBy(b => b.DataMaster!.ProductLine)
                .ThenBy(b => b.Year)
                .ToListAsync();

            ViewBag.BaselineRows = baselineRows;

            // Ambil daftar tahun untuk kolom tabel
            var baselineTableYears = baselineRows
                .Select(b => b.Year)
                .Distinct()
                .OrderBy(y => y)
                .ToList();

            ViewBag.BaselineTableYears = baselineTableYears;

            return View();
        }


        // =========================================================
        // DEPARTMENT CATEGORY
        // =========================================================

        // Menampilkan semua Department
        public async Task<IActionResult> DepartmentCategory()
        {
            // Ambil semua department dari database
            // kemudian urutkan berdasarkan nama department
            var departments = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            return View(departments);
        }


        // Menampilkan form tambah Department
        [HttpGet]
        public IActionResult CreateDepartment()
        {
            return View();
        }


        // Menyimpan Department baru
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDepartment(Department department)
        {
            // Jika data tidak valid, tampilkan kembali form
            if (!ModelState.IsValid)
            {
                return View(department);
            }

            // Tambahkan department baru ke database
            _context.Departments.Add(department);

            await _context.SaveChangesAsync();

            // Setelah berhasil, kembali ke halaman Department Category
            return RedirectToAction(nameof(DepartmentCategory));
        }


        // Menampilkan form edit Department
        [HttpGet]
        public async Task<IActionResult> EditDepartment(int id)
        {
            // Cari department berdasarkan ID
            var department = await _context.Departments
                .FindAsync(id);

            // Jika tidak ditemukan
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }


        // Menyimpan perubahan Department
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDepartment(
            int id,
            Department department)
        {
            // Pastikan ID URL sama dengan ID data
            if (id != department.DepartmentId)
            {
                return NotFound();
            }

            // Jika validasi gagal, tampilkan kembali form
            if (!ModelState.IsValid)
            {
                return View(department);
            }

            try
            {
                // Tandai data sebagai data yang diubah
                _context.Departments.Update(department);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Pastikan data masih ada
                bool exists = await _context.Departments
                    .AnyAsync(d => d.DepartmentId == id);

                if (!exists)
                {
                    return NotFound();
                }

                throw;
            }

            //jika berhasil, simpan pesan sukses
            TempData["SuccessMessage"] = "Department berhasil diperbarui.";

            return RedirectToAction(nameof(DepartmentCategory));
        }


        // Menghapus Department
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            // Cari department berdasarkan ID
            var department = await _context.Departments
                .FindAsync(id);

            if (department == null)
            {
                return NotFound();
            }

            // Hapus department
            _context.Departments.Remove(department);

            await _context.SaveChangesAsync();

            // Simpan pesan sukses untuk ditampilkan setelah redirect
            TempData["SuccessMessage"] = "Department berhasil dihapus.";

            return RedirectToAction(nameof(DepartmentCategory));
        }


        // =========================================================
        // DATA MASTER
        // =========================================================

        // Menampilkan semua Data Master
        public async Task<IActionResult> DataMaster()
        {
            // Ambil semua data master dari database
            // sekaligus mengambil nama Department melalui relasi
            // antara tbl_data_master dan tbl_department.
            var dataMasters = await _context.DataMasters
                .Include(d => d.Department)
                .OrderBy(d => d.DataMasterId)
                .ToListAsync();

            return View(dataMasters);
        }

        // Menampilkan form tambah Data Master
        [HttpGet]
        public async Task<IActionResult> CreateDataMaster()
        {
            // Ambil semua department untuk ditampilkan
            // pada dropdown Department.
            var departments = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            ViewBag.Departments = departments;

            return View();
        }


        // Menyimpan Data Master baru
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDataMaster(DataMaster dataMaster)
        {
            // Jika data tidak valid, tampilkan kembali form.
            // Department tetap harus dikirim ulang ke View
            // agar dropdown tidak kosong.
            if (!ModelState.IsValid)
            {
                var departments = await _context.Departments
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                ViewBag.Departments = departments;

                return View(dataMaster);
            }

            // Tambahkan Data Master baru ke database.
            _context.DataMasters.Add(dataMaster);

            await _context.SaveChangesAsync();

            // Setelah berhasil, kembali ke halaman Data Master.
            return RedirectToAction(nameof(DataMaster));
        }

        // Menampilkan form edit Data Master
        [HttpGet]
        public async Task<IActionResult> EditDataMaster(int id)
        {
            // Cari Data Master berdasarkan ID
            var dataMaster = await _context.DataMasters
                .FindAsync(id);

            // Jika data tidak ditemukan
            if (dataMaster == null)
            {
                return NotFound();
            }

            // Ambil semua department untuk dropdown
            var departments = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            ViewBag.Departments = departments;

            return View(dataMaster);
        }

        // Menghapus Data Master
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDataMaster(int id)
        {
            // Cari Data Master berdasarkan ID
            var dataMaster = await _context.DataMasters
                .FindAsync(id);

            // Jika data tidak ditemukan
            if (dataMaster == null)
            {
                return NotFound();
            }

            try
            {
                // Hapus Data Master dari database
                _context.DataMasters.Remove(dataMaster);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Jika Data Master masih digunakan oleh tabel lain,
                // misalnya tbl_work_hours, SQL Server dapat menolak
                // proses penghapusan karena Foreign Key.
                TempData["ErrorMessage"] =
                    "Data Master tidak dapat dihapus karena masih digunakan oleh data lain.";

                return RedirectToAction(nameof(DataMaster));
            }

            // Setelah berhasil dihapus, kembali ke halaman Data Master
            TempData["SuccessMessage"] =
                "Data Master berhasil dihapus.";

            return RedirectToAction(nameof(DataMaster));
        }



        // Menyimpan perubahan Data Master
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDataMaster(
            int id,
            DataMaster dataMaster)
        {
            // Pastikan ID URL sama dengan ID data
            if (id != dataMaster.DataMasterId)
            {
                return NotFound();
            }

            // Jika data tidak valid,
            // kirim kembali daftar department ke View.
            if (!ModelState.IsValid)
            {
                var departments = await _context.Departments
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                ViewBag.Departments = departments;

                return View(dataMaster);
            }

            try
            {
                // Tandai Data Master sebagai data yang diubah
                _context.DataMasters.Update(dataMaster);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Pastikan data masih ada
                bool exists = await _context.DataMasters
                    .AnyAsync(d => d.DataMasterId == id);

                if (!exists)
                {
                    return NotFound();
                }

                throw;
            }

            // Setelah berhasil, kembali ke halaman Data Master
            return RedirectToAction(nameof(DataMaster));
        }
        // =========================================================
        // BASELINE
        // =========================================================

        // Menampilkan semua data Baseline
        public async Task<IActionResult> Baseline()
        {
            // Ambil semua data baseline
            var baselines = await _context.Baselines
                .Include(b => b.DataMaster)
                    .ThenInclude(dm => dm!.Department)
                .OrderByDescending(b => b.Year)
                .ThenBy(b => b.DataMaster!.CostCenter)
                .ThenBy(b => b.DataMaster!.ProductLine)
                .ToListAsync();


            // Ambil semua department untuk dropdown modal
            ViewBag.Departments = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();


            // Ambil semua Data Master untuk dropdown Cost Center
            ViewBag.DataMasters = await _context.DataMasters
                .OrderBy(d => d.CostCenter)
                .ThenBy(d => d.ProductLine)
                .ToListAsync();


            return View(baselines);
        }
        // Menampilkan halaman untuk menambah Baseline
        // =========================================================
        // CREATE BASELINE
        // =========================================================

        // Menampilkan form tambah Baseline
        public async Task<IActionResult> CreateBaseline()
        {
            // Mengambil semua department untuk dropdown
            var departments = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            // Mengambil semua Data Master untuk kebutuhan
            // dropdown Cost Center berdasarkan Department
            var dataMasters = await _context.DataMasters
                .OrderBy(d => d.CostCenter)
                .ThenBy(d => d.ProductLine)
                .ToListAsync();

            ViewBag.Departments = departments;
            ViewBag.DataMasters = dataMasters;

            return View();
        }

        // Menyimpan Baseline baru
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBaseline(
            int departmentId,
            string costCenter,
            int year,
            decimal baselineHourPerPiece)
        {
            // =====================================================
            // VALIDASI INPUT
            // =====================================================

            if (departmentId <= 0)
                ModelState.AddModelError("", "You must select a department.");

            if (string.IsNullOrWhiteSpace(costCenter))
                ModelState.AddModelError("", "You must select a cost center.");

            if (year <= 0)
                ModelState.AddModelError("", "Year must be filled.");

            if (baselineHourPerPiece <= 0)
                ModelState.AddModelError(
                    "",
                    "Baseline Hour per Piece must be filled with a number.");

            if (!ModelState.IsValid)
            {
                // Jika validasi gagal, data dropdown harus dikirim ulang
                ViewBag.Departments = await _context.Departments
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                ViewBag.DataMasters = await _context.DataMasters
                    .OrderBy(d => d.CostCenter)
                    .ThenBy(d => d.ProductLine)
                    .ToListAsync();

                return View();
            }

            // =====================================================
            // CARI SEMUA PRODUCT LINE
            // BERDASARKAN DEPARTMENT + COST CENTER
            // =====================================================

            var matchedDataMasters = await _context.DataMasters
                .Where(d =>
                    d.DepartmentId == departmentId &&
                    d.CostCenter == costCenter)
                .ToListAsync();

            // Sama seperti PHP:
            // jika tidak ada Product Line untuk kombinasi tersebut,
            // proses tidak boleh dilanjutkan.
            if (!matchedDataMasters.Any())
            {
                ModelState.AddModelError(
                    "",
                    "No product line found for this Department + Cost Center combination.");

                ViewBag.Departments = await _context.Departments
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                ViewBag.DataMasters = await _context.DataMasters
                    .OrderBy(d => d.CostCenter)
                    .ThenBy(d => d.ProductLine)
                    .ToListAsync();

                return View();
            }

            // =====================================================
            // INSERT / UPDATE BASELINE
            // =====================================================

            foreach (var dataMaster in matchedDataMasters)
            {
                // Cek apakah Data Master + Year sudah memiliki baseline
                var existingBaseline = await _context.Baselines
                    .FirstOrDefaultAsync(b =>
                        b.DataMasterId == dataMaster.DataMasterId &&
                        b.Year == year);

                if (existingBaseline != null)
                {
                    // Jika sudah ada → UPDATE
                    existingBaseline.BaselineHourPerPiece =
                        baselineHourPerPiece;
                }
                else
                {
                    // Jika belum ada → INSERT
                    var baseline = new Baseline
                    {
                        DataMasterId = dataMaster.DataMasterId,
                        Year = year,
                        BaselineHourPerPiece = baselineHourPerPiece
                    };

                    _context.Baselines.Add(baseline);
                }
            }

            // Simpan semua perubahan ke SQL Server
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Baseline data saved for {matchedDataMasters.Count} product line(s) successfully!";

            // Kembali ke halaman Baseline
            return RedirectToAction(nameof(Baseline));
        }


        // =========================================================
        // EDIT BASELINE
        // =========================================================

        // Menampilkan form edit Baseline
        // Catatan: Department, Cost Center, Product Line TIDAK diubah lewat sini
        // (itu sudah menentukan baris mana yang di-edit). Yang bisa diubah
        // hanya Year dan Baseline Hour/Pc, sesuai baseline_id yang dipilih.
        [HttpGet]
        public async Task<IActionResult> EditBaseline(int id)
        {
            var baseline = await _context.Baselines
                .Include(b => b.DataMaster)
                    .ThenInclude(dm => dm!.Department)
                .FirstOrDefaultAsync(b => b.BaselineId == id);

            if (baseline == null)
            {
                return NotFound();
            }

            return View(baseline);
        }

        // Menyimpan perubahan Baseline
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBaseline(
            int id,
            int year,
            decimal baselineHourPerPiece)
        {
            var baseline = await _context.Baselines
                .Include(b => b.DataMaster)
                    .ThenInclude(dm => dm!.Department)
                .FirstOrDefaultAsync(b => b.BaselineId == id);

            if (baseline == null)
            {
                return NotFound();
            }

            // =====================================================
            // VALIDASI INPUT
            // =====================================================

            if (year <= 0)
                ModelState.AddModelError("", "Year must be filled.");

            if (baselineHourPerPiece <= 0)
                ModelState.AddModelError(
                    "",
                    "Baseline Hour per Piece must be filled with a number.");

            // Satu Data Master hanya boleh punya 1 baseline per tahun.
            // Cek apakah tahun baru yang dipilih sudah dipakai
            // oleh baseline lain milik Data Master yang sama.
            bool isDuplicate = await _context.Baselines
                .AnyAsync(b =>
                    b.DataMasterId == baseline.DataMasterId &&
                    b.Year == year &&
                    b.BaselineId != id);

            if (isDuplicate)
            {
                ModelState.AddModelError(
                    "",
                    "This product line already has a baseline for the selected year.");
            }

            if (!ModelState.IsValid)
            {
                // Supaya input yang barusan diketik tidak hilang di form
                baseline.Year = year;
                baseline.BaselineHourPerPiece = baselineHourPerPiece;

                return View(baseline);
            }

            // =====================================================
            // UPDATE BASELINE
            // =====================================================

            baseline.Year = year;
            baseline.BaselineHourPerPiece = baselineHourPerPiece;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Baseline berhasil diperbarui.";

            return RedirectToAction(nameof(Baseline));
        }


        // =========================================================
        // DELETE BASELINE
        // =========================================================

        // Menghapus data Baseline berdasarkan Id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBaseline(int id)
        {
            var baseline = await _context.Baselines
                .FirstOrDefaultAsync(b => b.BaselineId == id);

            if (baseline == null)
            {
                return NotFound();
            }

            _context.Baselines.Remove(baseline);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Baseline berhasil dihapus.";

            return RedirectToAction(nameof(Baseline));
        }

    }
}