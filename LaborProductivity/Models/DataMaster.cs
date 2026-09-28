using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborProductivity.Models
{
    // Model ini mewakili tabel dbo.tbl_data_master
    [Table("tbl_data_master")]
    public class DataMaster
    {
        // Primary Key
        [Key]
        [Column("data_master_id")]
        public int DataMasterId { get; set; }

        // Cost Center
        [Required(ErrorMessage = "Cost Center wajib diisi.")]
        [StringLength(50)]
        [Column("cost_center")]
        public string CostCenter { get; set; } = string.Empty;

        // ID Department yang terhubung dengan tbl_department
        [Range(1, int.MaxValue, ErrorMessage = "Department wajib dipilih.")]
        [Column("department_id")]
        public int DepartmentId { get; set; }

        // Product Group
        [Required(ErrorMessage = "Product Group wajib diisi.")]
        [StringLength(100)]
        [Column("product_group")]
        public string ProductGroup { get; set; } = string.Empty;

        // Product Line
        [Required(ErrorMessage = "Product Line wajib diisi.")]
        [StringLength(100)]
        [Column("product_line")]
        public string ProductLine { get; set; } = string.Empty;

        // Relasi ke tabel tbl_department
        // Digunakan untuk mendapatkan nama department,
        // misalnya Detection, Lighting, atau Optics.
        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }
    }
}
