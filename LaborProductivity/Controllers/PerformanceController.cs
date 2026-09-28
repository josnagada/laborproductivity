using LaborProductivity.Data;
using LaborProductivity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaborProductivity.Controllers
{
    // Performance dapat diakses oleh HR, CI, dan Production
    // sesuai dengan role pada halaman PHP sebelumnya.
    [Authorize(Roles = "hr,ci,production")]
    public class PerformanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PerformanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // PERFORMANCE
        // =========================================================

        // Menampilkan halaman Performance
        //
        // Parameter:
        // departmentId = filter Department
        // productGroup  = filter Product Group
        // productLine   = filter Product Line / Data Master ID
        // year          = Fiscal Year
        // month         = bulan berdasarkan Fiscal Calendar
        // fiscalWeek    = Fiscal Week
        // show          = penanda bahwa tombol Show Data ditekan
        public async Task<IActionResult> Index(
            string? departmentId,
            string? productGroup,
            int? productLine,
            int? year,
            int? month,
            int? fiscalWeek,
            int? show)
        {
            // =====================================================
            // MODEL AWAL
            // =====================================================

            var model = new PerformanceViewModel();

            // Ambil role user yang sedang login.
            string currentRole = GetCurrentRole();

            model.CurrentRole = currentRole;

            // Ambil Data Master ID milik user Production.
            int? myDataMasterId = GetMyDataMasterId();

            model.MyDataMasterId = myDataMasterId;

            // =====================================================
            // DEPARTMENT
            // =====================================================

            var departments = await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            model.Departments = departments;

            // =====================================================
            // DATA MASTER
            // =====================================================

            var dataMasterQuery = _context.DataMasters
                .AsNoTracking()
                .Include(d => d.Department)
                .AsQueryable();

            // Jika user Production,
            // hanya tampilkan Data Master miliknya.
            if (currentRole == "production")
            {
                if (myDataMasterId.HasValue)
                {
                    dataMasterQuery = dataMasterQuery
                        .Where(d => d.DataMasterId == myDataMasterId.Value);
                }
                else
                {
                    // Jika ID Data Master tidak ditemukan pada login,
                    // jangan tampilkan data Production lain.
                    dataMasterQuery = dataMasterQuery
                        .Where(d => false);
                }
            }

            var dataMasterList = await dataMasterQuery
                .OrderBy(d => d.ProductGroup)
                .ThenBy(d => d.ProductLine)
                .ToListAsync();

            model.DataMasterList = dataMasterList;

            // Simpan nilai filter ke ViewModel.
            model.DepartmentId = departmentId ?? string.Empty;
            model.ProductGroup = productGroup ?? string.Empty;
            model.ProductLineId = productLine;

            // =====================================================
            // FISCAL CALENDAR
            // =====================================================

            var fiscalCalendar = await _context.FiscalCalendars
                .AsNoTracking()
                .OrderBy(fc => fc.FiscalYear)
                .ThenBy(fc => fc.WeekNo)
                .ToListAsync();

            model.FiscalCalendar = fiscalCalendar;

            // Ambil semua Fiscal Year yang tersedia.
            var fiscalYears = fiscalCalendar
                .Select(fc => fc.FiscalYear)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            model.FiscalYears = fiscalYears;

            // =====================================================
            // MENENTUKAN TAHUN
            // =====================================================

            int selectedYear;

            if (year.HasValue)
            {
                selectedYear = year.Value;
            }
            else
            {
                selectedYear = DateTime.Now.Year;
            }

            // Sama seperti PHP:
            // jika tahun yang dipilih tidak tersedia,
            // gunakan Fiscal Year pertama yang tersedia.
            if (fiscalYears.Count > 0 &&
                !fiscalYears.Contains(selectedYear))
            {
                selectedYear = fiscalYears.First();
            }

            model.SelectedYear = selectedYear;

            model.SelectedMonth = month;
            model.SelectedFiscalWeek = fiscalWeek;

            // =====================================================
            // CEK APAKAH FILTER SUDAH DI-SUBMIT
            // =====================================================

            model.HasFiltered = show.HasValue;

            // Jika belum menekan Show Data,
            // cukup tampilkan halaman dan filter.
            if (!model.HasFiltered)
            {
                return View(model);
            }

            // =====================================================
            // MENENTUKAN FISCAL WEEK YANG DIPILIH
            // =====================================================

            var selectedCalendar = fiscalCalendar
                .Where(fc => fc.FiscalYear == selectedYear)
                .ToList();

            List<int> selectedWeeks;

            if (fiscalWeek.HasValue)
            {
                // Jika user memilih Fiscal Week tertentu,
                // hanya gunakan week tersebut.
                selectedWeeks = selectedCalendar
                    .Where(fc => fc.WeekNo == fiscalWeek.Value)
                    .Select(fc => fc.WeekNo)
                    .Distinct()
                    .ToList();
            }
            else if (month.HasValue)
            {
                // Jika user memilih bulan,
                // ambil semua fiscal week yang start_date-nya
                // berada pada bulan tersebut.
                selectedWeeks = selectedCalendar
                    .Where(fc => fc.StartDate.Month == month.Value)
                    .Select(fc => fc.WeekNo)
                    .Distinct()
                    .ToList();
            }
            else
            {
                // Jika hanya Fiscal Year dipilih,
                // gunakan semua week pada tahun tersebut.
                selectedWeeks = selectedCalendar
                    .Select(fc => fc.WeekNo)
                    .Distinct()
                    .ToList();
            }

            // Jika tidak ada fiscal week yang cocok.
            if (!selectedWeeks.Any())
            {
                model.DataMissingNotice =
                    "Tidak ada Fiscal Week yang cocok dengan filter ini.";

                return View(model);
            }

            // =====================================================
            // MENENTUKAN DATA MASTER FINAL
            // =====================================================

            IQueryable<DataMaster> finalDataMasterQuery =
                _context.DataMasters
                    .AsNoTracking();

            // Production hanya boleh melihat Data Master sendiri.
            if (currentRole == "production")
            {
                if (!myDataMasterId.HasValue)
                {
                    model.DataMissingNotice =
                        "Data Master untuk user Production tidak ditemukan.";

                    return View(model);
                }

                finalDataMasterQuery = finalDataMasterQuery
                    .Where(d => d.DataMasterId == myDataMasterId.Value);
            }
            else
            {
                // Jika Department dipilih.
                if (int.TryParse(departmentId, out int departmentIdValue))
                {
                    finalDataMasterQuery = finalDataMasterQuery
                        .Where(d => d.DepartmentId == departmentIdValue);
                }

                // Jika Product Group dipilih.
                if (!string.IsNullOrWhiteSpace(productGroup))
                {
                    finalDataMasterQuery = finalDataMasterQuery
                        .Where(d => d.ProductGroup == productGroup);
                }

                // Jika Product Line dipilih.
                if (productLine.HasValue)
                {
                    finalDataMasterQuery = finalDataMasterQuery
                        .Where(d => d.DataMasterId == productLine.Value);
                }
            }

            var finalDataMasters = await finalDataMasterQuery
                .OrderBy(d => d.ProductGroup)
                .ThenBy(d => d.ProductLine)
                .ToListAsync();

            var finalDataMasterIds = finalDataMasters
                .Select(d => d.DataMasterId)
                .ToList();

            model.FinalDataMasterCount = finalDataMasterIds.Count;

            // Tidak ada Product Line yang cocok.
            if (!finalDataMasterIds.Any())
            {
                model.DataMissingNotice =
                    "Tidak ada Product Line yang cocok dengan filter ini.";

                return View(model);
            }

            // Jika lebih dari satu Product Line,
            // berarti data akan diagregasi.
            model.IsAggregate = finalDataMasterIds.Count > 1;

            // =====================================================
            // SCOPE LABEL
            // =====================================================

            model.ScopeLabel = BuildScopeLabel(
                finalDataMasters,
                selectedYear,
                month,
                fiscalWeek);

            // =====================================================
            // WORK HOURS
            // =====================================================

            var workHours = await _context.WorkHours
                .AsNoTracking()
                .Where(w =>
                    w.FiscalYear == selectedYear &&
                    selectedWeeks.Contains(w.WeekNo) &&
                    finalDataMasterIds.Contains(w.DataMasterId))
                .GroupBy(w => new
                {
                    w.DataMasterId,
                    w.WeekNo
                })
                .Select(g => new
                {
                    DataMasterId = g.Key.DataMasterId,
                    WeekNo = g.Key.WeekNo,
                    NormalHours = g.Sum(x => x.NormalHours),
                    WorkHours = g.Sum(x => x.WorkHoursValue)
                })
                .ToListAsync();

            // =====================================================
            // PRODUCTION OUTPUT
            // =====================================================

            var productionOutputs = await _context.ProductionOutputs
                .AsNoTracking()
                .Where(p =>
                    p.FinishDate.Year == selectedYear &&
                    selectedWeeks.Contains(p.WeekNo) &&
                    finalDataMasterIds.Contains(p.DataMasterId))
                .GroupBy(p => new
                {
                    p.DataMasterId,
                    p.WeekNo
                })
                .Select(g => new
                {
                    DataMasterId = g.Key.DataMasterId,
                    WeekNo = g.Key.WeekNo,
                    QtyOutFinal = g.Sum(x => x.QualityOutput)
                })
                .ToListAsync();

            // =====================================================
            // BASELINE
            // =====================================================

            var baselines = await _context.Baselines
                .AsNoTracking()
                .Where(b =>
                    b.Year == selectedYear &&
                    finalDataMasterIds.Contains(b.DataMasterId))
                .ToDictionaryAsync(
                    b => b.DataMasterId,
                    b => b.BaselineHourPerPiece);

            // =====================================================
            // MERGE WORK HOURS + PRODUCTION OUTPUT
            // =====================================================
            //
            // Struktur key:
            // DataMasterId + WeekNo
            //
            // Sama seperti array "merged" pada PHP.

            var mergedKeys = workHours
                .Select(x => new
                {
                    x.DataMasterId,
                    x.WeekNo
                })
                .Union(
                    productionOutputs.Select(x => new
                    {
                        x.DataMasterId,
                        x.WeekNo
                    }))
                .ToList();

            // =====================================================
            // AGREGASI PER FISCAL WEEK
            // =====================================================

            var weeklyResults = new Dictionary<int, WeeklyPerformanceData>();

            foreach (var key in mergedKeys)
            {
                var work = workHours.FirstOrDefault(x =>
                    x.DataMasterId == key.DataMasterId &&
                    x.WeekNo == key.WeekNo);

                var output = productionOutputs.FirstOrDefault(x =>
                    x.DataMasterId == key.DataMasterId &&
                    x.WeekNo == key.WeekNo);

                decimal normalHours = work?.NormalHours ?? 0;
                decimal workHoursValue = work?.WorkHours ?? 0;
                decimal qtyOutFinal = output?.QtyOutFinal ?? 0;

                // Cari baseline untuk Product Line tersebut.
                decimal? baseline = null;

                if (baselines.TryGetValue(
                    key.DataMasterId,
                    out decimal baselineValue))
                {
                    baseline = baselineValue;
                }

                // Ambil hasil minggu yang sudah ada,
                // atau buat object baru.
                if (!weeklyResults.TryGetValue(
                    key.WeekNo,
                    out WeeklyPerformanceData? result))
                {
                    result = new WeeklyPerformanceData
                    {
                        WeekNo = key.WeekNo
                    };

                    weeklyResults[key.WeekNo] = result;
                }

                // Akumulasi Working Hours.
                result.NormalHours += normalHours;
                result.WorkHours += workHoursValue;

                // Akumulasi Quantity Output.
                result.QtyOutFinal += qtyOutFinal;

                // Baseline weighted:
                //
                // baseline * Qty Output
                //
                // Nanti dibagi total Qty Output.
                if (baseline.HasValue)
                {
                    result.WeightedBaselineSum +=
                        baseline.Value * qtyOutFinal;
                }
            }

            // =====================================================
            // MEMBUAT DATA CHART
            // =====================================================

            foreach (var week in selectedWeeks.OrderBy(w => w))
            {
                // Pastikan setiap fiscal week tetap muncul
                // walaupun tidak memiliki data.
                if (!weeklyResults.TryGetValue(
                    week,
                    out WeeklyPerformanceData? result))
                {
                    result = new WeeklyPerformanceData
                    {
                        WeekNo = week
                    };
                }

                // -------------------------------------------------
                // OT HOURS
                // -------------------------------------------------

                decimal otHours =
                    result.WorkHours - result.NormalHours;

                // -------------------------------------------------
                // AKTUAL HR/PC
                // -------------------------------------------------

                decimal? aktual = null;

                if (result.QtyOutFinal > 0)
                {
                    aktual =
                        result.WorkHours /
                        result.QtyOutFinal;
                }

                // -------------------------------------------------
                // BASELINE HR/PC
                // -------------------------------------------------

                decimal? baseline = null;

                if (result.QtyOutFinal > 0)
                {
                    baseline =
                        result.WeightedBaselineSum /
                        result.QtyOutFinal;
                }

                // -------------------------------------------------
                // SAVING
                // -------------------------------------------------

                decimal? saving = null;

                if (baseline.HasValue && aktual.HasValue)
                {
                    saving = baseline.Value - aktual.Value;
                }

                // -------------------------------------------------
                // MASUKKAN KE CHART DATA
                // -------------------------------------------------

                model.ChartLabels.Add(
                    $"WK{week:D2}");

                model.ChartNormalHours.Add(
                    Math.Round(result.NormalHours, 2));

                model.ChartOtHours.Add(
                    Math.Round(otHours, 2));

                model.ChartQtyOut.Add(
                    Math.Round(result.QtyOutFinal, 0));

                model.ChartBaseline.Add(
                    baseline.HasValue
                        ? Math.Round(baseline.Value, 5)
                        : null);

                model.ChartAktual.Add(
                    aktual.HasValue
                        ? Math.Round(aktual.Value, 5)
                        : null);

                model.ChartSaving.Add(
                    saving.HasValue
                        ? Math.Round(saving.Value, 5)
                        : null);
            }

            // =====================================================
            // WARNING AGGREGATE
            // =====================================================

            if (model.IsAggregate)
            {
                model.DataMissingNotice =
                    $"Menampilkan gabungan {model.FinalDataMasterCount} Product Line sekaligus. " +
                    "Baseline & Aktual (Hr/Pc) dihitung menggunakan rata-rata tertimbang " +
                    "(weighted by QtyOutFinal).";
            }

            return View(model);
        }

        // =========================================================
        // HELPER - CURRENT ROLE
        // =========================================================

        private string GetCurrentRole()
        {
            if (User.IsInRole("production"))
                return "production";

            if (User.IsInRole("hr"))
                return "hr";

            if (User.IsInRole("ci"))
                return "ci";

            return string.Empty;
        }

        // =========================================================
        // HELPER - DATA MASTER ID USER PRODUCTION
        // =========================================================

        private int? GetMyDataMasterId()
        {
            // Coba beberapa nama claim agar kompatibel dengan
            // mekanisme login yang mungkin sudah dibuat sebelumnya.

            string? value =
                User.FindFirst("data_master_id")?.Value
                ?? User.FindFirst("DataMasterId")?.Value
                ?? User.FindFirst("dataMasterId")?.Value;

            if (int.TryParse(value, out int dataMasterId))
            {
                return dataMasterId;
            }

            return null;
        }

        // =========================================================
        // HELPER - SCOPE LABEL
        // =========================================================

        private string BuildScopeLabel(
            List<DataMaster> dataMasters,
            int selectedYear,
            int? month,
            int? fiscalWeek)
        {
            string scope;

            // Jika satu Product Line.
            if (dataMasters.Count == 1)
            {
                var dm = dataMasters.First();

                scope =
                    $"{dm.Department?.DepartmentName ?? "-"} → " +
                    $"{dm.ProductGroup} → " +
                    $"{dm.ProductLine}";
            }
            else
            {
                // Jika beberapa Product Line.
                var departmentNames = dataMasters
                    .Select(d => d.Department?.DepartmentName)
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .Distinct()
                    .ToList();

                var productGroups = dataMasters
                    .Select(d => d.ProductGroup)
                    .Distinct()
                    .ToList();

                scope =
                    $"{string.Join(", ", departmentNames)} → " +
                    $"{string.Join(", ", productGroups)} → " +
                    "Multiple Product Lines";
            }

            scope += $" | FY{selectedYear}";

            if (month.HasValue &&
                month.Value >= 1 &&
                month.Value <= 12)
            {
                scope += $" | {GetMonthName(month.Value)}";
            }

            if (fiscalWeek.HasValue)
            {
                scope += $" | WK{fiscalWeek.Value:D2}";
            }

            return scope;
        }

        // =========================================================
        // HELPER - NAMA BULAN
        // =========================================================

        private string GetMonthName(int month)
        {
            string[] months =
            {
                "January",
                "February",
                "March",
                "April",
                "May",
                "June",
                "July",
                "August",
                "September",
                "October",
                "November",
                "December"
            };

            if (month < 1 || month > 12)
            {
                return string.Empty;
            }

            return months[month - 1];
        }


        // =========================================================
        // CLASS SEMENTARA UNTUK HASIL AGREGASI MINGGUAN
        // =========================================================

        private class WeeklyPerformanceData
        {
            public int WeekNo { get; set; }

            public decimal NormalHours { get; set; }

            public decimal WorkHours { get; set; }

            public decimal QtyOutFinal { get; set; }

            public decimal WeightedBaselineSum { get; set; }
        }
    }
}