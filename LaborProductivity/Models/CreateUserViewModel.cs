using System.ComponentModel.DataAnnotations;

namespace LaborProductivity.Models
{
    // ViewModel yang digunakan khusus untuk form tambah user.
    public class CreateUserViewModel
    {
        // Email user baru.
        [Required(ErrorMessage = "Email wajib diisi.")]
        [EmailAddress(ErrorMessage = "Format email tidak valid.")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        // Password user baru.
        [Required(ErrorMessage = "Password wajib diisi.")]
        [MinLength(6, ErrorMessage = "Password minimal 6 karakter.")]
        public string Password { get; set; } = string.Empty;

        // Role user baru.
        [Required(ErrorMessage = "Role wajib dipilih.")]
        public string Role { get; set; } = string.Empty;

        // Department yang menjadi scope user Production.
        public int? DepartmentId { get; set; }

        // Cost Center yang menjadi scope user Production.
        public string? CostCenter { get; set; }

        // Product Group yang menjadi scope user Production.
        public string? ProductGroup { get; set; }

        // Daftar Department untuk dropdown.
        public List<Department> Departments { get; set; } = new();

        // Daftar pilihan CC / Product Group.
        public List<DataMaster> DataMasters { get; set; } = new();

        // Status user baru.
        [Required(ErrorMessage = "Status wajib dipilih.")]
        public string Status { get; set; } = "active";
    }
}