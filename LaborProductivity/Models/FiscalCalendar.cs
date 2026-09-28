using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborProductivity.Models
{
    // Model ini mewakili tabel dbo.tbl_fiscal_calendar
    [Table("tbl_fiscal_calendar")]
    public class FiscalCalendar
    {
        [Key]
        [Column("fiscal_calendar_id")]
        public int FiscalCalendarId { get; set; }

        // Fiscal Year
        [Required]
        [Column("fiscal_year")]
        public int FiscalYear { get; set; }

        // Nomor Fiscal Week
        [Required]
        [Column("week_no")]
        public int WeekNo { get; set; }

        // Tanggal awal Fiscal Week
        [Required]
        [Column("start_date")]
        public DateOnly StartDate { get; set; }

        // Tanggal akhir Fiscal Week
        [Required]
        [Column("end_date")]
        public DateOnly EndDate { get; set; }
    }
}