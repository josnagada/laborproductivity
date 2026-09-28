using LaborProductivity.Data;
using LaborProductivity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ClosedXML.Excel;

namespace LaborProductivity.Controllers
{
    // Report Productivity hanya bisa diakses oleh HR, CI, dan Production
    [Authorize(Roles = "hr,ci,production")]
    public class ProductivityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductivityController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Menampilkan halaman utama Labor Productivity Report
        [HttpGet]
        public async Task<IActionResult> Index(
    string? reportType,
    int? departmentId,
    string? costCenter,
    int? year,
    int? fiscalWeek)
        {
            // Jika Report Type belum dipilih, gunakan Detail sebagai default
            reportType = string.IsNullOrWhiteSpace(reportType)
                ? "detail"
                : reportType.ToLower();

            // Pastikan hanya Detail atau Summary
            if (reportType != "detail" && reportType != "summary")
            {
                reportType = "detail";
            }

            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var viewModel = new ProductivityReportViewModel
            {
                ReportType = reportType,
                DepartmentId = departmentId,
                CostCenter = costCenter ?? string.Empty
            };

            // =========================================================
            // 1. AMBIL FISCAL YEAR
            // =========================================================

            viewModel.FiscalYears = await _context.FiscalCalendars
                .Select(f => f.FiscalYear)
                .Distinct()
                .OrderByDescending(f => f)
                .ToListAsync();

            // Jika belum ada Fiscal Year di database
            if (!viewModel.FiscalYears.Any())
            {
                viewModel.FiscalYears.Add(DateTime.Now.Year);
            }

            // Jika user belum memilih tahun,
            // gunakan Fiscal Year terbaru
            var selectedYear = year ?? viewModel.FiscalYears.First();

            // Pastikan tahun yang dipilih memang tersedia
            if (!viewModel.FiscalYears.Contains(selectedYear))
            {
                selectedYear = viewModel.FiscalYears.First();
            }

            viewModel.SelectedYear = selectedYear;

            // =========================================================
            // 2. AMBIL FISCAL WEEK BERDASARKAN FISCAL YEAR
            // =========================================================

            viewModel.FiscalWeeks = await _context.FiscalCalendars
                .Where(f => f.FiscalYear == selectedYear)
                .OrderBy(f => f.WeekNo)
                .Select(f => new ProductivityFiscalWeekItem
                {
                    WeekNo = f.WeekNo,
                    StartDate = f.StartDate,
                    EndDate = f.EndDate
                })
                .ToListAsync();

            // Validasi Fiscal Week yang dipilih
            //
            // fiscalWeek sekarang berupa daftar (checkbox), bukan satu
            // angka saja. Hanya WeekNo yang benar-benar ada pada
            // Fiscal Year terpilih yang akan dipakai.
            // Validasi Fiscal Week yang dipilih.
            // Jika user memilih week yang tidak tersedia pada Fiscal Year,
            // maka week tersebut dianggap tidak dipilih.
            var validWeekNos = viewModel.FiscalWeeks
                .Select(f => f.WeekNo)
                .ToHashSet();

            int? selectedWeek = null;

            if (fiscalWeek.HasValue &&
                validWeekNos.Contains(fiscalWeek.Value))
            {
                selectedWeek = fiscalWeek.Value;
            }

            viewModel.SelectedWeek = selectedWeek;

            // =========================================================
            // 3. DEPARTMENT
            // =========================================================

            viewModel.Departments = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            // =========================================================
            // 4. DATA MASTER
            // =========================================================

            var dataMasterQuery = _context.DataMasters
                .Include(d => d.Department)
                .AsQueryable();

            // ---------------------------------------------------------
            // Production
            // ---------------------------------------------------------
            // Production hanya boleh melihat Data Master yang sesuai
            // dengan UserProductionScope milik user yang sedang login.
            // ---------------------------------------------------------

            if (userRole == "production")
            {
                var userIdClaim =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (int.TryParse(userIdClaim, out int userId))
                {
                    var scopes = await _context.UserProductionScopes
                        .Where(s => s.UserId == userId)
                        .ToListAsync();

                    if (scopes.Any())
                    {
                        // Ambil semua Data Master terlebih dahulu.
                        // Karena scope user sudah berupa List, proses pencocokan
                        // dilakukan di memory agar tidak dipaksakan ke SQL.
                        var allDataMasters = await dataMasterQuery
                            .ToListAsync();

                        var allowedDataMasterIds = allDataMasters
                            .Where(d =>
                                scopes.Any(s =>
                                    s.DepartmentId == d.DepartmentId &&
                                    s.CostCenter == d.CostCenter &&
                                    s.ProductGroup == d.ProductGroup))
                            .Select(d => d.DataMasterId)
                            .ToList();

                        dataMasterQuery =
                            dataMasterQuery.Where(d =>
                                allowedDataMasterIds.Contains(
                                    d.DataMasterId));
                    }
                    else
                    {
                        // Jika Production user tidak memiliki scope,
                        // jangan tampilkan Data Master apa pun.
                        dataMasterQuery =
                            dataMasterQuery.Where(d => false);
                    }
                }
                else
                {
                    dataMasterQuery =
                        dataMasterQuery.Where(d => false);
                }
            }

            // ---------------------------------------------------------
            // Filter Department
            // ---------------------------------------------------------

            if (departmentId.HasValue)
            {
                dataMasterQuery = dataMasterQuery
                    .Where(d =>
                        d.DepartmentId == departmentId.Value);
            }
            Console.WriteLine($"DEBUG DepartmentId: {departmentId}");

            // =====================================================
            // Data Master untuk sumber filter
            // =====================================================
            // Query ini masih mengikuti filter Department
            // dan Production Scope, tetapi belum difilter Cost Center.
            // Jadi dropdown Cost Center hanya menampilkan data
            // yang memang berada di Department yang dipilih.
            viewModel.FilterDataMasters = await dataMasterQuery
                .OrderBy(d => d.CostCenter)
                .ThenBy(d => d.ProductGroup)
                .ThenBy(d => d.ProductLine)
                .ToListAsync();

            // =====================================================
            // Cost Center filter untuk data laporan
            // =====================================================
            if (!string.IsNullOrWhiteSpace(costCenter))
            {
                dataMasterQuery = dataMasterQuery
                    .Where(d =>
                        d.CostCenter == costCenter);
            }

            // Data Master yang digunakan untuk laporan
            viewModel.DataMasters = await dataMasterQuery
                .OrderBy(d => d.CostCenter)
                .ThenBy(d => d.ProductGroup)
                .ThenBy(d => d.ProductLine)
                .ToListAsync();

            // =========================================================
            // 5. COST CENTER UNTUK PRODUCTION
            // =========================================================

            if (userRole == "production" &&
                viewModel.DataMasters.Any())
            {
                // Production tidak memilih Cost Center secara bebas.
                // Nilainya mengikuti scope user.
                viewModel.CostCenter =
                    viewModel.DataMasters
                        .Select(d => d.CostCenter)
                        .Distinct()
                        .Count() == 1
                            ? viewModel.DataMasters
                                .Select(d => d.CostCenter)
                                .First()
                            : string.Empty;
            }

            // =========================================================
            // 6. TENTUKAN APAKAH FILTER SUDAH DIGUNAKAN
            // =========================================================

            viewModel.HasFiltered =
    year.HasValue ||
    viewModel.SelectedWeek.HasValue ||
    departmentId.HasValue ||
    !string.IsNullOrWhiteSpace(costCenter);


            // =========================================================
            // 7. TENTUKAN FISCAL WEEK UNTUK REPORT
            // =========================================================
            //
            // Jika user memilih Fiscal Week tertentu,
            // report hanya menampilkan minggu tersebut.
            //
            // Jika user tidak memilih Fiscal Week,
            // report menampilkan seluruh minggu pada Fiscal Year
            // yang sedang dipilih.
            // =========================================================

            if (viewModel.HasFiltered && viewModel.DataMasters.Any())
            {
                var selectedWeeks = viewModel.FiscalWeeks
    .Where(f =>
        !viewModel.SelectedWeek.HasValue ||
        f.WeekNo == viewModel.SelectedWeek.Value)
    .OrderBy(f => f.WeekNo)
    .ToList();

                // Simpan nomor Fiscal Week yang akan ditampilkan
                // ke dalam ViewModel.
                viewModel.Periods = selectedWeeks
     .Select(f => f.WeekNo)
     .ToList();


                // =========================================================
                // 8. AMBIL DATA MASTER ID
                // =========================================================
                //
                // DataMaster yang digunakan sudah melewati seluruh filter
                // sebelumnya.
                //
                // Kita hanya mengambil ID-nya karena ID tersebut nantinya
                // digunakan untuk mencari:
                // - Work Hours
                // - Production Output
                // - Baseline
                // =========================================================

                var dataMasterIds = viewModel.DataMasters
    .Select(d => d.DataMasterId)
    .ToList();

                // Ambil semua Cost Center yang sedang digunakan dalam report.
                // Work Hours menggunakan scope Cost Center, bukan Product Line.
                var reportCostCenters = viewModel.DataMasters
                    .Select(d => d.CostCenter)
                    .Distinct()
                    .ToList();

                // =========================================================
                // 9. AMBIL DATA WORK HOURS
                // =========================================================
                //
                // Data Work Hours diambil berdasarkan:
                // - Fiscal Year yang dipilih
                // - Fiscal Week yang sedang ditampilkan
                // - Data Master yang sudah lolos filter
                // =========================================================

                var workHoursData = await _context.WorkHours
                    .Where(w =>
                        w.FiscalYear == selectedYear &&
                        dataMasterIds.Contains(w.DataMasterId) &&
                        viewModel.Periods.Contains(w.WeekNo))
                    .ToListAsync();
                // =========================================================
                // 10. AMBIL DATA PRODUCTION OUTPUT
                // =========================================================
                //
                // Data Production Output diambil berdasarkan:
                // - Fiscal Week yang sedang ditampilkan
                // - Data Master yang sudah lolos filter
                //
                // QualityInput  -> Qty Loading
                // QualityOutput -> Qty Out Final
                // =========================================================

                var productionData = await _context.ProductionOutputs
                    .Where(p =>
                        dataMasterIds.Contains(p.DataMasterId) &&
                        viewModel.Periods.Contains(p.WeekNo))
                    .ToListAsync();

                // =========================================================
                // 11. AMBIL DATA BASELINE
                // =========================================================
                //
                // Baseline dicari berdasarkan:
                // - Tahun/Fiscal Year yang dipilih
                // - Data Master yang sudah lolos filter
                //
                // BaselineHourPerPiece merupakan nilai baseline
                // dalam satuan Hour per Piece.
                // =========================================================

                var baselineData = await _context.Baselines
                    .Where(b =>
                        b.Year == selectedYear &&
                        dataMasterIds.Contains(b.DataMasterId))
                    .ToListAsync();

                // =========================================================
                // 12. BUAT DICTIONARY BASELINE
                // =========================================================
                //
                // Dictionary digunakan agar nilai baseline dapat dicari
                // berdasarkan DataMasterId dengan lebih mudah.
                //
                // Contoh:
                // DataMasterId 1 -> Baseline 0.12500
                // DataMasterId 3 -> Baseline 0.18000
                // =========================================================

                var baselineDictionary = baselineData
                    .GroupBy(b => b.DataMasterId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().BaselineHourPerPiece);

                // =========================================================
                // 13. BUAT BARIS REPORT
                // =========================================================
                //
                // Setiap Data Master akan menjadi satu baris pada
                // Labor Productivity Report.
                //
                // Contoh:
                //
                // 440 - SMD
                // 450 - Product A
                // 460 - Product B
                // =========================================================

                foreach (var dataMaster in viewModel.DataMasters)
                {
                    var row = new ProductivityReportRow
                    {
                        DataMasterId = dataMaster.DataMasterId,

                        // Label yang akan ditampilkan pada tabel report.
                        // Format:
                        // Cost Center - Product Line
                        Label = $"{dataMaster.CostCenter} - {dataMaster.ProductLine}"
                    };

                    // =========================================================
                    // 14. AMBIL BASELINE UNTUK DATA MASTER
                    // =========================================================
                    //
                    // Baseline dicari berdasarkan DataMasterId.
                    //
                    // Jika baseline tersedia:
                    //    row.Total.Baseline akan diisi.
                    //
                    // Jika baseline tidak tersedia:
                    //    nilainya tetap null.
                    // =========================================================

                    if (baselineDictionary.TryGetValue(
                        dataMaster.DataMasterId,
                        out var baselineValue))
                    {
                        row.Total.Baseline = baselineValue;
                    }

                    // =========================================================
                    // 15. SIAPKAN DATA PER FISCAL WEEK
                    // =========================================================
                    //
                    // Setiap Data Master akan memiliki data untuk setiap
                    // Fiscal Week yang dipilih.
                    //
                    // Jika belum ada data Work Hours / Production Output,
                    // nilai default tetap 0.
                    // =========================================================

                    foreach (var weekNo in viewModel.Periods)
                    {
                        row.Periods[weekNo] = new ProductivityPeriodData();
                    }

                    // =========================================================
                    // 16. AMBIL DATA WORK HOURS PER FISCAL WEEK
                    // =========================================================
                    //
                    // Data Work Hours dikelompokkan berdasarkan:
                    // - DataMasterId
                    // - WeekNo
                    //
                    // Normal Hours dan Work Hours dijumlahkan jika terdapat
                    // lebih dari satu record pada minggu yang sama.
                    // =========================================================

                    foreach (var weekNo in viewModel.Periods)
                    {
                        var weekWorkHours = workHoursData
                            .Where(w =>
                                w.DataMasterId == dataMaster.DataMasterId &&
                                w.WeekNo == weekNo)
                            .ToList();

                        if (weekWorkHours.Any())
                        {
                            row.Periods[weekNo].NormalHours =
                                weekWorkHours.Sum(w => w.NormalHours);

                            row.Periods[weekNo].WorkHours =
                                weekWorkHours.Sum(w => w.WorkHoursValue);
                        }
                    }

                    // =========================================================
                    // 17. AMBIL DATA PRODUCTION OUTPUT PER FISCAL WEEK
                    // =========================================================
                    //
                    // Qty Loading  = total Quality Input
                    // Qty Out Final = total Quality Output
                    //
                    // Data dikelompokkan berdasarkan:
                    // - DataMasterId
                    // - WeekNo
                    // =========================================================

                    foreach (var weekNo in viewModel.Periods)
                    {
                        var weekProduction = productionData
                            .Where(p =>
                                p.DataMasterId == dataMaster.DataMasterId &&
                                p.WeekNo == weekNo)
                            .ToList();

                        if (weekProduction.Any())
                        {
                            row.Periods[weekNo].QtyLoading =
                                weekProduction.Sum(p => p.QualityInput);

                            row.Periods[weekNo].QtyOutFinal =
                                weekProduction.Sum(p => p.QualityOutput);
                        }
                    }

                    // =========================================================
                    // 18. HITUNG OT HOURS PER FISCAL WEEK
                    // =========================================================
                    //
                    // Rumus:
                    // OT Hours = Work Hours - Normal Hours
                    //
                    // Jika Work Hours lebih kecil dari Normal Hours,
                    // OT tidak boleh menjadi nilai negatif.
                    // =========================================================

                    foreach (var weekNo in viewModel.Periods)
                    {
                        var period = row.Periods[weekNo];

                        period.OtHours =
                            Math.Max(
                                0,
                                period.WorkHours - period.NormalHours);
                    }

                    // =========================================================
                    // 19. HITUNG ACTUAL PER FISCAL WEEK
                    // =========================================================
                    // =========================================================
                    // RUMUS:
                    // Actual = Work Hours / Qty Out Final
                    //
                    // Actual hanya dihitung jika:
                    // - Work Hours > 0
                    // - Qty Out Final > 0
                    //
                    // Jika Work Hours atau Qty Out Final = 0,
                    // Actual tidak dapat dihitung sehingga nilainya null.
                    // =========================================================

                    foreach (var weekNo in viewModel.Periods)
                    {
                        var period = row.Periods[weekNo];

                        if (period.WorkHours > 0 &&
                            period.QtyOutFinal > 0)
                        {
                            period.Aktual =
                                period.WorkHours / period.QtyOutFinal;
                        }
                        else
                        {
                            period.Aktual = null;
                        }
                    }

                    // =========================================================
                    // 20. MASUKKAN BASELINE KE SETIAP FISCAL WEEK
                    // =========================================================
                    //
                    // Baseline berasal dari:
                    // - DataMasterId
                    // - Fiscal Year
                    //
                    // Karena baseline tidak berbeda berdasarkan minggu,
                    // nilai baseline yang sama digunakan untuk setiap week.
                    // =========================================================

                    foreach (var weekNo in viewModel.Periods)
                    {
                        row.Periods[weekNo].Baseline =
                            row.Total.Baseline;
                    }

                    // =========================================================
                    // 21. HITUNG SAVING PER FISCAL WEEK
                    // =========================================================
                    //
                    // Rumus:
                    // Saving = Baseline - Actual
                    //
                    // Jika Baseline atau Actual tidak tersedia,
                    // Saving tetap null.
                    // =========================================================

                    foreach (var weekNo in viewModel.Periods)
                    {
                        var period = row.Periods[weekNo];

                        if (period.Baseline.HasValue &&
                            period.Aktual.HasValue)
                        {
                            period.Saving =
                                period.Baseline.Value -
                                period.Aktual.Value;
                        }
                        else
                        {
                            period.Saving = null;
                        }
                    }

                    // =========================================================
                    // 22. HITUNG TOTAL UNTUK DATA MASTER
                    // =========================================================
                    //
                    // Total dihitung dari seluruh Fiscal Week yang dipilih.
                    //
                    // Untuk nilai numerik:
                    // - Normal Hours → dijumlahkan
                    // - Work Hours → dijumlahkan
                    // - OT Hours → dijumlahkan
                    // - Qty Loading → dijumlahkan
                    // - Qty Out Final → dijumlahkan
                    //
                    // Baseline tidak dijumlahkan karena baseline merupakan
                    // standar per Data Master untuk satu Fiscal Year.
                    // =========================================================

                    row.Total.NormalHours =
                        row.Periods.Values.Sum(p => p.NormalHours);

                    row.Total.WorkHours =
                        row.Periods.Values.Sum(p => p.WorkHours);

                    row.Total.OtHours =
                        row.Periods.Values.Sum(p => p.OtHours);

                    row.Total.QtyLoading =
                        row.Periods.Values.Sum(p => p.QtyLoading);

                    row.Total.QtyOutFinal =
                        row.Periods.Values.Sum(p => p.QtyOutFinal);

                    // =========================================================
                    // 23. HITUNG TOTAL ACTUAL
                    // =========================================================
                    //
                    // Rumus:
                    // Total Actual = Total Work Hours / Total Qty Out Final
                    //
                    // Actual total dihitung dari total keseluruhan agar hasil
                    // tidak dipengaruhi oleh jumlah minggu yang dipilih.
                    // =========================================================

                    if (row.Total.QtyOutFinal > 0)
                    {
                        row.Total.Aktual =
                            row.Total.WorkHours /
                            row.Total.QtyOutFinal;
                    }
                    else
                    {
                        row.Total.Aktual = null;
                    }

                    // =========================================================
                    // 24. HITUNG TOTAL SAVING
                    // =========================================================
                    //
                    // Rumus:
                    // Total Saving = Baseline - Total Actual
                    //
                    // Jika Baseline atau Actual tidak tersedia,
                    // Saving tetap null.
                    // =========================================================

                    if (row.Total.Baseline.HasValue &&
                        row.Total.Aktual.HasValue)
                    {
                        row.Total.Saving =
                            row.Total.Baseline.Value -
                            row.Total.Aktual.Value;
                    }
                    else
                    {
                        row.Total.Saving = null;
                    }

                    // Tambahkan row ke hasil report.
                    viewModel.Rows.Add(row);
                }

            }

            return View(viewModel);
        }

