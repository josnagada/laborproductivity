using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborProductivity.Models
{
    // Model ini mewakili tabel dbo.tbl_settings
    [Table("tbl_settings")]
    public class Setting
    {
        [Key]
        [Column("setting_id")]
        public int SettingId { get; set; }

        // Nama aplikasi
        [Required]
        [StringLength(100)]
        [Column("app_name")]
        public string AppName { get; set; } = string.Empty;

        // Nama file logo
        [StringLength(255)]
        [Column("logo")]
        public string? Logo { get; set; }

        // Nama file favicon
        [StringLength(255)]
        [Column("favicon")]
        public string? Favicon { get; set; }

        // Nama file icon untuk halaman login
        [StringLength(255)]
        [Column("login_icon")]
        public string? LoginIcon { get; set; }

        // Warna utama aplikasi
        [StringLength(20)]
        [Column("color")]
        public string? Color { get; set; }

        // Teks yang ditampilkan pada footer
        [StringLength(500)]
        [Column("footer_text")]
        public string? FooterText { get; set; }

        //carousel
        [StringLength(255)]
        [Column("carousel_1")]
        public string? Carousel1 { get; set; }

        [StringLength(255)]
        [Column("carousel_2")]
        public string? Carousel2 { get; set; }

        [StringLength(255)]
        [Column("carousel_3")]
        public string? Carousel3 { get; set; }
    }
}