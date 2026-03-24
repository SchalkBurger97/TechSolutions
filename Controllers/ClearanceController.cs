using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TechSolutions.Models;
using TechSolutions.Services;
using Microsoft.AspNet.Identity;

namespace TechSolutions.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class ClearanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _audit;

        public ClearanceController()
        {
            _context = new ApplicationDbContext();
            _audit = new AuditService(_context);
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
                ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                return View(model);
            }

            try
            {
                var customer = _context.Customers.Find(model.CustomerID);
                string entityDesc = customer != null
                    ? $"{customer.FullName} ({customer.CustomerCode})"
                    : $"CustomerID {model.CustomerID}";

                var existing = _context.MedicalClearances
                    .FirstOrDefault(m => m.CustomerID == model.CustomerID);

                if (existing == null)
                {
                    // ── Create ──
                    model.CreatedDate = DateTime.Now;
                    model.ModifiedDate = DateTime.Now;

                    if (model.ClearanceStatus == "Approved")
                        model.ApprovedBy = User.Identity.GetUserId();

                    _context.MedicalClearances.Add(model);
                    _context.SaveChanges();

                    _audit.Log(this, "Create", "Clearance", model.ClearanceID,
                        entityDesc,
                        $"Medical clearance created with status '{model.ClearanceStatus}'.");
                }
                else
                {
                    // ── Build field changes ──
                    var changes = new Dictionary<string, (string, string)>
                    {
                        ["Status"] = (existing.ClearanceStatus, model.ClearanceStatus),
                        ["Healthcare Facility"] = (existing.HealthcareFacility, model.HealthcareFacility),
                        ["Has TB Test"] = (existing.HasTBTest.ToString(), model.HasTBTest.ToString()),
                        ["TB Test Date"] = (existing.TBTestDate?.ToString("dd/MM/yyyy"), model.TBTestDate?.ToString("dd/MM/yyyy")),
                        ["TB Expiry Date"] = (existing.TBTestExpiryDate?.ToString("dd/MM/yyyy"), model.TBTestExpiryDate?.ToString("dd/MM/yyyy")),
                        ["Has COVID Vaccination"] = (existing.HasCOVIDVaccination.ToString(), model.HasCOVIDVaccination.ToString()),
                        ["COVID Status"] = (existing.CovidVaccinationStatus, model.CovidVaccinationStatus),
                        ["Has Background Check"] = (existing.HasBackgroundCheck.ToString(), model.HasBackgroundCheck.ToString()),
                        ["Background Check Status"] = (existing.BackgroundCheckStatus, model.BackgroundCheckStatus),
                        ["Cleared for Onsite"] = (existing.ClearedForOnsiteTraining.ToString(), model.ClearedForOnsiteTraining.ToString()),
                        ["Notes"] = (existing.ClearanceNotes, model.ClearanceNotes),
                    };

                    string fieldChanges = _audit.BuildFieldChanges(changes);

                    // ── Update ──
                    existing.HealthcareFacility = model.HealthcareFacility;
                    existing.ClearanceStatus = model.ClearanceStatus;
                    existing.HasTBTest = model.HasTBTest;
                    existing.TBTestDate = model.TBTestDate;
                    existing.TBTestResult = model.TBTestResult;
                    existing.TBTestExpiryDate = model.TBTestExpiryDate;
                    existing.HasCOVIDVaccination = model.HasCOVIDVaccination;
                    existing.CovidVaccinationStatus = model.CovidVaccinationStatus;
                    existing.LastVaccinationDate = model.LastVaccinationDate;
                    existing.VaccineBrand = model.VaccineBrand;
                    existing.HasBackgroundCheck = model.HasBackgroundCheck;
                    existing.BackgroundCheckStatus = model.BackgroundCheckStatus;
                    existing.BackgroundCheckDate = model.BackgroundCheckDate;
                    existing.ClearedForOnsiteTraining = model.ClearedForOnsiteTraining;
                    existing.ClearanceNotes = model.ClearanceNotes;
                    existing.ModifiedDate = DateTime.Now;

                    if (model.ClearanceStatus == "Approved" && existing.ClearanceStatus != "Approved")
                        existing.ApprovedBy = User.Identity.GetUserId();

                    _context.SaveChanges();

                    _audit.Log(this, "Update", "Clearance", existing.ClearanceID,
                        entityDesc,
                        $"Medical clearance updated. Status: '{model.ClearanceStatus}'.",
                        fieldChanges);
                }

                TempData["SuccessMessage"] = "Medical clearance updated successfully.";
                return RedirectToAction("Details", "Customer", new { id = model.CustomerID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error saving medical clearance: " + ex.Message;
                ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                return View(model);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _context.Dispose(); _audit.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