        // =========================================================
        // EXPORT KE EXCEL
        // =========================================================
        //
        // Method ini membuat file Excel (.xlsx) dari Labor
        // Productivity Report berdasarkan filter yang sama
        // dengan yang digunakan pada method Index().
        //
        // reportType menentukan bentuk kolom yang di-export:
        // - "summary" -> Baseline, Actual, Saving saja per minggu
        // - "detail"  -> seluruh kolom (Normal Hours, Work Hours,
        //                 OT, Qty Loading, Qty Out Final, Baseline,
        //                 Actual, Saving) per minggu
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Export(
            string? reportType,
            int? departmentId,
            string? costCenter,
            int? year,
            int? fiscalWeek)
        {
            // Menentukan jenis laporan.
            reportType = string.IsNullOrWhiteSpace(reportType)
                ? "detail"
                : reportType.ToLower();

            if (reportType != "detail" && reportType != "summary")
            {
                reportType = "detail";
            }

            // Menentukan tahun yang digunakan.
            var fiscalYears = await _context.FiscalCalendars
                .Select(f => f.FiscalYear)
                .Distinct()
                .OrderByDescending(f => f)
                .ToListAsync();

            if (!fiscalYears.Any())
            {
                fiscalYears.Add(DateTime.Now.Year);
            }

            var selectedYear = year ?? fiscalYears.First();

            if (!fiscalYears.Contains(selectedYear))
            {
                selectedYear = fiscalYears.First();
            }

            // Mengambil minggu pada fiscal year yang dipilih.
            var fiscalWeeks = await _context.FiscalCalendars
                .Where(f => f.FiscalYear == selectedYear)
                .OrderBy(f => f.WeekNo)
                .Select(f => new ProductivityFiscalWeekItem
                {
                    WeekNo = f.WeekNo,
                    StartDate = f.StartDate,
                    EndDate = f.EndDate
                })
                .ToListAsync();

            // Menentukan periode minggu.
            var selectedWeekNos = fiscalWeek.HasValue &&
                        fiscalWeeks.Any(fw => fw.WeekNo == fiscalWeek.Value)
     ? new List<int> { fiscalWeek.Value }
     : new List<int>();

            var selectedWeeks = fiscalWeeks
                .Where(f =>
                    !selectedWeekNos.Any() ||
                    selectedWeekNos.Contains(f.WeekNo))
                .OrderBy(f => f.WeekNo)
                .ToList();

            var periods = selectedWeeks
                .Select(f => f.WeekNo)
                .ToList();

            // Mengambil Data Master sesuai filter.
            var dataMasterQuery = _context.DataMasters
                .Include(d => d.Department)
                .AsQueryable();

            if (departmentId.HasValue)
            {
                dataMasterQuery = dataMasterQuery
                    .Where(d => d.DepartmentId == departmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(costCenter))
            {
                dataMasterQuery = dataMasterQuery
                    .Where(d => d.CostCenter == costCenter);
            }

            var dataMasters = await dataMasterQuery
                .OrderBy(d => d.CostCenter)
                .ThenBy(d => d.ProductGroup)
                .ThenBy(d => d.ProductLine)
                .ToListAsync();

            if (!dataMasters.Any())
            {
                return BadRequest("Tidak ada data untuk filter yang dipilih.");
            }

            var dataMasterIds = dataMasters
                .Select(d => d.DataMasterId)
                .ToList();

            // Mengambil Work Hours.
            var workHoursData = await _context.WorkHours
                .Where(w =>
                    w.FiscalYear == selectedYear &&
                    dataMasterIds.Contains(w.DataMasterId) &&
                    periods.Contains(w.WeekNo))
                .ToListAsync();

            // Mengambil Production Output.
            var productionData = await _context.ProductionOutputs
                .Where(p =>
                    dataMasterIds.Contains(p.DataMasterId) &&
                    periods.Contains(p.WeekNo))
                .ToListAsync();

            // Mengambil Baseline.
            var baselineData = await _context.Baselines
                .Where(b =>
                    b.Year == selectedYear &&
                    dataMasterIds.Contains(b.DataMasterId))
                .ToListAsync();

            var baselineDictionary = baselineData
                .GroupBy(b => b.DataMasterId)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().BaselineHourPerPiece);

            // Membuat workbook Excel.
            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add(
                reportType == "summary"
                    ? "Summary Productivity"
                    : "Detail Productivity");

            // Judul laporan.
            worksheet.Cell(1, 1).Value =
                reportType == "summary"
                    ? "Labor Productivity - Summary"
                    : "Labor Productivity - Detail";

            worksheet.Cell(2, 1).Value =
                $"Fiscal Year: {selectedYear}";

            // =========================================================
            // HEADER EXCEL
            // =========================================================

            var headerRow1 = 4;
            var headerRow2 = 5;
            var dataStartRow = 6;

            worksheet.Cell(headerRow1, 1).Value = "CC - Product Line";

            var currentColumn = 2;

            if (reportType == "summary")
            {
                foreach (var weekNo in periods)
                {
                    worksheet.Cell(headerRow1, currentColumn).Value =
                        $"Week {weekNo}";

                    worksheet.Range(
                        headerRow1,
                        currentColumn,
                        headerRow1,
                        currentColumn + 2)
                        .Merge();

                    worksheet.Cell(headerRow2, currentColumn).Value =
                        "Baseline";

                    worksheet.Cell(headerRow2, currentColumn + 1).Value =
                        "Actual (HR/PC)";

                    worksheet.Cell(headerRow2, currentColumn + 2).Value =
                        "Saving";

                    currentColumn += 3;
                }

                worksheet.Cell(headerRow1, currentColumn).Value = "Total";

                worksheet.Range(
                    headerRow1,
                    currentColumn,
                    headerRow1,
                    currentColumn + 2)
                    .Merge();

                worksheet.Cell(headerRow2, currentColumn).Value =
                    "Baseline";

                worksheet.Cell(headerRow2, currentColumn + 1).Value =
                    "Actual (HR/PC)";

                worksheet.Cell(headerRow2, currentColumn + 2).Value =
                    "Saving";
            }
            else
            {
                foreach (var weekNo in periods)
                {
                    worksheet.Cell(headerRow1, currentColumn).Value =
                        $"Week {weekNo}";

                    worksheet.Range(
                        headerRow1,
                        currentColumn,
                        headerRow1,
                        currentColumn + 7)
                        .Merge();

                    worksheet.Cell(headerRow2, currentColumn).Value =
                        "Normal Hours";

                    worksheet.Cell(headerRow2, currentColumn + 1).Value =
                        "Work Hours";

                    worksheet.Cell(headerRow2, currentColumn + 2).Value =
                        "OT";

                    worksheet.Cell(headerRow2, currentColumn + 3).Value =
                        "Qty Loading";

                    worksheet.Cell(headerRow2, currentColumn + 4).Value =
                        "Qty Out Final";

                    worksheet.Cell(headerRow2, currentColumn + 5).Value =
                        "Baseline";

                    worksheet.Cell(headerRow2, currentColumn + 6).Value =
                        "Actual";

                    worksheet.Cell(headerRow2, currentColumn + 7).Value =
                        "Saving";

                    currentColumn += 8;
                }

                worksheet.Cell(headerRow1, currentColumn).Value = "Total";

                worksheet.Range(
                    headerRow1,
                    currentColumn,
                    headerRow1,
                    currentColumn + 7)
                    .Merge();

                worksheet.Cell(headerRow2, currentColumn).Value =
                    "Normal Hours";

                worksheet.Cell(headerRow2, currentColumn + 1).Value =
                    "Work Hours";

                worksheet.Cell(headerRow2, currentColumn + 2).Value =
                    "OT";

                worksheet.Cell(headerRow2, currentColumn + 3).Value =
                    "Qty Loading";

                worksheet.Cell(headerRow2, currentColumn + 4).Value =
                    "Qty Out Final";

                worksheet.Cell(headerRow2, currentColumn + 5).Value =
                    "Baseline";

                worksheet.Cell(headerRow2, currentColumn + 6).Value =
                    "Actual";

                worksheet.Cell(headerRow2, currentColumn + 7).Value =
                    "Saving";
            }

            // =========================================================
            // MEMBUAT DATA REPORT
            // =========================================================

            var rows = new List<ProductivityReportRow>();

            foreach (var dataMaster in dataMasters)
            {
                var row = new ProductivityReportRow
                {
                    DataMasterId = dataMaster.DataMasterId,
                    Label = $"{dataMaster.CostCenter} - {dataMaster.ProductLine}"
                };

                if (baselineDictionary.TryGetValue(
                    dataMaster.DataMasterId,
                    out var baselineValue))
                {
                    row.Total.Baseline = baselineValue;
                }

                foreach (var weekNo in periods)
                {
                    row.Periods[weekNo] =
                        new ProductivityPeriodData();
                }

                foreach (var weekNo in periods)
                {
                    var weekWorkHours = workHoursData
                        .Where(w =>
                            w.DataMasterId == dataMaster.DataMasterId &&
                            w.WeekNo == weekNo)
                        .ToList();

                    row.Periods[weekNo].NormalHours =
                        weekWorkHours.Sum(w => w.NormalHours);

                    row.Periods[weekNo].WorkHours =
                        weekWorkHours.Sum(w => w.WorkHoursValue);

                    var weekProduction = productionData
                        .Where(p =>
                            p.DataMasterId == dataMaster.DataMasterId &&
                            p.WeekNo == weekNo)
                        .ToList();

                    row.Periods[weekNo].QtyLoading =
                        weekProduction.Sum(p => p.QualityInput);

                    row.Periods[weekNo].QtyOutFinal =
                        weekProduction.Sum(p => p.QualityOutput);

                    var period = row.Periods[weekNo];

                    period.OtHours =
                        Math.Max(
                            0,
                            period.WorkHours -
                            period.NormalHours);

                    if (period.QtyOutFinal > 0)
                    {
                        period.Aktual =
                            period.WorkHours /
                            period.QtyOutFinal;
                    }

                    period.Baseline =
                        row.Total.Baseline;

                    if (period.Baseline.HasValue &&
                        period.Aktual.HasValue)
                    {
                        period.Saving =
                            period.Baseline.Value -
                            period.Aktual.Value;
                    }
                }

                row.Total.NormalHours =
                    row.Periods.Values.Sum(p => p.NormalHours);

                row.Total.WorkHours =
                    row.Periods.Values.Sum(p => p.WorkHours);

                row.Total.OtHours =
                    row.Periods.Values.Sum(p => p.OtHours);

                row.Total.QtyLoading =
                    row.Periods.Values.Sum(p => p.QtyLoading);

                row.Total.QtyOutFinal =
                    row.Periods.Values.Sum(p => p.QtyOutFinal);

                if (row.Total.QtyOutFinal > 0)
                {
                    row.Total.Aktual =
                        row.Total.WorkHours /
                        row.Total.QtyOutFinal;
                }

                if (row.Total.Baseline.HasValue &&
                    row.Total.Aktual.HasValue)
                {
                    row.Total.Saving =
                        row.Total.Baseline.Value -
                        row.Total.Aktual.Value;
                }

                rows.Add(row);
            }

            // =========================================================
            // ISI DATA KE EXCEL
            // =========================================================

            var dataRow = dataStartRow;

            foreach (var row in rows)
            {
                worksheet.Cell(dataRow, 1).Value = row.Label;

                var column = 2;

                if (reportType == "summary")
                {
                    foreach (var weekNo in periods)
                    {
                        var period = row.Periods[weekNo];

                        worksheet.Cell(dataRow, column).Value =
                            period.Baseline ?? 0;

                        worksheet.Cell(dataRow, column + 1).Value =
                            period.Aktual ?? 0;

                        worksheet.Cell(dataRow, column + 2).Value =
                            period.Saving ?? 0;

                        column += 3;
                    }

                    worksheet.Cell(dataRow, column).Value =
                        row.Total.Baseline ?? 0;

                    worksheet.Cell(dataRow, column + 1).Value =
                        row.Total.Aktual ?? 0;

                    worksheet.Cell(dataRow, column + 2).Value =
                        row.Total.Saving ?? 0;
                }
                else
                {
                    foreach (var weekNo in periods)
                    {
                        var period = row.Periods[weekNo];

                        worksheet.Cell(dataRow, column).Value =
                            period.NormalHours;

                        worksheet.Cell(dataRow, column + 1).Value =
                            period.WorkHours;

                        worksheet.Cell(dataRow, column + 2).Value =
                            period.OtHours;

                        worksheet.Cell(dataRow, column + 3).Value =
                            period.QtyLoading;

                        worksheet.Cell(dataRow, column + 4).Value =
                            period.QtyOutFinal;

                        worksheet.Cell(dataRow, column + 5).Value =
                            period.Baseline ?? 0;

                        worksheet.Cell(dataRow, column + 6).Value =
                            period.Aktual ?? 0;

                        worksheet.Cell(dataRow, column + 7).Value =
                            period.Saving ?? 0;

                        column += 8;
                    }

                    worksheet.Cell(dataRow, column).Value =
                        row.Total.NormalHours;

                    worksheet.Cell(dataRow, column + 1).Value =
                        row.Total.WorkHours;

                    worksheet.Cell(dataRow, column + 2).Value =
                        row.Total.OtHours;

                    worksheet.Cell(dataRow, column + 3).Value =
                        row.Total.QtyLoading;

                    worksheet.Cell(dataRow, column + 4).Value =
                        row.Total.QtyOutFinal;

                    worksheet.Cell(dataRow, column + 5).Value =
                        row.Total.Baseline ?? 0;

                    worksheet.Cell(dataRow, column + 6).Value =
                        row.Total.Aktual ?? 0;

                    worksheet.Cell(dataRow, column + 7).Value =
                        row.Total.Saving ?? 0;
                }

                dataRow++;
            }

            // =========================================================
            // FORMAT EXCEL
            // =========================================================

            var usedRange = worksheet.RangeUsed();

            if (usedRange != null)
            {
                usedRange.Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;

                usedRange.Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;

                usedRange.Style.Border.InsideBorder =
                    XLBorderStyleValues.Thin;
            }

            worksheet.Range(
                headerRow1,
                1,
                headerRow2,
                currentColumn + (reportType == "summary" ? 2 : 7))
                .Style.Font.Bold = true;

            worksheet.Range(
                headerRow1,
                1,
                headerRow2,
                currentColumn + (reportType == "summary" ? 2 : 7))
                .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

            worksheet.Columns().AdjustToContents();

            // =========================================================
            // DOWNLOAD FILE
            // =========================================================

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;

            var fileName =
                $"Laporan_{reportType}_{selectedYear}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}