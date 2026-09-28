using LaborProductivity.Data;
using LaborProductivity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaborProductivity.Controllers
{
    [Authorize(Roles = "hr")]
    public class WorkHoursReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WorkHoursReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int? SelectedDepartmentId,
            string? SelectedCostCenter,
            int? SelectedFiscalYear,
            int? SelectedFiscalWeek)
        {
            // =========================================================
            // 1. Ambil Department
            // =========================================================
            var departments = await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            // =========================================================
            // 2. Ambil Fiscal Year
            // =========================================================
            var fiscalYears = await _context.FiscalCalendars
                .AsNoTracking()
                .Select(fc => fc.FiscalYear)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            if (!fiscalYears.Any())
            {
                return View(
                    "~/Views/HR/WorkHoursReport.cshtml",
                    new WorkHoursReportViewModel
                    {
                        Departments = departments,
                        FiscalYears = new List<int>()
                    });
            }

            // =========================================================
            // 3. Tentukan Fiscal Year
            //
            // Jika user memilih tahun yang tersedia,
            // gunakan tahun tersebut.
            //
            // Jika belum memilih, gunakan tahun sekarang
            // jika tersedia. Jika tidak, gunakan tahun terbaru.
            // =========================================================
            int selectedFiscalYear;

            if (SelectedFiscalYear.HasValue &&
                fiscalYears.Contains(SelectedFiscalYear.Value))
            {
                selectedFiscalYear = SelectedFiscalYear.Value;
            }
            else
            {
                int currentYear = DateTime.Now.Year;

                selectedFiscalYear = fiscalYears.Contains(currentYear)
                    ? currentYear
                    : fiscalYears.First();
            }

            // =========================================================
            // 4. Ambil Fiscal Week berdasarkan Fiscal Year
            // =========================================================
            var fiscalWeekOptions = await _context.FiscalCalendars
                .AsNoTracking()
                .Where(fc => fc.FiscalYear == selectedFiscalYear)
                .OrderBy(fc => fc.WeekNo)
                .Select(fc => new WorkHoursReportFiscalWeekOption
                {
                    WeekNo = fc.WeekNo,
                    StartDate = fc.StartDate,
                    EndDate = fc.EndDate
                })
                .ToListAsync();

            // =========================================================
            // 5. Ambil Data Master
            // =========================================================
            var dataMasters = await _context.DataMasters
                .AsNoTracking()
                .Include(dm => dm.Department)
                .OrderBy(dm => dm.CostCenter)
                .ThenBy(dm => dm.ProductGroup)
                .ThenBy(dm => dm.ProductLine)
                .ToListAsync();

            // =========================================================
            // 6. Filter Data Master berdasarkan Department
            // =========================================================
            var filteredDataMasters = dataMasters.AsEnumerable();

            if (SelectedDepartmentId.HasValue)
            {
                filteredDataMasters = filteredDataMasters
                    .Where(dm =>
                        dm.DepartmentId == SelectedDepartmentId.Value);
            }

            // =========================================================
            // 7. Buat daftar CC - Product Group
            // =========================================================
            var costCenterOptions = filteredDataMasters
                .GroupBy(dm => new
                {
                    dm.CostCenter,
                    dm.ProductGroup
                })
                .OrderBy(g => g.Key.CostCenter)
                .ThenBy(g => g.Key.ProductGroup)
                .Select(g => new WorkHoursReportCostCenterOption
                {
                    CostCenter = g.Key.CostCenter,
                    ProductGroup = g.Key.ProductGroup
                })
                .ToList();

            // =========================================================
            // 8. Query Work Hours
            //
            // Fiscal Year selalu menjadi filter utama.
            // =========================================================
            var workHoursQuery = _context.WorkHours
                .AsNoTracking()
                .Include(wh => wh.DataMaster)
                .Where(wh =>
                    wh.FiscalYear == selectedFiscalYear);

            // Filter Fiscal Week
            if (SelectedFiscalWeek.HasValue)
            {
                workHoursQuery = workHoursQuery
                    .Where(wh =>
                        wh.WeekNo == SelectedFiscalWeek.Value);
            }

            // Filter Department
            if (SelectedDepartmentId.HasValue)
            {
                workHoursQuery = workHoursQuery
                    .Where(wh =>
                        wh.DataMaster != null &&
                        wh.DataMaster.DepartmentId ==
                        SelectedDepartmentId.Value);
            }

            // Filter Cost Center
            if (!string.IsNullOrWhiteSpace(SelectedCostCenter))
            {
                workHoursQuery = workHoursQuery
                    .Where(wh =>
                        wh.DataMaster != null &&
                        wh.DataMaster.CostCenter ==
                        SelectedCostCenter);
            }

            // Eksekusi query
            var workHoursData = await workHoursQuery
                .OrderBy(wh => wh.DataMaster!.CostCenter)
                .ThenBy(wh => wh.DataMaster!.ProductLine)
                .ThenBy(wh => wh.WeekNo)
                .ToListAsync();

            // =========================================================
            // 9. Tentukan Data Master yang akan ditampilkan
            // =========================================================
            var reportDataMasters = dataMasters.AsEnumerable();

            if (SelectedDepartmentId.HasValue)
            {
                reportDataMasters = reportDataMasters
                    .Where(dm =>
                        dm.DepartmentId ==
                        SelectedDepartmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(SelectedCostCenter))
            {
                reportDataMasters = reportDataMasters
                    .Where(dm =>
                        dm.CostCenter ==
                        SelectedCostCenter);
            }

            // =========================================================
            // 10. Kelompokkan berdasarkan CC + Product Group
            // =========================================================
            var productLineGroups = reportDataMasters
                .GroupBy(dm => new
                {
                    dm.CostCenter,
                    dm.ProductGroup
                })
                .OrderBy(g => g.Key.CostCenter)
                .ThenBy(g => g.Key.ProductGroup)
                .ToList();

            // =========================================================
            // 11. Buat lookup Work Hours
            // =========================================================
            var hoursLookup = workHoursData
                .Where(wh => wh.DataMaster != null)
                .GroupBy(wh => new
                {
                    CostCenter = wh.DataMaster!.CostCenter,
                    ProductGroup = wh.DataMaster.ProductGroup,
                    ProductLine = wh.DataMaster.ProductLine,
                    wh.WeekNo
                })
                .ToDictionary(
                    g => (
                        g.Key.CostCenter,
                        g.Key.ProductGroup,
                        g.Key.ProductLine,
                        g.Key.WeekNo
                    ),
                    g => new WorkHoursReportHours
                    {
                        NormalHours = g.Sum(x => x.NormalHours),
                        WorkHours = g.Sum(x => x.WorkHoursValue)
                    });

            // =========================================================
            // 12. Tentukan Week yang memiliki data
            // =========================================================
            var reportWeeks = workHoursData
                .Select(wh => wh.WeekNo)
                .Distinct()
                .OrderBy(w => w)
                .ToList();

            var reportRows =
                new List<WorkHoursReportCostCenterRow>();

            var grandTotal =
                new WorkHoursReportTotal();

            // Inisialisasi Grand Total per week
            foreach (var week in reportWeeks)
            {
                grandTotal.WeeklyHours[week] =
                    new WorkHoursReportHours();
            }

            // =========================================================
            // 13. Proses setiap CC + Product Group
            // =========================================================
            foreach (var group in productLineGroups)
            {
                // Ambil semua Product Line yang berada di dalam
                // Cost Center + Product Group yang sama.
                var productLines = group
                    .OrderBy(dm => dm.ProductLine)
                    .Select(dm => dm.ProductLine)
                    .Distinct()
                    .ToList();

                if (!productLines.Any())
                {
                    continue;
                }

                var costCenterRow = new WorkHoursReportCostCenterRow
                {
                    CostCenter = group.Key.CostCenter,
                    ProductGroup = group.Key.ProductGroup
                };

                // =====================================================
                // 14. Tentukan nilai sumber untuk setiap Week
                //
                // Jika dalam satu CC + Product Group terdapat beberapa
                // Product Line, kita hanya mengambil SATU data Work Hours
                // sebagai sumber.
                //
                // Contoh:
                //
                // 450 - ROY ROGER
                // qwr = 100 / 120
                // SMs = tidak ada
                //
                // Maka nilai sumber Week tersebut adalah:
                // 100 / 120
                //
                // Nilai ini nantinya ditampilkan ke semua Product Line.
                // =====================================================
                var sourceHoursByWeek =
                    new Dictionary<int, WorkHoursReportHours>();

                foreach (var week in reportWeeks)
                {
                    // Cari data Work Hours yang tersedia pada
                    // CC + Product Group + Week yang sedang diproses.
                    var sourceWorkHour = workHoursData
                        .Where(wh =>
                            wh.DataMaster != null &&
                            wh.DataMaster.CostCenter ==
                                group.Key.CostCenter &&
                            wh.DataMaster.ProductGroup ==
                                group.Key.ProductGroup &&
                            wh.WeekNo == week)
                        .OrderBy(wh => wh.DataMaster!.ProductLine)
                        .FirstOrDefault();

                    if (sourceWorkHour != null)
                    {
                        sourceHoursByWeek[week] =
                            new WorkHoursReportHours
                            {
                                NormalHours =
                                    sourceWorkHour.NormalHours,

                                WorkHours =
                                    sourceWorkHour.WorkHoursValue
                            };
                    }
                    else
                    {
                        // Jika tidak ada data pada minggu tersebut,
                        // gunakan 0.
                        sourceHoursByWeek[week] =
                            new WorkHoursReportHours();
                    }
                }

                // =====================================================
                // 15. Buat baris Product Line
                //
                // Semua Product Line mendapatkan nilai yang sama dari
                // sourceHoursByWeek.
                //
                // Penting:
                // Nilai Product Line tidak dijumlahkan untuk menentukan
                // Total CC.
                // =====================================================
                foreach (var productLine in productLines)
                {
                    var productLineRow =
                        new WorkHoursReportProductLineRow
                        {
                            ProductLine = productLine
                        };

                    foreach (var week in reportWeeks)
                    {
                        var sourceHours =
                            sourceHoursByWeek[week];

                        productLineRow.WeeklyHours[week] =
                            new WorkHoursReportHours
                            {
                                NormalHours =
                                    sourceHours.NormalHours,

                                WorkHours =
                                    sourceHours.WorkHours
                            };
                    }

                    // Total Product Line
                    productLineRow.Total =
                        new WorkHoursReportHours();

                    foreach (var week in reportWeeks)
                    {
                        var hours =
                            productLineRow.WeeklyHours[week];

                        productLineRow.Total.NormalHours +=
                            hours.NormalHours;

                        productLineRow.Total.WorkHours +=
                            hours.WorkHours;
                    }

                    costCenterRow.ProductLines.Add(productLineRow);
                }

                // =====================================================
                // 16. Nilai CC
                //
                // CC hanya menggunakan SATU nilai sumber per Week.
                //
                // Jadi jika:
                //
                // qwr = 100 / 120
                // SMs = 100 / 120
                //
                // CC tetap:
                //
                // 100 / 120
                //
                // BUKAN:
                //
                // 200 / 240
                // =====================================================
                foreach (var week in reportWeeks)
                {
                    var sourceHours =
                        sourceHoursByWeek[week];

                    costCenterRow.WeeklyHours[week] =
                        new WorkHoursReportHours
                        {
                            NormalHours =
                                sourceHours.NormalHours,

                            WorkHours =
                                sourceHours.WorkHours
                        };
                }

                // =====================================================
                // 17. Total CC
                // =====================================================
                costCenterRow.Total =
                    new WorkHoursReportHours();

                foreach (var week in reportWeeks)
                {
                    var hours =
                        costCenterRow.WeeklyHours[week];

                    costCenterRow.Total.NormalHours +=
                        hours.NormalHours;

                    costCenterRow.Total.WorkHours +=
                        hours.WorkHours;

                    // Grand Total dihitung dari CC.
                    //
                    // Jadi CC 450 hanya dihitung satu kali,
                    // walaupun mempunyai beberapa Product Line.
                    grandTotal.WeeklyHours[week].NormalHours +=
                        hours.NormalHours;

                    grandTotal.WeeklyHours[week].WorkHours +=
                        hours.WorkHours;
                }

                reportRows.Add(costCenterRow);
            }

            // =========================================================
            // 17. Grand Total
            // =========================================================
            grandTotal.Total =
                new WorkHoursReportHours();

            foreach (var week in reportWeeks)
            {
                var hours =
                    grandTotal.WeeklyHours[week];

                grandTotal.Total.NormalHours +=
                    hours.NormalHours;

                grandTotal.Total.WorkHours +=
                    hours.WorkHours;
            }

            // =========================================================
            // 18. Buat ViewModel
            // =========================================================
            var model = new WorkHoursReportViewModel
            {
                SelectedDepartmentId =
                    SelectedDepartmentId,

                SelectedCostCenter =
                    SelectedCostCenter,

                SelectedFiscalYear =
                    selectedFiscalYear,

                SelectedFiscalWeek =
                    SelectedFiscalWeek,

                Departments =
                    departments,

                CostCenters =
                    costCenterOptions,

                FiscalYears =
                    fiscalYears,

                FiscalWeeks =
                    fiscalWeekOptions,

                ReportWeeks =
                    reportWeeks,

                ReportRows =
                    reportRows,

                GrandTotal =
                    grandTotal
            };

            return View(
                "~/Views/HR/WorkHoursReport.cshtml",
                model);
        }
    }
}