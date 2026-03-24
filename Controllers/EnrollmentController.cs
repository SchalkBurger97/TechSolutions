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
    public class EnrollmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _audit;

        public EnrollmentController()
        {
            _context = new ApplicationDbContext();
            _audit = new AuditService(_context);
        }

        // GET: Enrollment/Index?customerId=5
        public ActionResult Index(int customerId)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction("Index", "Customer");
            }

            var enrollments = _context.EnrollmentHistories
                .Where(e => e.CustomerID == customerId)
                .OrderByDescending(e => e.EnrollmentDate)
                .ToList();

            ViewBag.Customer = customer;
            ViewBag.TotalEnrollments = enrollments.Count;
            ViewBag.ActiveEnrollments = enrollments.Count(e => e.EnrollmentStatus == "Active");
            ViewBag.CompletedEnrollments = enrollments.Count(e => e.EnrollmentStatus == "Completed");
            ViewBag.TotalRevenue = enrollments.Sum(e => e.AmountPaid);
            ViewBag.OutstandingBalance = enrollments.Sum(e => e.CourseFee - e.AmountPaid);

            return View(enrollments);
        }

        // GET: Enrollment/Details/5
        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return RedirectToAction("Index", "Customer");

            var enrollment = _context.EnrollmentHistories.Find(id.Value);
            if (enrollment == null)
            {
                TempData["ErrorMessage"] = "Enrollment not found.";
                return RedirectToAction("Index", "Customer");
            }

            ViewBag.Customer = _context.Customers.Find(enrollment.CustomerID);
            return View(enrollment);
        }

        // GET: Enrollment/Create?customerId=5
        public ActionResult Create(int customerId)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction("Index", "Customer");
            }

            ViewBag.Customer = customer;
            ViewBag.PaymentStatuses = GetPaymentStatuses();
            ViewBag.EnrollmentStatuses = GetEnrollmentStatuses();
            return View(new EnrollmentHistory
            {
                CustomerID = customerId,
                EnrollmentDate = DateTime.Now,
                PaymentStatus = "Pending",
                EnrollmentStatus = "Active"
            });
        }

        // POST: Enrollment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EnrollmentHistory model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                ViewBag.PaymentStatuses = GetPaymentStatuses();
                ViewBag.EnrollmentStatuses = GetEnrollmentStatuses();
                return View(model);
            }

            try
            {
                _context.EnrollmentHistories.Add(model);
                _context.SaveChanges();

                var customer = _context.Customers.Find(model.CustomerID);
                string entityDesc = customer != null
                    ? $"{customer.FullName} ({customer.CustomerCode})"
                    : $"CustomerID {model.CustomerID}";

                _audit.Log(this, "Create", "Enrollment", model.EnrollmentID,
                    entityDesc,
                    $"Enrolled in '{model.CourseName}' ({model.CourseCode}). Fee: R{model.CourseFee:N2}.");

                TempData["SuccessMessage"] = "Enrollment created successfully.";
                return RedirectToAction("Index", new { customerId = model.CustomerID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error creating enrollment: " + ex.Message;
                ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                ViewBag.PaymentStatuses = GetPaymentStatuses();
                ViewBag.EnrollmentStatuses = GetEnrollmentStatuses();
                return View(model);
            }
        }

        // GET: Enrollment/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue) return RedirectToAction("Index", "Customer");

            var enrollment = _context.EnrollmentHistories.Find(id.Value);
            if (enrollment == null)
            {
                TempData["ErrorMessage"] = "Enrollment not found.";
                return RedirectToAction("Index", "Customer");
            }

            ViewBag.Customer = _context.Customers.Find(enrollment.CustomerID);
            ViewBag.PaymentStatuses = GetPaymentStatuses();
            ViewBag.EnrollmentStatuses = GetEnrollmentStatuses();
            return View(enrollment);
        }

        // POST: Enrollment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EnrollmentHistory model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                ViewBag.PaymentStatuses = GetPaymentStatuses();
                ViewBag.EnrollmentStatuses = GetEnrollmentStatuses();
                return View(model);
            }

            try
            {
                var existing = _context.EnrollmentHistories.Find(model.EnrollmentID);
                if (existing == null)
                {
                    TempData["ErrorMessage"] = "Enrollment not found.";
                    return RedirectToAction("Index", new { customerId = model.CustomerID });
                }

                var customer = _context.Customers.Find(model.CustomerID);
                string entityDesc = customer != null
                    ? $"{customer.FullName} ({customer.CustomerCode})"
                    : $"CustomerID {model.CustomerID}";

                // ── Build field changes ──
                var changes = new Dictionary<string, (string, string)>
                {
                    ["Course Name"] = (existing.CourseName, model.CourseName),
                    ["Course Code"] = (existing.CourseCode, model.CourseCode),
                    ["Enrollment Status"] = (existing.EnrollmentStatus, model.EnrollmentStatus),
                    ["Payment Status"] = (existing.PaymentStatus, model.PaymentStatus),
                    ["Course Fee"] = (existing.CourseFee.ToString("N2"), model.CourseFee.ToString("N2")),
                    ["Amount Paid"] = (existing.AmountPaid.ToString("N2"), model.AmountPaid.ToString("N2")),
                    ["Start Date"] = (existing.CourseStartDate?.ToString("dd/MM/yyyy"), model.CourseStartDate?.ToString("dd/MM/yyyy")),
                    ["End Date"] = (existing.CourseEndDate?.ToString("dd/MM/yyyy"), model.CourseEndDate?.ToString("dd/MM/yyyy")),
                    ["Completion Date"] = (existing.CompletionDate?.ToString("dd/MM/yyyy"), model.CompletionDate?.ToString("dd/MM/yyyy")),
                    ["Grade"] = (existing.Grade, model.Grade),
                    ["Certificate Issued"] = (existing.CertificateIssued.ToString(), model.CertificateIssued.ToString()),
                    ["Notes"] = (existing.Notes, model.Notes),
                };
                string fieldChanges = _audit.BuildFieldChanges(changes);

                // ── Update fields ──
                existing.CourseName = model.CourseName;
                existing.CourseCode = model.CourseCode;
                existing.EnrollmentDate = model.EnrollmentDate;
                existing.CourseStartDate = model.CourseStartDate;
                existing.CourseEndDate = model.CourseEndDate;
                existing.CourseFee = model.CourseFee;
                existing.AmountPaid = model.AmountPaid;
                existing.PaymentStatus = model.PaymentStatus;
                existing.EnrollmentStatus = model.EnrollmentStatus;
                existing.CompletionDate = model.CompletionDate;
                existing.Grade = model.Grade;
                existing.CertificateIssued = model.CertificateIssued;
                existing.CertificateIssueDate = model.CertificateIssueDate;
                existing.Notes = model.Notes;

                _context.SaveChanges();

                _audit.Log(this, "Update", "Enrollment", existing.EnrollmentID,
                    entityDesc,
                    $"Updated enrollment in '{existing.CourseName}'. Status: {existing.EnrollmentStatus}.",
                    fieldChanges);

                TempData["SuccessMessage"] = "Enrollment updated successfully.";
                return RedirectToAction("Index", new { customerId = model.CustomerID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating enrollment: " + ex.Message;
                ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                ViewBag.PaymentStatuses = GetPaymentStatuses();
                ViewBag.EnrollmentStatuses = GetEnrollmentStatuses();
                return View(model);
            }
        }

        // POST: Enrollment/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id, int customerId)
        {
            try
            {
                var enrollment = _context.EnrollmentHistories.Find(id);
                if (enrollment != null)
                {
                    var customer = _context.Customers.Find(customerId);
                    string entityDesc = customer != null
                        ? $"{customer.FullName} ({customer.CustomerCode})"
                        : $"CustomerID {customerId}";

                    _audit.Log(this, "Delete", "Enrollment", id,
                        entityDesc,
                        $"Deleted enrollment in '{enrollment.CourseName}' ({enrollment.EnrollmentStatus}).");

                    _context.EnrollmentHistories.Remove(enrollment);
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Enrollment deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Enrollment not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting enrollment: " + ex.Message;
            }

            return RedirectToAction("Index", new { customerId });
        }

        private SelectList GetPaymentStatuses() => new SelectList(new[] { "Pending", "Paid", "Partial", "Refunded" });
        private SelectList GetEnrollmentStatuses() => new SelectList(new[] { "Active", "Completed", "Withdrawn", "Deferred" });

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _context.Dispose(); _audit.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
