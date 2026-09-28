using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborProductivity.Models
{
    // Model User mewakili tabel dbo.User di database
    [Table("User")]
    public class User
    {
        // ID User, dibuat otomatis oleh SQL Server
        [Key]
        public int Id { get; set; }

        // Alamat email user
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        // Password user yang akan disimpan dalam bentuk hash
        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;

        // Nama/path foto user, boleh kosong
        [MaxLength(255)]
        public string? Photo { get; set; }

        // Role user: admin, hr, ci, production
        [Required]
        [MaxLength(30)]
        public string Role { get; set; } = string.Empty;

        // Hubungan dengan DataMaster, boleh kosong
        public int? DataMasterId { get; set; }

        // Status user
        [Required]
        [MaxLength(10)]
        public string Status { get; set; } = string.Empty;
    }
}