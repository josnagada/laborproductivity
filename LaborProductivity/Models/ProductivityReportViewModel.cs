using System;

namespace LaborProductivity.Models
{
    /// <summary>
    /// ViewModel utama untuk halaman Labor Productivity Report.
    /// Digunakan untuk membawa filter dan hasil laporan dari Controller ke View.
    /// </summary>
    public class ProductivityReportViewModel
    {
        // ============================================================
        // FILTER REPORT
        // ============================================================

        /// <summary>
        /// Jenis laporan: "detail" atau "summary".
        /// </summary>
        public string ReportType { get; set; } = "detail";

        /// <summary>
        /// Department yang dipilih.
        /// Null berarti semua department.
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// Cost Center yang dipilih.
        /// Kosong berarti semua Cost Center.
        /// </summary>
        public string CostCenter { get; set; } = string.Empty;

        /// <summary>
        /// Fiscal Year yang dipilih.
        /// </summary>
        public int SelectedYear { get; set; }

        /// <summary>
        /// Fiscal Week yang dipilih.
        /// Null berarti semua minggu dalam Fiscal Year.
        /// </summary>
        public int? SelectedWeek { get; set; }

        // ============================================================
        // DATA UNTUK DROPDOWN FILTER
        // ============================================================

        /// <summary>
        /// Daftar Fiscal Year yang tersedia.
        /// </summary>
        public List<int> FiscalYears { get; set; } = new();

        /// <summary>
        /// Daftar Fiscal Week berdasarkan Fiscal Year.
        /// </summary>
        public List<ProductivityFiscalWeekItem> FiscalWeeks { get; set; } = new();

        /// <summary>
        /// Daftar Department untuk filter.
        /// </summary>
        public List<Department> Departments { get; set; } = new();

        /// <summary>
        /// Daftar Data Master yang digunakan
        /// untuk filter Cost Center / Product Group.
        /// </summary>
        public List<DataMaster> DataMasters { get; set; } = new();

        public List<DataMaster> FilterDataMasters { get; set; } = new();


        // ============================================================
        // HASIL REPORT
        // ============================================================

        /// <summary>
        /// Daftar periode/minggu yang ditampilkan pada tabel.
        /// </summary>
        public List<int> Periods { get; set; } = new();

        /// <summary>
        /// Baris-baris Product Line pada report.
        /// </summary>
        public List<ProductivityReportRow> Rows { get; set; } = new();

        /// <summary>
        /// Menandakan apakah user sudah menekan tombol Show Data.
        /// </summary>
        public bool HasFiltered { get; set; }

        /// <summary>
        /// Pesan jika tidak ada data yang sesuai dengan filter.
        /// </summary>
        public string DataMissingNotice { get; set; } = string.Empty;
    }


    /// <summary>
    /// Data Fiscal Week yang digunakan pada dropdown.
    /// </summary>
    public class ProductivityFiscalWeekItem
    {
        public int WeekNo { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }
    }


    /// <summary>
    /// Satu baris Product Line pada Productivity Report.
    /// </summary>
    public class ProductivityReportRow
    {
        /// <summary>
        /// ID Data Master.
        /// </summary>
        public int DataMasterId { get; set; }

        /// <summary>
        /// Label Product Line yang ditampilkan pada report.
        /// Contoh: "440 - SMD".
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Data productivity untuk masing-masing Fiscal Week.
        /// </summary>
        public Dictionary<int, ProductivityPeriodData> Periods { get; set; } = new();

        /// <summary>
        /// Total seluruh periode yang sedang ditampilkan.
        /// </summary>
        public ProductivityPeriodData Total { get; set; } = new();
    }


    /// <summary>
    /// Data productivity untuk satu Fiscal Week
    /// atau Total seluruh periode.
    /// </summary>
    public class ProductivityPeriodData
    {
        // ============================================================
        // WORK HOURS
        // ============================================================

        /// <summary>
        /// Actual Normal Hours dari input Work Hours.
        /// </summary>
        public decimal NormalHours { get; set; }

        /// <summary>
        /// Work Hours dari input Work Hours.
        /// </summary>
        public decimal WorkHours { get; set; }

        /// <summary>
        /// Overtime Hours.
        /// Rumus: Work Hours - Normal Hours.
        /// </summary>
        public decimal OtHours { get; set; }


        // ============================================================
        // PRODUCTION OUTPUT
        // ============================================================

        /// <summary>
        /// Quantity Loading.
        /// Diambil dari Quality Input.
        /// </summary>
        public decimal QtyLoading { get; set; }

        /// <summary>
        /// Quantity Output Final.
        /// Diambil dari Quality Output.
        /// </summary>
        public decimal QtyOutFinal { get; set; }


        // ============================================================
        // LABOR PRODUCTIVITY
        // ============================================================

        /// <summary>
        /// Final Baseline dalam satuan Hour per Piece.
        /// Diambil dari tbl_baseline.
        /// </summary>
        public decimal? Baseline { get; set; }

        /// <summary>
        /// Aktual Hour per Piece.
        /// Rumus: Work Hours / Qty Out Final.
        ///
        /// Null jika Qty Out Final = 0.
        /// </summary>
        public decimal? Aktual { get; set; }

        /// <summary>
        /// Saving Labor Productivity.
        /// Rumus: Baseline - Aktual.
        ///
        /// Null jika Baseline atau Aktual tidak tersedia.
        /// </summary>
        public decimal? Saving { get; set; }
    }
}