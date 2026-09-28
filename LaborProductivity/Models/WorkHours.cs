using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborProductivity.Models
{
    [Table("tbl_work_hours")]
    public class WorkHours
    {
        [Key]
        [Column("work_hours_id")]
        public int WorkHoursId { get; set; }

        [Required]
        [Column("fiscal_year")]
        public int FiscalYear { get; set; }

        [Required]
        [Column("work_date")]
        public DateOnly WorkDate { get; set; }

        [Required]
        [Column("week_no")]
        public int WeekNo { get; set; }

        [Required]
        [Column("data_master_id")]
        public int DataMasterId { get; set; }

        [Required]
        [Column("normal_hours", TypeName = "decimal(10,2)")]
        public decimal NormalHours { get; set; }

        [Required]
        [Column("work_hours", TypeName = "decimal(10,2)")]
        public decimal WorkHoursValue { get; set; }

        // Relasi ke Data Master
        public DataMaster? DataMaster { get; set; }
    }
}