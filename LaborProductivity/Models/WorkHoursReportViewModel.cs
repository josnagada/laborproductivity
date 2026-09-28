using System.ComponentModel.DataAnnotations;

namespace LaborProductivity.Models
{
    // ViewModel utama untuk halaman Work Hours Report
    public class WorkHoursReportViewModel
    {
        // ============================================================
        // FILTER YANG DIPILIH USER
        // ============================================================

        // Department yang dipilih.
        // Null berarti semua department.
        [Display(Name = "Department")]
        public int? SelectedDepartmentId { get; set; }

        // Cost Center yang dipilih.
        // String kosong/null berarti semua CC.
        [Display(Name = "CC - Product Group")]
        public string? SelectedCostCenter { get; set; }

        // Fiscal Year yang dipilih.
        [Display(Name = "Fiscal Year")]
        public int SelectedFiscalYear { get; set; }

        // Fiscal Week yang dipilih.
        // Null berarti semua minggu.
        [Display(Name = "Fiscal Week")]
        public int? SelectedFiscalWeek { get; set; }


        // ============================================================
        // DATA UNTUK DROPDOWN FILTER
        // ============================================================

        // Daftar Department
        public List<Department> Departments { get; set; } = new();

        // Daftar kombinasi Cost Center + Product Group
        // yang akan ditampilkan pada dropdown.
        public List<WorkHoursReportCostCenterOption> CostCenters { get; set; } = new();

        // Daftar Fiscal Year
        public List<int> FiscalYears { get; set; } = new();

        // Daftar Fiscal Week berdasarkan Fiscal Year yang dipilih
        public List<WorkHoursReportFiscalWeekOption> FiscalWeeks { get; set; } = new();


        // ============================================================
        // DATA PIVOT REPORT
        // ============================================================

        // Daftar minggu yang benar-benar ditampilkan
        // sebagai kolom pada pivot.
        public List<int> ReportWeeks { get; set; } = new();

        // Daftar Cost Center yang sudah dikelompokkan
        // dengan Product Line untuk kebutuhan pivot.
        public List<WorkHoursReportCostCenterRow> ReportRows { get; set; } = new();

        // Total keseluruhan report.
        public WorkHoursReportTotal GrandTotal { get; set; } = new();
    }


    // ================================================================
    // OPTION DROPDOWN CC - PRODUCT GROUP
    // ================================================================

    public class WorkHoursReportCostCenterOption
    {
        public string CostCenter { get; set; } = string.Empty;

        public string ProductGroup { get; set; } = string.Empty;

        // Teks yang akan ditampilkan:
        // contoh: 450 - ROY ROGER
        public string DisplayText =>
            $"{CostCenter} - {ProductGroup}";
    }


    // ================================================================
    // OPTION DROPDOWN FISCAL WEEK
    // ================================================================

    public class WorkHoursReportFiscalWeekOption
    {
        public int WeekNo { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        // Teks yang akan ditampilkan:
        // contoh:
        // Week 1 - 01/01/2025 s/d 07/01/2025
        public string DisplayText =>
            $"Week {WeekNo} - " +
            $"{StartDate:dd/MM/yyyy} s/d {EndDate:dd/MM/yyyy}";
    }


    // ================================================================
    // BARIS COST CENTER PADA PIVOT
    // ================================================================

    public class WorkHoursReportCostCenterRow
    {
        // Contoh: 450
        public string CostCenter { get; set; } = string.Empty;

        // Contoh: ROY ROGER
        public string ProductGroup { get; set; } = string.Empty;

        // Product Line yang berada di bawah CC + Product Group
        public List<WorkHoursReportProductLineRow> ProductLines { get; set; } = new();

        // Nilai jam yang ditampilkan pada baris CC.
        // Nilai ini bukan hasil penjumlahan Product Line.
        public Dictionary<int, WorkHoursReportHours> WeeklyHours { get; set; } = new();

        // Total CC
        public WorkHoursReportHours Total { get; set; } = new();
    }


    // ================================================================
    // BARIS PRODUCT LINE
    // ================================================================

    public class WorkHoursReportProductLineRow
    {
        // Contoh:
        // qwr
        // SMs
        public string ProductLine { get; set; } = string.Empty;

        // Jam berdasarkan Fiscal Week
        public Dictionary<int, WorkHoursReportHours> WeeklyHours { get; set; } = new();

        // Total jam Product Line
        public WorkHoursReportHours Total { get; set; } = new();
    }


    // ================================================================
    // NORMAL HOURS DAN WORK HOURS
    // ================================================================

    public class WorkHoursReportHours
    {
        public decimal NormalHours { get; set; }

        public decimal WorkHours { get; set; }
    }


    // ================================================================
    // GRAND TOTAL
    // ================================================================

    public class WorkHoursReportTotal
    {
        // Total per Fiscal Week
        public Dictionary<int, WorkHoursReportHours> WeeklyHours { get; set; } = new();

        // Total seluruh minggu
        public WorkHoursReportHours Total { get; set; } = new();
    }
}