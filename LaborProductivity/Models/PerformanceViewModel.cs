using System;
using System.Collections.Generic;

namespace LaborProductivity.Models
{
    // ============================================================
    // VIEW MODEL UNTUK HALAMAN PERFORMANCE TREND
    // ============================================================
    // ViewModel ini digunakan untuk mengirim:
    // 1. Data filter
    // 2. Data Fiscal Calendar
    // 3. Data hasil perhitungan chart
    // dari Controller ke file .cshtml.
    // ============================================================

    public class PerformanceViewModel
    {
        // ========================================================
        // FILTER DEPARTMENT
        // ========================================================

        // Daftar seluruh department untuk dropdown.
        public List<Department> Departments { get; set; } = new();

        // Department yang sedang dipilih.
        // Kosong berarti "All Departments".
        public string DepartmentId { get; set; } = string.Empty;


        // ========================================================
        // FILTER PRODUCT GROUP
        // ========================================================

        // Nilai Product Group yang sedang dipilih.
        // Kosong berarti "All Product Groups".
        public string ProductGroup { get; set; } = string.Empty;


        // ========================================================
        // FILTER PRODUCT LINE
        // ========================================================

        // Data Master yang sedang dipilih.
        // Kosong berarti "All Product Line".
        public int? ProductLineId { get; set; }


        // Daftar Data Master yang digunakan untuk cascading filter.
        public List<DataMaster> DataMasterList { get; set; } = new();


        // ========================================================
        // FILTER FISCAL YEAR
        // ========================================================

        // Daftar Fiscal Year yang tersedia.
        public List<int> FiscalYears { get; set; } = new();

        // Fiscal Year yang sedang dipilih.
        public int SelectedYear { get; set; }


        // ========================================================
        // FILTER MONTH
        // ========================================================

        // 1 = January
        // 2 = February
        // ...
        // 12 = December
        //
        // Null berarti "All Months".
        public int? SelectedMonth { get; set; }


        // ========================================================
        // FILTER FISCAL WEEK
        // ========================================================

        // Fiscal Week yang sedang dipilih.
        //
        // Null berarti "All Weeks".
        public int? SelectedFiscalWeek { get; set; }


        // ========================================================
        // FISCAL CALENDAR
        // ========================================================

        // Data Fiscal Calendar digunakan oleh JavaScript
        // untuk membuat cascading:
        //
        // Fiscal Year -> Month -> Fiscal Week
        public List<FiscalCalendar> FiscalCalendar { get; set; } = new();


        // ========================================================
        // STATUS FILTER / DATA
        // ========================================================

        // Menandakan apakah tombol "Show Data" sudah ditekan.
        //
        // PHP:
        // $has_filtered = isset($_GET['show']);
        public bool HasFiltered { get; set; }


        // Pesan jika tidak ada Data Master yang cocok.
        public string DataMissingNotice { get; set; } = string.Empty;


        // Menandakan apakah data merupakan gabungan
        // lebih dari satu Product Line.
        //
        // Digunakan untuk menampilkan warning:
        // "Menampilkan gabungan X Product Line sekaligus."
        public bool IsAggregate { get; set; }


        // Jumlah Product Line yang digabung.
        public int FinalDataMasterCount { get; set; }


        // ========================================================
        // LABEL RINGKASAN FILTER
        // ========================================================

        // Contoh:
        //
        // Detection -> APD -> CL (CC001)
        // | FY2026, January, Week 3
        //
        // Label ini ditampilkan di atas masing-masing chart.
        public string ScopeLabel { get; set; } = string.Empty;


        // ========================================================
        // CHART LABEL
        // ========================================================

        // Contoh:
        // WK01
        // WK02
        // WK03
        // dst.
        public List<string> ChartLabels { get; set; } = new();


        // ========================================================
        // CHART 1 - WORKING HOURS TREND
        // ========================================================

        // Normal Hours.
        public List<decimal> ChartNormalHours { get; set; } = new();

        // Overtime Hours.
        //
        // OT = Work Hours - Normal Hours
        public List<decimal> ChartOtHours { get; set; } = new();

        // Total Quantity Output Final.
        public List<decimal> ChartQtyOut { get; set; } = new();


        // ========================================================
        // CHART 2 - LABOR PRODUCTIVITY TREND
        // ========================================================

        // Baseline Hour per Piece.
        public List<decimal?> ChartBaseline { get; set; } = new();

        // Aktual Hour per Piece.
        //
        // Aktual = Work Hours / QtyOutFinal
        public List<decimal?> ChartAktual { get; set; } = new();

        // Saving Hour per Piece.
        //
        // Saving = Baseline - Aktual
        public List<decimal?> ChartSaving { get; set; } = new();


        // ========================================================
        // INFORMASI ROLE
        // ========================================================

        // Role user yang sedang login.
        //
        // Contoh:
        // hr
        // ci
        // production
        public string CurrentRole { get; set; } = string.Empty;


        // ========================================================
        // PRODUCTION USER
        // ========================================================

        // Jika user adalah Production, user hanya boleh
        // melihat Data Master miliknya sendiri.
        public int? MyDataMasterId { get; set; }


        // ========================================================
        // HELPER UNTUK NAMA BULAN
        // ========================================================
        //
        // Digunakan oleh View ketika menampilkan:
        // January, February, March, dst.
        //
        // Dictionary ini sengaja dibuat di ViewModel supaya
        // Controller dan View menggunakan sumber yang sama.
        public Dictionary<int, string> MonthNames { get; set; } = new()
        {
            { 1, "January" },
            { 2, "February" },
            { 3, "March" },
            { 4, "April" },
            { 5, "May" },
            { 6, "June" },
            { 7, "July" },
            { 8, "August" },
            { 9, "September" },
            { 10, "October" },
            { 11, "November" },
            { 12, "December" }
        };
    }
}
