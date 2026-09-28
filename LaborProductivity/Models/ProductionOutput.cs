using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborProductivity.Models
{
    // Model ini mewakili tabel dbo.tbl_production_output
    [Table("tbl_production_output")]
    public class ProductionOutput
    {
        [Key]
        [Column("production_output_id")]
        public int ProductionOutputId { get; set; }

        // Tanggal akhir/finish production output
        // Di database menggunakan tipe DATE
        [Required]
        [Column("finish_date")]
        public DateOnly FinishDate { get; set; }

        // Nomor Fiscal Week
        // Bisa bernilai 1 sampai 52 atau 53
        [Required]
        [Column("week_no")]
        public int WeekNo { get; set; }

        // Foreign Key ke tbl_data_master
        [Required]
        [Column("data_master_id")]
        public int DataMasterId { get; set; }

        // Jumlah quality/input
        [Required]
        [Column("quality_input", TypeName = "decimal(10,2)")]
        public decimal QualityInput { get; set; }

        // Jumlah quality/output
        [Required]
        [Column("quality_output", TypeName = "decimal(10,2)")]
        public decimal QualityOutput { get; set; }

        // Relasi ke Data Master
        public DataMaster? DataMaster { get; set; }
    }
}