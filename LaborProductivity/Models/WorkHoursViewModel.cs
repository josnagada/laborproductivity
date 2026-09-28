using System.ComponentModel.DataAnnotations;

namespace LaborProductivity.Models
{
    public class WorkHoursViewModel
    {
        // =====================================================
        // DATA INPUT WORK HOURS
        // =====================================================

        [Required(ErrorMessage = "Fiscal Year wajib dipilih.")]
        [Display(Name = "Fiscal Year")]
        public int FiscalYear { get; set; }

        [Required(ErrorMessage = "Fiscal Week wajib dipilih.")]
        [Display(Name = "Fiscal Week")]
        public int WeekNo { get; set; }

        [Required(ErrorMessage = "Data Master wajib dipilih.")]
        [Display(Name = "CC - Product Group")]
        public int DataMasterId { get; set; }

        [Required(ErrorMessage = "Normal Hours wajib diisi.")]
        [Range(0, 999999, ErrorMessage = "Normal Hours tidak valid.")]
        [Display(Name = "Normal Hours")]
        public decimal NormalHours { get; set; }

        [Required(ErrorMessage = "Work Hours wajib diisi.")]
        [Range(0, 999999, ErrorMessage = "Work Hours tidak valid.")]
        [Display(Name = "Work Hours")]
        public decimal WorkHoursValue { get; set; }


        // =====================================================
        // DATA HASIL DARI FISCAL CALENDAR
        // =====================================================

        [Display(Name = "Work Date")]
        public DateOnly? WorkDate { get; set; }


        // =====================================================
        // DATA UNTUK DROPDOWN
        // =====================================================

        public List<int> FiscalYears { get; set; } = new();

        public List<int> FiscalWeeks { get; set; } = new();


        // Daftar Data Master yang akan ditampilkan
        // berdasarkan Department yang dipilih.
        public List<DataMaster> DataMasters { get; set; } = new();


        // Daftar Department
        public List<Department> Departments { get; set; } = new();

        // Data Work Hours yang sudah tersimpan
        public List<WorkHours> ExistingWorkHours { get; set; } = new();
    }
}