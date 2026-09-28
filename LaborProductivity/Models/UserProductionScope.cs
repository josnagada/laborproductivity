using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborProductivity.Models
{
    // Model ini menyimpan batas akses data untuk user dengan role Production.
    // Satu user dapat memiliki satu atau lebih scope.
    [Table("tbl_user_production_scope")]
    public class UserProductionScope
    {
        // Primary Key tabel scope.
        [Key]
        [Column("scope_id")]
        public int ScopeId { get; set; }

        // ID user yang memiliki scope ini.
        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        // Department yang menjadi batas akses user.
        [Required]
        [Column("department_id")]
        public int DepartmentId { get; set; }

        // Cost Center yang menjadi batas akses user.
        [Required]
        [StringLength(50)]
        [Column("cost_center")]
        public string CostCenter { get; set; } = string.Empty;

        // Product Group yang menjadi batas akses user.
        [Required]
        [StringLength(100)]
        [Column("product_group")]
        public string ProductGroup { get; set; } = string.Empty;

        // Relasi ke tabel User.
        public User? User { get; set; }

        // Relasi ke tabel Department.
        public Department? Department { get; set; }
    }
}