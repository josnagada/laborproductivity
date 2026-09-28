using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborProductivity.Models
{
    // Model ini mewakili tabel dbo.tbl_slider
    [Table("tbl_slider")]
    public class Slider
    {
        // ID utama slider
        [Key]
        [Column("slider_id")]
        public int SliderId { get; set; }

        // Nama file gambar slider
        [StringLength(255)]
        [Column("photo")]
        public string? Photo { get; set; }

        // Judul yang ditampilkan pada slider
        [StringLength(255)]
        [Column("heading")]
        public string? Heading { get; set; }

        // Isi atau deskripsi slider
        [Column("content")]
        public string? Content { get; set; }

        // Tulisan pada tombol
        [StringLength(100)]
        [Column("button_text")]
        public string? ButtonText { get; set; }

        // URL tujuan ketika tombol diklik
        [StringLength(500)]
        [Column("button_url")]
        public string? ButtonUrl { get; set; }

        // Posisi teks: Left, Center, atau Right
        [Required]
        [StringLength(20)]
        [Column("position")]
        public string Position { get; set; } = "Center";

        // Status slider: Active atau Inactive
        [Required]
        [StringLength(20)]
        [Column("status")]
        public string Status { get; set; } = "Active";

        // Waktu ketika slider dibuat
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}