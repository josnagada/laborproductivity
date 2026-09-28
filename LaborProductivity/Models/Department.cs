using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborProductivity.Models
{
    // Model ini mewakili tabel dbo.tbl_department
    [Table("tbl_department")]
    public class Department
    {
        // Primary Key
        [Key]
        [Column("department_id")]
        public int DepartmentId { get; set; }

        // Nama department, misalnya:
        // Detection, Lighting, Optics
        [Required(ErrorMessage = "Department Name wajib diisi.")]
        [StringLength(100)]
        [Column("department_name")]
        public string DepartmentName { get; set; } = string.Empty;
    }
}
