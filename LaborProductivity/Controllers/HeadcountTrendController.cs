using LaborProductivity.Data;
using LaborProductivity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaborProductivity.Controllers
{
    // Halaman ini dapat digunakan oleh HR, CI, dan Production.
    [Authorize(Roles = "hr,ci,production")]
    public class HeadcountTrendController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HeadcountTrendController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // HALAMAN UTAMA HEADCOUNT TREND
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            int? SelectedDepartmentId,
            string? SelectedCostCenter,
            int? SelectedFiscalYear,
            int? SelectedFiscalWeek,
            bool show = false)
        {
            // ========================================================
            // 1. AMBIL ROLE USER
            // ========================================================

            string currentRole =
                User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                ?.Trim()
                .ToLower()
                ?? string.Empty;

            // ========================================================
            // 2. AMBIL DEPARTMENT
            // ========================================================

            var departments = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            // ========================================================
            // 3. AMBIL FISCAL YEAR
            // ========================================================

            var fiscalYears = await _context.FiscalCalendars
                .Select(fc => fc.FiscalYear)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            // Jika belum ada Fiscal Calendar,
            // gunakan tahun sekarang seperti PHP.
            if (!fiscalYears.Any())
            {
                fiscalYears.Add(DateTime.Now.Year);
            }

            // Jika tahun yang dipilih tidak tersedia,
            // gunakan tahun terbaru.
            int selectedYear;

            if (SelectedFiscalYear.HasValue &&
                fiscalYears.Contains(SelectedFiscalYear.Value))
            {
                selectedYear = SelectedFiscalYear.Value;
            }
            else
            {
                selectedYear = fiscalYears.First();
            }

            // ========================================================
            // 4. AMBIL FISCAL WEEK BERDASARKAN FISCAL YEAR
            // ========================================================

            var fiscalWeeks = await _context.FiscalCalendars
                .Where(fc => fc.FiscalYear == selectedYear)
                .OrderBy(fc => fc.WeekNo)
                .Select(fc => new HeadcountTrendFiscalWeekOption
                {
                    WeekNo = fc.WeekNo,
                    StartDate = fc.StartDate,
                    EndDate = fc.EndDate
                })
                .ToListAsync();

            // ========================================================
            // 5. AMBIL DATA MASTER
            // ========================================================

            var dataMasterQuery = _context.DataMasters
                .Include(dm => dm.Department)
                .AsQueryable();

            // ========================================================
            // CATATAN PRODUCTION
            // ========================================================
            //
            // Untuk sekarang Production belum kita kunci ke
            // DataMasterId miliknya.
            //
            // Bagian ini akan kita perbaiki nanti setelah struktur
            // login/claim Production kita pastikan.
            //
            // Untuk sementara:
            //
            // HR  -> semua Data Master
            // CI  -> semua Data Master
            // Production -> sementara semua Data Master
            //
            // ========================================================

            var allDataMasters = await dataMasterQuery
                .OrderBy(dm => dm.ProductGroup)
                .ThenBy(dm => dm.ProductLine)
                .ToListAsync();

            // ========================================================
            // 6. DATA MASTER UNTUK FILTER CC - PRODUCT GROUP
            // ========================================================

            IEnumerable<DataMaster> filteredDataMasters =
                allDataMasters;

            if (SelectedDepartmentId.HasValue)
            {
                filteredDataMasters = filteredDataMasters
                    .Where(dm =>
                        dm.DepartmentId ==
                        SelectedDepartmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(SelectedCostCenter))
            {
                filteredDataMasters = filteredDataMasters
                    .Where(dm =>
                        dm.CostCenter ==
                        SelectedCostCenter);
            }

            var costCenters = filteredDataMasters
                .GroupBy(dm => new
                {
                    dm.CostCenter,
                    dm.ProductGroup
                })
                .OrderBy(g => g.Key.CostCenter)
                .Select(g => new HeadcountTrendCostCenterOption
                {
                    CostCenter = g.Key.CostCenter,
                    ProductGroup = g.Key.ProductGroup
                })
                .ToList();

            // ========================================================
            // 7. SIAPKAN MODEL
            // ========================================================

            var model = new HeadcountTrendViewModel
            {
                SelectedDepartmentId = SelectedDepartmentId,
                SelectedCostCenter = SelectedCostCenter,
                SelectedFiscalYear = selectedYear,
                SelectedFiscalWeek = SelectedFiscalWeek,

                Departments = departments,
                CostCenters = costCenters,
                FiscalYears = fiscalYears,
                FiscalWeeks = fiscalWeeks,

                HasFiltered = show
            };

            // ========================================================
            // 8. JIKA BELUM KLIK SHOW DATA
            // ========================================================

            if (!show)
            {
                return View(
                    GetViewPath(currentRole),
                    model);
            }

            // ========================================================
            // 9. TENTUKAN DATA MASTER YANG AKAN DIPROSES
            // ========================================================

            var finalDataMasterIds = filteredDataMasters
                .Select(dm => dm.DataMasterId)
                .ToList();

            if (!finalDataMasterIds.Any())
            {
                model.DataMissingNotice =
                    "Tidak ada Product Line yang cocok dengan filter ini.";

                return View(
                    GetViewPath(currentRole),
                    model);
            }

            // ========================================================
            // 10. QUERY WORK HOURS
            // ========================================================

            var workHoursQuery = _context.WorkHours
                .Include(wh => wh.DataMaster)
                .Where(wh =>
                    wh.FiscalYear == selectedYear &&
                    finalDataMasterIds.Contains(wh.DataMasterId));

            // Jika Fiscal Week dipilih,
            // hanya ambil week tersebut.
            if (SelectedFiscalWeek.HasValue)
            {
                workHoursQuery = workHoursQuery
                    .Where(wh =>
                        wh.WeekNo ==
                        SelectedFiscalWeek.Value);
            }

            var workHoursData = await workHoursQuery
                .OrderBy(wh => wh.DataMasterId)
                .ThenBy(wh => wh.WorkDate)
                .ToListAsync();

            // ========================================================
            // 11. KONVERSI DATA WORK HOURS KE ROW HARIAN
            // ========================================================

            foreach (var workHour in workHoursData)
            {
                var dataMaster = workHour.DataMaster;

                if (dataMaster == null)
                {
                    continue;
                }

                int dayOfWeek =
                    (int)workHour.WorkDate.DayOfWeek;

                // C#:
                // Sunday = 0
                // Monday = 1
                //
                // Sedangkan rumus PHP:
                // Monday = 1 ... Sunday = 7
                //
                int phpDayOfWeek =
                    dayOfWeek == 0
                        ? 7
                        : dayOfWeek;

                decimal standardHours =
                    GetStandardHoursForDay(phpDayOfWeek);

                decimal overtimeHours =
                    workHour.WorkHoursValue -
                    workHour.NormalHours;

                var row = new HeadcountTrendDailyRow
                {
                    DataMasterId =
                        workHour.DataMasterId,

                    ProductLine =
                        $"{dataMaster.CostCenter} - " +
                        $"{dataMaster.ProductLine}",

                    Date =
                        workHour.WorkDate,

                    WeekNo =
                        workHour.WeekNo,

                    DayName =
                        GetDayNameIndonesia(phpDayOfWeek),

                    StandardHours =
                        standardHours,

                    NormalHours =
                        workHour.NormalHours,

                    WorkHours =
                        workHour.WorkHoursValue,

                    OvertimeHours =
                        overtimeHours
                };

                model.Rows.Add(row);
            }

            // ========================================================
            // 12. AGREGASI NORMAL HOURS PER PRODUCT LINE + WEEK
            // ========================================================
            //
            // PHP:
            //
            // $weeklyHours[$pl][$wk]
            //     += normal_hours
            //
            // Jadi yang dijumlahkan adalah NORMAL HOURS.
            // ========================================================

            var weeklyHours =
                new Dictionary<string, Dictionary<string, decimal>>();

            foreach (var row in model.Rows)
            {
                string weekLabel =
                    $"WK{row.WeekNo:D2}";

                string productLine =
                    row.ProductLine;

                if (!weeklyHours.ContainsKey(productLine))
                {
                    weeklyHours[productLine] =
                        new Dictionary<string, decimal>();
                }

                if (!weeklyHours[productLine]
                    .ContainsKey(weekLabel))
                {
                    weeklyHours[productLine][weekLabel] = 0;
                }

                weeklyHours[productLine][weekLabel] +=
                    row.NormalHours;
            }

            // ========================================================
            // 13. BUAT LABEL WEEK UNTUK CHART
            // ========================================================

            var chartLabels = weeklyHours
                .SelectMany(x => x.Value.Keys)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            model.ChartLabels = chartLabels;

            // ========================================================
            // 14. STANDARD HOURS MINGGUAN
            // ========================================================
            //
            // Senin-Jumat:
            // 7.25 x 5 = 36.25
            //
            // Sabtu:
            // 4.00
            //
            // Total:
            // 40.25 jam
            // ========================================================

            const decimal weeklyStandardHours = 40.25m;

            // ========================================================
            // 15. HITUNG HEADCOUNT
            // ========================================================

            foreach (var productLineData in weeklyHours)
            {
                string productLine =
                    productLineData.Key;

                var dataset =
                    new HeadcountTrendDataset
                    {
                        Label = productLine
                    };

                foreach (var week in chartLabels)
                {
                    decimal totalNormalHours = 0;

                    if (productLineData.Value
                        .TryGetValue(
                            week,
                            out decimal value))
                    {
                        totalNormalHours = value;
                    }

                    // PHP:
                    //
                    // Weekly FTE =
                    // SUM Normal Hours / 40.25
                    //
                    decimal weeklyFte =
                        totalNormalHours /
                        weeklyStandardHours;

                    // PHP:
                    //
                    // Weekly HC = ROUND(Weekly FTE)
                    //
                    int weeklyHeadcount =
                        (int)Math.Round(
                            weeklyFte,
                            MidpointRounding.AwayFromZero);

                    dataset.Data.Add(
                        weeklyHeadcount);
                }

                model.ChartDatasets.Add(dataset);
            }

            // ========================================================
            // 16. JIKA TIDAK ADA DATA
            // ========================================================

            if (!model.Rows.Any() ||
                !model.ChartDatasets.Any())
            {
                model.DataMissingNotice =
                    "Belum ada data Work Hours untuk filter ini.";
            }

            // ========================================================
            // 17. KEMBALIKAN VIEW SESUAI ROLE
            // ========================================================

            return View(
                GetViewPath(currentRole),
                model);
        }

        // ============================================================
        // STANDARD HOURS BERDASARKAN HARI
        // ============================================================
        //
        // Sama dengan PHP:
        //
        // Senin - Jumat = 7.25
        // Sabtu         = 4.00
        // Minggu        = 0.00
        // ============================================================

        private static decimal GetStandardHoursForDay(
            int dayOfWeek)
        {
            if (dayOfWeek >= 1 && dayOfWeek <= 5)
            {
                return 7.25m;
            }

            if (dayOfWeek == 6)
            {
                return 4.00m;
            }

            return 0.00m;
        }

        // ============================================================
        // NAMA HARI INDONESIA
        // ============================================================

        private static string GetDayNameIndonesia(
            int dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "Senin",
                2 => "Selasa",
                3 => "Rabu",
                4 => "Kamis",
                5 => "Jumat",
                6 => "Sabtu",
                7 => "Minggu",
                _ => "-"
            };
        }

        // ============================================================
        // TENTUKAN FILE VIEW BERDASARKAN ROLE
        // ============================================================

        private static string GetViewPath(
            string role)
        {
            return role switch
            {
                "hr" =>
                    "~/Views/HR/HeadcountTrend.cshtml",

                "ci" =>
                    "~/Views/CI/HeadcountTrend.cshtml",

                "production" =>
                    "~/Views/Production/HeadcountTrend.cshtml",

                _ =>
                    "~/Views/Shared/Error.cshtml"
            };
        }
    }
}