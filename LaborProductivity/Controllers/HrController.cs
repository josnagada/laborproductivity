using LaborProductivity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaborProductivity.Controllers
{
    // Hanya user dengan role hr yang boleh mengakses controller ini
    [Authorize(Roles = "hr")]
    public class HrController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HrController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Menampilkan halaman utama Dashboard HR
        public async Task<IActionResult> Index(int? year)
        {
            // Ambil daftar tahun yang tersedia dari data Work Hours
            var years = await _context.WorkHours
                .Select(w => w.FiscalYear)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            // Jika user belum memilih tahun,
            // gunakan tahun terbaru yang tersedia
            int selectedYear = year ?? years.FirstOrDefault();

            // Ambil data Work Hours untuk tahun yang dipilih
            var workHoursData = await _context.WorkHours
     .Where(w => w.FiscalYear == selectedYear)
     .Select(w => new
     {
         w.DataMasterId,
         ProductGroup = w.DataMaster != null
             ? w.DataMaster.ProductGroup
             : "Unknown",
         w.NormalHours,
         w.WorkHoursValue
     })
     .ToListAsync();

            // Hitung OT Hours
            // OT Hours = Work Hours - Normal Hours
            var otData = workHoursData
     .Select(w => new
     {
         w.DataMasterId,
         w.ProductGroup,
         OtHours = w.WorkHoursValue - w.NormalHours
     })
     .Where(w => w.OtHours > 0)
     .ToList();
            // Jumlahkan OT Hours berdasarkan Product Group
            var otByProductGroup = otData
                .GroupBy(w => w.ProductGroup)
                .Select(g => new
                {
                    ProductGroup = g.Key,
                    OtHours = g.Sum(x => x.OtHours)
                })
                .OrderByDescending(x => x.OtHours)
                .ToList();

            // Ambil Product Group dengan OT Hours tertinggi
            var highestOt = otByProductGroup.FirstOrDefault();

            // Ambil Product Group dengan OT Hours terendah
            var lowestOt = otByProductGroup
                .OrderBy(x => x.OtHours)
                .FirstOrDefault();

            ViewBag.Years = years;
            ViewBag.SelectedYear = selectedYear;

            ViewBag.OtByProductGroup = otByProductGroup;
            ViewBag.HighestOt = highestOt;
            ViewBag.LowestOt = lowestOt;

            return View();
        }
    }
}