using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborProductivity.Models
{
    // Model ini mewakili tabel dbo.tbl_baseline
    [Table("tbl_baseline")]
    public class Baseline
    {
        // Primary Key
        [Key]
        [Column("baseline_id")]
        public int BaselineId { get; set; }

        // Foreign Key ke tbl_data_master
        [Required]
        [Column("data_master_id")]
        public int DataMasterId { get; set; }

        // Tahun baseline
        [Required]
        [Column("year")]
        public int Year { get; set; }
        
        // Baseline jam kerja per piece
        // SQL Server: decimal(10,5)
        [Required]
        [Column("baseline_hour_per_piece", TypeName = "decimal(10,5)")]
        public decimal BaselineHourPerPiece { get; set; }

        // Relasi ke DataMaster
        public DataMaster? DataMaster { get; set; }
    }
}