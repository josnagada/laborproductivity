using System.ComponentModel.DataAnnotations;

namespace LaborProductivity.Models
{
    // Model untuk menampung data yang dikirim dari form login
    public class LoginViewModel
    {
        // Role yang dipilih user saat login
        [Required(ErrorMessage = "Role wajib dipilih.")]
        public string Role { get; set; } = string.Empty;

        // Email user
        [Required(ErrorMessage = "Email wajib diisi.")]
        [EmailAddress(ErrorMessage = "Format email tidak valid.")]
        public string Email { get; set; } = string.Empty;

        // Password user
        [Required(ErrorMessage = "Password wajib diisi.")]
        public string Password { get; set; } = string.Empty;

        // Menentukan apakah user ingin tetap login
        public bool RememberMe { get; set; }
    }
}