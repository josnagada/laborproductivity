using System.ComponentModel.DataAnnotations;

namespace LaborProductivity.Models
{
    // ViewModel utama untuk halaman Headcount Trend.
    // Digunakan oleh HR, CI, dan nantinya Production.
    public class HeadcountTrendViewModel
    {
        // ============================================================
        // FILTER
        // ============================================================

        [Display(Name = "Department")]
        public int? SelectedDepartmentId { get; set; }

        [Display(Name = "CC - Product Group")]
        public string? SelectedCostCenter { get; set; }

        [Display(Name = "Fiscal Year")]
        public int SelectedFiscalYear { get; set; }

        [Display(Name = "Fiscal Week")]
        public int? SelectedFiscalWeek { get; set; }

        // Menentukan apakah user sudah menekan tombol Show Data.
        public bool HasFiltered { get; set; }

        // Pesan jika tidak ada Data Master yang sesuai filter.
        public string? DataMissingNotice { get; set; }

        // ============================================================
        // DATA UNTUK FILTER
        // ============================================================

        public List<Department> Departments { get; set; } = new();

        public List<HeadcountTrendCostCenterOption> CostCenters { get; set; } = new();

        public List<int> FiscalYears { get; set; } = new();

        public List<HeadcountTrendFiscalWeekOption> FiscalWeeks { get; set; } = new();

        // ============================================================
        // DATA CHART
        // ============================================================

        // Label sumbu X:
        // WK01, WK02, WK03, dan seterusnya.
        public List<string> ChartLabels { get; set; } = new();

        // Dataset untuk masing-masing Product Line.
        public List<HeadcountTrendDataset> ChartDatasets { get; set; } = new();

        // ============================================================
        // DATA DETAIL
        // ============================================================

        // Menyimpan data harian yang sudah diambil dari Work Hours.
        // Ini berguna jika nantinya kita membutuhkan informasi detail.
        public List<HeadcountTrendDailyRow> Rows { get; set; } = new();
    }

    // ================================================================
    // OPTION CC - PRODUCT GROUP
    // ================================================================

    public class HeadcountTrendCostCenterOption
    {
        public string CostCenter { get; set; } = string.Empty;

        public string ProductGroup { get; set; } = string.Empty;

        public string DisplayText =>
            $"{CostCenter} - {ProductGroup}";
    }

    // ================================================================
    // OPTION FISCAL WEEK
    // ================================================================

    public class HeadcountTrendFiscalWeekOption
    {
        public int WeekNo { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public string DisplayText =>
            $"Week {WeekNo} ({StartDate:dd/MM/yyyy} - {EndDate:dd/MM/yyyy})";
    }

    // ================================================================
    // DATASET CHART
    // ================================================================

    public class HeadcountTrendDataset
    {
        // Contoh:
        // 440 - SMs
        // 450 - qwr
        public string Label { get; set; } = string.Empty;

        // Nilai Headcount untuk setiap Week.
        //
        // Urutannya mengikuti ChartLabels.
        //
        // Contoh:
        // WK01 = 2
        // WK02 = 3
        // WK03 = 2
        public List<int> Data { get; set; } = new();
    }

    // ================================================================
    // DATA WORK HOURS HARIAN
    // ================================================================

    public class HeadcountTrendDailyRow
    {
        public int DataMasterId { get; set; }

        public string ProductLine { get; set; } = string.Empty;

        public DateOnly Date { get; set; }

        public int WeekNo { get; set; }

        public string DayName { get; set; } = string.Empty;

        // Jam kerja standar berdasarkan hari.
        //
        // Senin-Jumat = 7.25
        // Sabtu       = 4.00
        // Minggu      = 0.00
        public decimal StandardHours { get; set; }

        public decimal NormalHours { get; set; }

        public decimal WorkHours { get; set; }

        // Overtime = Work Hours - Normal Hours
        public decimal OvertimeHours { get; set; }
    }
}