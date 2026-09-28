using System;
using System.Collections.Generic;

namespace LaborProductivity.Models
{
    // ViewModel utama untuk halaman Production Output Report.
    public class ProductionReportViewModel
    {
        // =====================================================
        // Informasi scope user Production
        // =====================================================

        public string CostCenter { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public string ProductGroup { get; set; } = string.Empty;

        // =====================================================
        // Filter
        // =====================================================

        // Fiscal Year yang dipilih user.
        public int SelectedYear { get; set; }

        // Menyimpan satu Fiscal Week.
        // Properti ini tetap dipertahankan karena masih digunakan
        // oleh beberapa bagian controller/view yang sudah ada.
        public int? SelectedWeek { get; set; }

        // Menyimpan beberapa Fiscal Week yang dipilih user.
        // Digunakan untuk mendukung filter multi-week.
        public List<int> SelectedWeeks { get; set; } = new();

        // Daftar Fiscal Year untuk dropdown.
        public List<int> FiscalYears { get; set; } = new();

        // Daftar seluruh Fiscal Week untuk filter.
        public List<FiscalWeekReportItem> FiscalWeeks { get; set; } = new();

        // =====================================================
        // Data Report
        // =====================================================

        // Kolom minggu (header) yang benar-benar memiliki data.
        //
        // Dipakai bersama oleh setiap Product Line di bawah,
        // sehingga urutan kolom antar baris selalu sama.
        public List<FiscalWeekReportItem> ReportWeeks { get; set; } = new();

        // Satu baris per Product Line.
        //
        // Sebelumnya report ini menjumlahkan seluruh Product Line
        // menjadi satu baris CC. Sekarang setiap Product Line
        // ditampilkan terpisah supaya satu Product Group dengan
        // beberapa Product Line tetap terlihat sebagai baris
        // yang berbeda.
        public List<ProductLineReport> ProductLines { get; set; } = new();

        // =====================================================
        // Total Report
        // =====================================================

        // Total seluruh Qty Input
        // dari semua Product Line dan semua minggu.
        public decimal TotalQualityInput { get; set; }

        // Total seluruh Qty Output
        // dari semua Product Line dan semua minggu.
        public decimal TotalQualityOutput { get; set; }
    }

    // =========================================================
    // Informasi Fiscal Week
    // =========================================================

    // Menyimpan informasi Fiscal Week beserta tanggalnya.
    public class FiscalWeekReportItem
    {
        // Nomor Fiscal Week.
        public int WeekNo { get; set; }

        // Tanggal awal Fiscal Week.
        public DateOnly StartDate { get; set; }

        // Tanggal akhir Fiscal Week.
        public DateOnly EndDate { get; set; }
    }

    // =========================================================
    // Product Line Report
    // =========================================================

    // Data satu Product Line pada report.
    //
    // Berisi nilai per minggu dan total untuk Product Line tersebut.
    public class ProductLineReport
    {
        // Nama Product Line.
        public string ProductLine { get; set; } = string.Empty;

        // Data production untuk setiap Fiscal Week.
        //
        // Urutan list ini mengikuti urutan
        // ProductionReportViewModel.ReportWeeks.
        //
        // Jika suatu minggu tidak memiliki data untuk Product Line
        // tersebut, nilainya tetap dibuat 0 supaya posisi kolom
        // antar Product Line tetap sejajar.
        public List<ProductionReportWeek> Weeks { get; set; } = new();

        // Total Quality Input untuk Product Line.
        public decimal TotalQualityInput { get; set; }

        // Total Quality Output untuk Product Line.
        public decimal TotalQualityOutput { get; set; }
    }

    // =========================================================
    // Production Report per Week
    // =========================================================

    // Data Production Output untuk satu Product Line
    // pada satu Fiscal Week.
    public class ProductionReportWeek
    {
        // Nomor Fiscal Week.
        public int WeekNo { get; set; }

        // Tanggal awal Fiscal Week.
        public DateOnly StartDate { get; set; }

        // Tanggal akhir Fiscal Week.
        public DateOnly EndDate { get; set; }

        // Jumlah Quality Input.
        public decimal QualityInput { get; set; }

        // Jumlah Quality Output.
        public decimal QualityOutput { get; set; }
    }
}