using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TechSolutions.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.TotalCustomers = 1000;
            ViewBag.NewThisMonth = 50;
            ViewBag.AverageQuality = 85.5;
            ViewBag.IssuesCount = 0;

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
    }
}