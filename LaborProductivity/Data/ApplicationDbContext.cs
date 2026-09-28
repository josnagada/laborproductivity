using LaborProductivity.Models;
using Microsoft.EntityFrameworkCore;

namespace LaborProductivity.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Database Labor Productivity

        // tabel dbo.User
        public DbSet<User> Users { get; set; }

        // tabel dbo.tbl_department
        public DbSet<Department> Departments { get; set; }

        // tabel dbo.tbl_data_master
        public DbSet<DataMaster> DataMasters { get; set; }

        // tabel dbo.tbl_baseline
        public DbSet<Baseline> Baselines { get; set; }

        // tabel dbo.tbl_fiscal_calendar
        public DbSet<FiscalCalendar> FiscalCalendars { get; set; }

        // tabel dbo.tbl_production_output
        public DbSet<ProductionOutput> ProductionOutputs { get; set; }

        // tabel work_hours_hr
        public DbSet<WorkHours> WorkHours { get; set; }

        // Hubungkan model user production dengan rolenya
        public DbSet<UserProductionScope> UserProductionScopes { get; set; }

        // tabel tbl_Setting
        public DbSet<Setting> Settings { get; set; }

        //tabel tbl_Slider
        public DbSet<Slider> Sliders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // KONFIGURASI BASELINE
           

            modelBuilder.Entity<Baseline>()
                .ToTable("tbl_baseline");

            modelBuilder.Entity<Baseline>()
                .HasKey(b => b.BaselineId);

            // UNIQUE BASELINE
            // DATA MASTER + YEAR
            // Satu Data Master hanya boleh memiliki
            // satu Baseline untuk setiap tahun.

            modelBuilder.Entity<Baseline>()
                .HasIndex(b => new
                {
                    b.DataMasterId,
                    b.Year
                })
                .IsUnique();


            // =====================================================
            // RELASI BASELINE -> DATA MASTER
            // =====================================================

            modelBuilder.Entity<Baseline>()
                .HasOne(b => b.DataMaster)
                .WithMany()
                .HasForeignKey(b => b.DataMasterId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // KONFIGURASI FISCAL CALENDAR
            // =====================================================

            modelBuilder.Entity<FiscalCalendar>()
                .ToTable("tbl_fiscal_calendar");

            modelBuilder.Entity<FiscalCalendar>()
                .HasKey(fc => fc.FiscalCalendarId);


            // =====================================================
            // UNIQUE FISCAL YEAR + WEEK
            // =====================================================

            // Satu Fiscal Year tidak boleh memiliki
            // Fiscal Week yang sama lebih dari satu kali.

            modelBuilder.Entity<FiscalCalendar>()
                .HasIndex(fc => new
                {
                    fc.FiscalYear,
                    fc.WeekNo
                })
                .IsUnique();


            // =====================================================
            // KONFIGURASI PRODUCTION OUTPUT
            // =====================================================

            modelBuilder.Entity<ProductionOutput>()
                .ToTable("tbl_production_output");

            modelBuilder.Entity<ProductionOutput>()
                .HasKey(p => p.ProductionOutputId);


            // =====================================================
            // UNIQUE PRODUCTION OUTPUT
            // DATA MASTER + WEEK + FINISH DATE
            // =====================================================

            // Satu Data Master tidak boleh memiliki
            // dua Production Output dengan kombinasi
            // Week + Finish Date yang sama.

            modelBuilder.Entity<ProductionOutput>()
                .HasIndex(p => new
                {
                    p.DataMasterId,
                    p.WeekNo,
                    p.FinishDate
                })
                .IsUnique();


            // =====================================================
            // RELASI PRODUCTION OUTPUT -> DATA MASTER
            // =====================================================

            modelBuilder.Entity<ProductionOutput>()
                .HasOne(p => p.DataMaster)
                .WithMany()
                .HasForeignKey(p => p.DataMasterId)
                .OnDelete(DeleteBehavior.Restrict);

            //konfigurasi workhours
            modelBuilder.Entity<WorkHours>()
        .ToTable("tbl_work_hours");

            modelBuilder.Entity<WorkHours>()
        .HasKey(w => w.WorkHoursId);

            modelBuilder.Entity<WorkHours>()
        .HasOne(w => w.DataMaster)
        .WithMany()
        .HasForeignKey(w => w.DataMasterId)
        .OnDelete(DeleteBehavior.Restrict);
        }

    }
}