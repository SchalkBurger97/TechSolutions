using System;
using System.Linq;
using System.Web.Mvc;
using TechSolutions.Models;

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
            // Get real data from database
            var customers = _context.Customers.Where(c => !c.IsDeleted).ToList();

            ViewBag.TotalCustomers = customers.Count;

            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            ViewBag.NewThisMonth = customers.Count(c => c.CreatedDate >= startOfMonth);

            var avgQuality = customers.Any() ? customers.Average(c => c.DataQualityScore) : 0;
            ViewBag.AverageQuality = Math.Round(avgQuality, 0);

            ViewBag.IssuesCount = customers.Count(c => c.DataQualityScore < 60);

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