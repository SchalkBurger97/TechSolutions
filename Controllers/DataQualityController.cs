using System;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using TechSolutions.Models;

namespace TechSolutions.Controllers
{
    [Authorize]
    public class DataQualityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DataQualityController()
        {
            _context = new ApplicationDbContext();
        }

        public ActionResult Index()
        {
            var customers = _context.Customers
                .Where(c => !c.IsDeleted)
                .ToList();

            if (!customers.Any())
            {
                ViewBag.HasData = false;
                return View();
            }

            ViewBag.HasData = true;
            int total = customers.Count;

            // ── Overview KPIs (all simple primitives) ──
            ViewBag.TotalCustomers = total;
            ViewBag.AvgQuality = Math.Round(customers.Average(c => (double)c.DataQualityScore), 1);
            ViewBag.AvgRisk = Math.Round(customers.Average(c => (double)c.RiskScore), 1);
            ViewBag.FullyComplete = customers.Count(c => c.DataQualityScore == 100);
            ViewBag.CriticalCount = customers.Count(c => c.DataQualityScore < 40);
            ViewBag.HighQualityCount = customers.Count(c => c.DataQualityScore >= 80);
            ViewBag.MedQualityCount = customers.Count(c => c.DataQualityScore >= 60 && c.DataQualityScore < 80);
            ViewBag.LowQualityCount = customers.Count(c => c.DataQualityScore < 60);
            ViewBag.RiskLow = customers.Count(c => c.RiskScore < 30);
            ViewBag.RiskMed = customers.Count(c => c.RiskScore >= 30 && c.RiskScore < 60);
            ViewBag.RiskHigh = customers.Count(c => c.RiskScore >= 60);
            ViewBag.ActiveCount = customers.Count(c => c.IsActive);
            ViewBag.InactiveCount = customers.Count(c => !c.IsActive);

            // ── Documents ──
            var docs = _context.IdentificationDocuments.ToList();
            int custWithDocs = docs.Select(d => d.CustomerID).Distinct().Count();
            ViewBag.DocCoverage = total > 0 ? Math.Round(custWithDocs * 100.0 / total, 1) : 0.0;
            ViewBag.DocsVerified = docs.Count(d => d.IsVerified);
            ViewBag.DocsExpired = docs.Count(d => d.ExpiryDate.HasValue && d.ExpiryDate.Value < DateTime.Now);
            ViewBag.DocsExpiringSoon = docs.Count(d => d.ExpiryDate.HasValue && d.ExpiryDate.Value >= DateTime.Now && d.ExpiryDate.Value < DateTime.Now.AddMonths(3));

            // ── Clearances ──
            var clearances = _context.MedicalClearances.ToList();
            int custWithClear = clearances.Select(c => c.CustomerID).Distinct().Count();
            ViewBag.ClearCoverage = total > 0 ? Math.Round(custWithClear * 100.0 / total, 1) : 0.0;
            ViewBag.ClearApproved = clearances.Count(c => c.ClearanceStatus == "Approved" || c.ClearedForOnsiteTraining);
            ViewBag.ClearPending = clearances.Count(c => c.ClearanceStatus == "Pending" || c.ClearanceStatus == "In Review");
            ViewBag.ClearRejected = clearances.Count(c => c.ClearanceStatus == "Rejected");

            // ── Payments ──
            var payments = _context.PaymentInformation.Where(p => p.IsActive).ToList();
            int custWithPay = payments.Select(p => p.CustomerID).Distinct().Count();
            ViewBag.PayCoverage = total > 0 ? Math.Round(custWithPay * 100.0 / total, 1) : 0.0;
            ViewBag.PayExpired = payments.Count(p => p.CardExpiryDate.HasValue && p.CardExpiryDate.Value < DateTime.Now);
            ViewBag.PayExpiringSoon = payments.Count(p => p.CardExpiryDate.HasValue && p.CardExpiryDate.Value >= DateTime.Now && p.CardExpiryDate.Value < DateTime.Now.AddMonths(3));

            // ── Enrollments ──
            var enrollments = _context.EnrollmentHistories.ToList();
            decimal totalFees = enrollments.Sum(e => e.CourseFee);
            decimal totalPaid = enrollments.Sum(e => e.AmountPaid);
            ViewBag.EnrolActive = enrollments.Count(e => e.EnrollmentStatus == "Active");
            ViewBag.EnrolCompleted = enrollments.Count(e => e.EnrollmentStatus == "Completed");
            ViewBag.EnrolWithdrawn = enrollments.Count(e => e.EnrollmentStatus == "Withdrawn");
            ViewBag.EnrolTotalFees = totalFees;
            ViewBag.EnrolTotalPaid = totalPaid;
            ViewBag.EnrolOutstanding = totalFees - totalPaid;
            ViewBag.EnrolPaidPct = totalFees > 0 ? Math.Round((double)totalPaid / (double)totalFees * 100, 1) : 0.0;

            ViewBag.LastRefreshed = DateTime.Now.ToString("HH:mm:ss");

            // All chart data pre-built as JSON strings
            // so the view never touches complex types

            // ── Score histogram ──
            var hLabels = new StringBuilder("[");
            var hData = new StringBuilder("[");
            for (int i = 0; i <= 9; i++)
            {
                int low = i * 10, high = low + 10;
                int cnt = customers.Count(c =>
                    c.DataQualityScore >= low &&
                    (i == 9 ? c.DataQualityScore <= 100 : c.DataQualityScore < high));
                if (i > 0) { hLabels.Append(","); hData.Append(","); }
                hLabels.Append("\"").Append(low).Append("-").Append(high).Append("%\"");
                hData.Append(cnt);
            }
            hLabels.Append("]"); hData.Append("]");
            ViewBag.HistogramLabels = hLabels.ToString();
            ViewBag.HistogramData = hData.ToString();

            // ── Quality by customer type ──
            var typeGroups = customers
                .GroupBy(c => string.IsNullOrWhiteSpace(c.CustomerType) ? "Unknown" : c.CustomerType)
                .Select(g => new { Type = g.Key, Avg = Math.Round(g.Average(c => (double)c.DataQualityScore), 1) })
                .OrderByDescending(x => x.Avg)
                .ToList();

            var tLab = new StringBuilder("[");
            var tDat = new StringBuilder("[");
            for (int i = 0; i < typeGroups.Count; i++)
            {
                if (i > 0) { tLab.Append(","); tDat.Append(","); }
                tLab.Append("\"").Append(typeGroups[i].Type.Replace("\"", "\\\"")).Append("\"");
                tDat.Append(typeGroups[i].Avg.ToString("F1"));
            }
            tLab.Append("]"); tDat.Append("]");
            ViewBag.TypeLabels = tLab.ToString();
            ViewBag.TypeData = tDat.ToString();

            // ── Field completion ──
            double[] pcts = new double[]
            {
                Math.Round(customers.Count(c => !string.IsNullOrWhiteSpace(c.FirstName))  * 100.0 / total, 1),
                Math.Round(customers.Count(c => !string.IsNullOrWhiteSpace(c.LastName))   * 100.0 / total, 1),
                Math.Round(customers.Count(c => !string.IsNullOrWhiteSpace(c.Email))      * 100.0 / total, 1),
                Math.Round(customers.Count(c => !string.IsNullOrWhiteSpace(c.Phone))      * 100.0 / total, 1),
                Math.Round(customers.Count(c => c.DateOfBirth.HasValue)                   * 100.0 / total, 1),
                Math.Round(customers.Count(c => !string.IsNullOrWhiteSpace(c.Address))    * 100.0 / total, 1),
                Math.Round(customers.Count(c => !string.IsNullOrWhiteSpace(c.City))       * 100.0 / total, 1),
                Math.Round(customers.Count(c => !string.IsNullOrWhiteSpace(c.Province))   * 100.0 / total, 1),
                Math.Round(customers.Count(c => !string.IsNullOrWhiteSpace(c.PostalCode)) * 100.0 / total, 1),
            };
            string[] fieldNames = { "First Name", "Last Name", "Email", "Phone", "Date of Birth", "Address", "City", "Province", "Postal Code" };

            var fLab = new StringBuilder("[");
            var fDat = new StringBuilder("[");
            var fCol = new StringBuilder("[");
            for (int i = 0; i < fieldNames.Length; i++)
            {
                if (i > 0) { fLab.Append(","); fDat.Append(","); fCol.Append(","); }
                string col = pcts[i] >= 80 ? "\"rgba(46,158,107,0.75)\"" :
                             pcts[i] >= 60 ? "\"rgba(212,142,16,0.75)\"" :
                                             "\"rgba(192,57,43,0.75)\"";
                fLab.Append("\"").Append(fieldNames[i]).Append("\"");
                fDat.Append(pcts[i].ToString("F1"));
                fCol.Append(col);
            }
            fLab.Append("]"); fDat.Append("]"); fCol.Append("]");
            ViewBag.FieldLabels = fLab.ToString();
            ViewBag.FieldData = fDat.ToString();
            ViewBag.FieldColors = fCol.ToString();

            // ── Quality trend (last 6 months) ──
            var trLab = new StringBuilder("[");
            var trScr = new StringBuilder("[");
            var trCnt = new StringBuilder("[");
            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                var monthStart = new DateTime(month.Year, month.Month, 1);
                var monthEnd = monthStart.AddMonths(1);
                var mc = customers.Where(c => c.CreatedDate >= monthStart && c.CreatedDate < monthEnd).ToList();
                if (i < 5) { trLab.Append(","); trScr.Append(","); trCnt.Append(","); }
                trLab.Append("\"").Append(monthStart.ToString("MMM yy")).Append("\"");
                trScr.Append(mc.Any() ? Math.Round(mc.Average(c => (double)c.DataQualityScore), 1).ToString("F1") : "null");
                trCnt.Append(mc.Count);
            }
            trLab.Append("]"); trScr.Append("]"); trCnt.Append("]");
            ViewBag.TrendLabels = trLab.ToString();
            ViewBag.TrendScores = trScr.ToString();
            ViewBag.TrendCounts = trCnt.ToString();

            return View();
        }

        [HttpGet]
        public JsonResult LiveStats()
        {
            var customers = _context.Customers.Where(c => !c.IsDeleted).ToList();
            return Json(new
            {
                totalCustomers = customers.Count,
                avgQuality = customers.Any() ? Math.Round(customers.Average(c => (double)c.DataQualityScore), 1) : 0.0,
                newToday = customers.Count(c => c.CreatedDate >= DateTime.Today),
                criticalCount = customers.Count(c => c.DataQualityScore < 40),
                pendingClear = _context.MedicalClearances.Count(c => c.ClearanceStatus == "Pending" || c.ClearanceStatus == "In Review"),
                expiredDocs = _context.IdentificationDocuments.Count(d => d.ExpiryDate.HasValue && d.ExpiryDate.Value < DateTime.Now),
                lastRefreshed = DateTime.Now.ToString("HH:mm:ss")
            }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}
