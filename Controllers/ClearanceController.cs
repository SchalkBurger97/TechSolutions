using System;
using System.Linq;
using System.Web.Mvc;
using TechSolutions.Models;

namespace TechSolutions.Controllers
{
    [Authorize]
    public class ClearanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClearanceController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: Clearance/Manage/5
        public ActionResult Manage(int customerId)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction("Index", "Customer");
            }

            // Get existing clearance or create new one
            var clearance = _context.MedicalClearances
                .FirstOrDefault(m => m.CustomerID == customerId);

            if (clearance == null)
            {
                clearance = new MedicalClearance
                {
                    CustomerID = customerId,
                    HasTBTest = false,
                    HasCOVIDVaccination = false,
                    HasBackgroundCheck = false,
                    ClearedForOnsiteTraining = false,
                    ClearanceStatus = "Pending"
                };
            }

            ViewBag.Customer = customer;
            return View(clearance);
        }

        // POST: Clearance/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Manage(MedicalClearance model)
        {
            if (!ModelState.IsValid)
            {
                var customer = _context.Customers.Find(model.CustomerID);
                ViewBag.Customer = customer;
                return View(model);
            }

            try
            {
                var existing = _context.MedicalClearances
                    .FirstOrDefault(m => m.CustomerID == model.CustomerID);

                if (existing == null)
                {
                    // Create new
                    model.CreatedDate = DateTime.Now;
                    model.ModifiedDate = DateTime.Now;
                    _context.MedicalClearances.Add(model);
                }
                else
                {
                    // Update existing - ALL FIELDS
                    existing.HealthcareFacility = model.HealthcareFacility;
                    existing.ClearanceStatus = model.ClearanceStatus;

                    // TB Test
                    existing.HasTBTest = model.HasTBTest;
                    existing.TBTestDate = model.TBTestDate;
                    existing.TBTestResult = model.TBTestResult;
                    existing.TBTestExpiryDate = model.TBTestExpiryDate;

                    // COVID Vaccination
                    existing.HasCOVIDVaccination = model.HasCOVIDVaccination;
                    existing.CovidVaccinationStatus = model.CovidVaccinationStatus;
                    existing.LastVaccinationDate = model.LastVaccinationDate;
                    existing.VaccineBrand = model.VaccineBrand;

                    // Background Check
                    existing.HasBackgroundCheck = model.HasBackgroundCheck;
                    existing.BackgroundCheckStatus = model.BackgroundCheckStatus;
                    existing.BackgroundCheckDate = model.BackgroundCheckDate;

                    // Overall
                    existing.ClearedForOnsiteTraining = model.ClearedForOnsiteTraining;
                    existing.ClearanceNotes = model.ClearanceNotes;
                    existing.ModifiedDate = DateTime.Now;
                }

                _context.SaveChanges();

                TempData["SuccessMessage"] = "Medical clearance updated successfully.";
                return RedirectToAction("Details", "Customer", new { id = model.CustomerID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error saving medical clearance: " + ex.Message;
                var customer = _context.Customers.Find(model.CustomerID);
                ViewBag.Customer = customer;
                return View(model);
            }
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