using System;
using System.Linq;
using System.Web.Mvc;
using TechSolutions.Models;
using TechSolutions.Services;

namespace TechSolutions.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController()
        {
            _context = new ApplicationDbContext();
        }

        public ActionResult Index()
        {
            var customers = _context.Customers.Where(c => !c.IsDeleted).ToList();
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var qualityReport = _context.Reports.FirstOrDefault(r => r.StoredProcedureName == "rpt_CustomerQuality" && r.IsActive);

            ViewBag.QualityReportId = qualityReport?.ReportID;
            // ── Existing stats ──
            ViewBag.TotalCustomers = customers.Count;
            ViewBag.NewThisMonth = customers.Count(c => c.CreatedDate >= startOfMonth);
            ViewBag.AverageQuality = customers.Any()
                ? Math.Round(customers.Average(c => c.DataQualityScore), 0)
                : 0;
            ViewBag.IssuesCount = customers.Count(c => c.DataQualityScore < 60);

            // ── Quality distribution ──
            ViewBag.HighQualityCount = customers.Count(c => c.DataQualityScore >= 80);
            ViewBag.MedQualityCount = customers.Count(c => c.DataQualityScore >= 60 && c.DataQualityScore < 80);
            ViewBag.LowQualityCount = customers.Count(c => c.DataQualityScore < 60);

            // ── Risk ──
            ViewBag.HighRiskCount = customers.Count(c => c.RiskScore >= 60);

            // ── Enrollments ──
            var enrollments = _context.EnrollmentHistories.ToList();
            ViewBag.ActiveEnrollments = enrollments.Count(e => e.EnrollmentStatus == "Active");
            ViewBag.CompletedThisMonth = enrollments.Count(e =>
                e.EnrollmentStatus == "Completed" &&
                e.CompletionDate.HasValue &&
                e.CompletionDate.Value >= startOfMonth);
            ViewBag.OutstandingBalance = enrollments
                .Where(e => e.EnrollmentStatus == "Active" || e.EnrollmentStatus == "Deferred")
                .Sum(e => e.CourseFee - e.AmountPaid);

            // ── Compliance: expired documents ──
            var allDocs = _context.IdentificationDocuments.ToList();
            ViewBag.ExpiredDocuments = allDocs.Count(d =>
                d.ExpiryDate.HasValue && d.ExpiryDate.Value < DateTime.Now);

            // ── Compliance: expired payment methods ──
            var allPayments = _context.PaymentInformation
                .Where(p => p.IsActive)
                .ToList();
            ViewBag.ExpiredPayments = allPayments.Count(p =>
                p.CardExpiryDate.HasValue && p.CardExpiryDate.Value < DateTime.Now);

            // ── Compliance: pending medical clearances ──
            var allClearances = _context.MedicalClearances.ToList();
            ViewBag.PendingClearances = allClearances.Count(c =>
                c.ClearanceStatus == "Pending" || c.ClearanceStatus == "In Review");

            // ── Recent customers (last 5) ──
            ViewBag.RecentCustomers = customers
                .OrderByDescending(c => c.CreatedDate)
                .Take(5)
                .ToList();

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}