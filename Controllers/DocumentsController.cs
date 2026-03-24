using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TechSolutions.Helpers;
using TechSolutions.Models;
using TechSolutions.Services;
using Microsoft.AspNet.Identity;

namespace TechSolutions.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class DocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _audit;

        public DocumentsController()
        {
            _context = new ApplicationDbContext();
            _audit = new AuditService(_context);
        }

        // GET: Documents/Index?customerId=5
        public ActionResult Index(int customerId)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction("Index", "Customer");
            }

            var documents = _context.IdentificationDocuments
                .Where(d => d.CustomerID == customerId)
                .OrderByDescending(d => d.CreatedDate)
                .ToList();

            ViewBag.Customer = customer;
            return View(documents);
        }

        // GET: Documents/Create?customerId=5
        public ActionResult Create(int customerId)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction("Index", "Customer");
            }

            ViewBag.Customer = customer;
            ViewBag.DocumentTypes = GetDocumentTypes();
            return View(new IdentificationDocument { CustomerID = customerId });
        }

        // POST: Documents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IdentificationDocument model, HttpPostedFileBase documentFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                ViewBag.DocumentTypes = GetDocumentTypes();
                return View(model);
            }

            try
            {
                // ── File upload (encrypted at rest)
                if (documentFile != null && documentFile.ContentLength > 0)
                {
                    if (documentFile.ContentLength > 5 * 1024 * 1024)
                    {
                        TempData["ErrorMessage"] = "File size must be less than 5MB.";
                        ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                        ViewBag.DocumentTypes = GetDocumentTypes();
                        return View(model);
                    }

                    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(documentFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["ErrorMessage"] = "Only PDF, JPG, and PNG files are allowed.";
                        ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                        ViewBag.DocumentTypes = GetDocumentTypes();
                        return View(model);
                    }

                    var uploadsPath = Server.MapPath("~/App_Data/DocumentUploads");
                    if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

                    var encryptedFileName = $"{model.CustomerID}_{Guid.NewGuid()}.encrypted";
                    var encryptedFilePath = Path.Combine(uploadsPath, encryptedFileName);
                    var tempFilePath = Path.Combine(uploadsPath, $"temp_{Guid.NewGuid()}{fileExtension}");

                    documentFile.SaveAs(tempFilePath);
                    EncryptionHelper.EncryptFile(tempFilePath, encryptedFilePath);
                    if (System.IO.File.Exists(tempFilePath)) System.IO.File.Delete(tempFilePath);

                    model.DocumentFilePath = encryptedFileName;
                    model.OriginalFileName = documentFile.FileName;
                    model.FileType = fileExtension.TrimStart('.');
                    model.FileSizeBytes = documentFile.ContentLength;
                }

                // ── Auto-generate document reference number
                model.DocumentNumber = GenerateDocumentNumber(model.DocumentType);

                model.CreatedBy = User.Identity.GetUserId();
                model.CreatedDate = DateTime.Now;
                model.ModifiedDate = DateTime.Now;

                _context.IdentificationDocuments.Add(model);
                _context.SaveChanges();

                var customer = _context.Customers.Find(model.CustomerID);
                string entityDesc = customer != null
                    ? $"{customer.FullName} ({customer.CustomerCode})"
                    : $"CustomerID {model.CustomerID}";

                _audit.Log(this, "Create", "Document", model.DocumentID,
                    entityDesc,
                    $"Added {model.DocumentType} document. Reference: {model.DocumentNumber}.");

                TempData["SuccessMessage"] = $"Document added successfully. Reference: {model.DocumentNumber}";
                return RedirectToAction("Index", new { customerId = model.CustomerID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error saving document: " + ex.Message;
                ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                ViewBag.DocumentTypes = GetDocumentTypes();
                return View(model);
            }
        }

        // GET: Documents/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue) return RedirectToAction("Index", "Customer");

            var document = _context.IdentificationDocuments.Find(id.Value);
            if (document == null)
            {
                TempData["ErrorMessage"] = "Document not found.";
                return RedirectToAction("Index", "Customer");
            }

            ViewBag.Customer = _context.Customers.Find(document.CustomerID);
            ViewBag.DocumentTypes = GetDocumentTypes();
            return View(document);
        }

        // POST: Documents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(IdentificationDocument model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                ViewBag.DocumentTypes = GetDocumentTypes();
                return View(model);
            }

            try
            {
                var existing = _context.IdentificationDocuments.Find(model.DocumentID);
                if (existing == null)
                {
                    TempData["ErrorMessage"] = "Document not found.";
                    return RedirectToAction("Index", new { customerId = model.CustomerID });
                }

                var customer = _context.Customers.Find(model.CustomerID);
                string entityDesc = customer != null
                    ? $"{customer.FullName} ({customer.CustomerCode})"
                    : $"CustomerID {model.CustomerID}";

                var changes = new Dictionary<string, (string, string)>
                {
                    ["Document Type"] = (existing.DocumentType, model.DocumentType),
                    ["Issuing Country"] = (existing.IssuingCountry, model.IssuingCountry),
                    ["Issuing Authority"] = (existing.IssuingAuthority, model.IssuingAuthority),
                    ["Issue Date"] = (existing.IssueDate?.ToString("dd/MM/yyyy"), model.IssueDate?.ToString("dd/MM/yyyy")),
                    ["Expiry Date"] = (existing.ExpiryDate?.ToString("dd/MM/yyyy"), model.ExpiryDate?.ToString("dd/MM/yyyy")),
                    ["Is Verified"] = (existing.IsVerified.ToString(), model.IsVerified.ToString()),
                    ["Verification Notes"] = (existing.VerificationNotes, model.VerificationNotes),
                };
                string fieldChanges = _audit.BuildFieldChanges(changes);

                existing.IssuingCountry = model.IssuingCountry;
                existing.IssuingAuthority = model.IssuingAuthority;
                existing.IssueDate = model.IssueDate;
                existing.ExpiryDate = model.ExpiryDate;
                existing.IsVerified = model.IsVerified;
                existing.VerificationNotes = model.VerificationNotes;
                existing.ModifiedDate = DateTime.Now;

                // Track verification timestamp
                if (model.IsVerified && !existing.IsVerified)
                {
                    existing.VerifiedBy = User.Identity.GetUserId();
                    existing.VerifiedDate = DateTime.Now;
                }

                _context.SaveChanges();

                _audit.Log(this, "Update", "Document", existing.DocumentID,
                    entityDesc,
                    $"Updated {existing.DocumentType} document. Reference: {existing.DocumentNumber}.",
                    fieldChanges);

                TempData["SuccessMessage"] = "Document updated successfully.";
                return RedirectToAction("Index", new { customerId = model.CustomerID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating document: " + ex.Message;
                ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                ViewBag.DocumentTypes = GetDocumentTypes();
                return View(model);
            }
        }

        // POST: Documents/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id, int customerId)
        {
            try
            {
                var document = _context.IdentificationDocuments.Find(id);
                if (document != null)
                {
                    var customer = _context.Customers.Find(customerId);
                    string entityDesc = customer != null
                        ? $"{customer.FullName} ({customer.CustomerCode})"
                        : $"CustomerID {customerId}";

                    _audit.Log(this, "Delete", "Document", id,
                        entityDesc,
                        $"Deleted {document.DocumentType} document. Reference: {document.DocumentNumber}.");

                    _context.IdentificationDocuments.Remove(document);
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Document deleted successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting document: " + ex.Message;
            }

            return RedirectToAction("Index", new { customerId });
        }

        // GET: Documents/Download/5
        public ActionResult Download(int id)
        {
            try
            {
                var document = _context.IdentificationDocuments.Find(id);
                if (document == null) return RedirectToAction("Index", "Customer");

                var customer = _context.Customers.Find(document.CustomerID);
                string entityDesc = customer != null
                    ? $"{customer.FullName} ({customer.CustomerCode})"
                    : $"CustomerID {document.CustomerID}";

                var uploadsPath = Server.MapPath("~/App_Data/DocumentUploads");
                var encryptedFilePath = Path.Combine(uploadsPath, document.DocumentFilePath);

                if (!System.IO.File.Exists(encryptedFilePath))
                {
                    TempData["ErrorMessage"] = "Document file not found on server.";
                    return RedirectToAction("Index", new { customerId = document.CustomerID });
                }

                byte[] decryptedBytes = EncryptionHelper.DecryptFile(encryptedFilePath);

                _audit.Log(this, "View", "Document", id,
                    entityDesc,
                    $"Downloaded document file: {document.OriginalFileName}");

                string contentType = "application/octet-stream";
                switch (document.FileType?.ToLower())
                {
                    case "pdf": contentType = "application/pdf"; break;
                    case "jpg":
                    case "jpeg": contentType = "image/jpeg"; break;
                    case "png": contentType = "image/png"; break;
                }

                return File(decryptedBytes, contentType, document.OriginalFileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error downloading document: " + ex.Message;
                return RedirectToAction("Index", "Customer");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private string GenerateDocumentNumber(string documentType)
        {
            string prefix;
            if (documentType == "Driver's License")
                prefix = "DRV";
            else if (documentType == "Passport")
                prefix = "PSP";
            else if (documentType == "National ID Card")
                prefix = "NID";
            else
                prefix = "OTH";

            int year = DateTime.Now.Year;
            string pattern = $"{prefix}-{year}-";

            // Count existing documents with this prefix+year to get next sequence
            int count = _context.IdentificationDocuments
                .Count(d => d.DocumentNumber != null && d.DocumentNumber.StartsWith(pattern));

            int sequence = count + 1;

            // Collision guard: increment until unique
            string candidate;
            do
            {
                candidate = $"{pattern}{sequence:D5}";
                sequence++;
            }
            while (_context.IdentificationDocuments.Any(d => d.DocumentNumber == candidate));

            return candidate;
        }

        private SelectList GetDocumentTypes()
        {
            return new SelectList(new[] { "Driver's License", "Passport", "National ID Card", "Other" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _context.Dispose(); _audit.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
