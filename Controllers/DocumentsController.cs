using System;
using System.Linq;
using System.Web.Mvc;
using TechSolutions.Models;
using System.IO;
using System.Web;
using TechSolutions.Helpers;

namespace TechSolutions.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DocumentsController()
        {
            _context = new ApplicationDbContext();
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

            var model = new IdentificationDocument
            {
                CustomerID = customerId
            };

            ViewBag.Customer = customer;
            ViewBag.DocumentTypes = GetDocumentTypes();
            return View(model);
        }

        // POST: Documents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IdentificationDocument model, HttpPostedFileBase documentFile)
        {
            if (!ModelState.IsValid)
            {
                var customer = _context.Customers.Find(model.CustomerID);
                ViewBag.Customer = customer;
                ViewBag.DocumentTypes = GetDocumentTypes();
                return View(model);
            }

            try
            {
                // Handle file upload with ENCRYPTION
                if (documentFile != null && documentFile.ContentLength > 0)
                {
                    // Validate file size (5MB max)
                    if (documentFile.ContentLength > 5 * 1024 * 1024)
                    {
                        TempData["ErrorMessage"] = "File size must be less than 5MB.";
                        var customer2 = _context.Customers.Find(model.CustomerID);
                        ViewBag.Customer = customer2;
                        ViewBag.DocumentTypes = GetDocumentTypes();
                        return View(model);
                    }

                    // Validate file type
                    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(documentFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["ErrorMessage"] = "Only PDF, JPG, and PNG files are allowed.";
                        var customer2 = _context.Customers.Find(model.CustomerID);
                        ViewBag.Customer = customer2;
                        ViewBag.DocumentTypes = GetDocumentTypes();
                        return View(model);
                    }

                    // Create uploads directory if it doesn't exist
                    var uploadsPath = Server.MapPath("~/App_Data/DocumentUploads");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    // Generate unique filename for encrypted file
                    var encryptedFileName = $"{model.CustomerID}_{Guid.NewGuid()}.encrypted";
                    var encryptedFilePath = Path.Combine(uploadsPath, encryptedFileName);

                    // Save temp file first
                    var tempFilePath = Path.Combine(uploadsPath, $"temp_{Guid.NewGuid()}{fileExtension}");
                    documentFile.SaveAs(tempFilePath);

                    // Encrypt the file
                    EncryptionHelper.EncryptFile(tempFilePath, encryptedFilePath);

                    // Delete temp file - USE System.IO.File
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        System.IO.File.Delete(tempFilePath);
                    }

                    // Store file info in model
                    model.DocumentFilePath = encryptedFileName;
                    model.OriginalFileName = documentFile.FileName;
                    model.FileType = fileExtension.TrimStart('.');
                    model.FileSizeBytes = documentFile.ContentLength;
                }

                // Encrypt sensitive fields
                if (!string.IsNullOrEmpty(model.IDNumber))
                {
                    model.DocumentNumber = model.IDNumber; // Auto-populate
                    model.IDNumber = EncryptionHelper.EncryptString(model.IDNumber);
                }

                if (!string.IsNullOrEmpty(model.PassportNumber))
                {
                    model.DocumentNumber = model.PassportNumber; // Auto-populate
                    model.PassportNumber = EncryptionHelper.EncryptString(model.PassportNumber);
                }

                if (!string.IsNullOrEmpty(model.ProfessionalRegNumber))
                {
                    model.DocumentNumber = model.ProfessionalRegNumber; // Auto-populate
                    model.ProfessionalRegNumber = EncryptionHelper.EncryptString(model.ProfessionalRegNumber);
                }

                // Encrypt the DocumentNumber too
                if (!string.IsNullOrEmpty(model.DocumentNumber))
                {
                    model.DocumentNumber = EncryptionHelper.EncryptString(model.DocumentNumber);
                }

                model.CreatedDate = DateTime.Now;
                model.ModifiedDate = DateTime.Now;

                _context.IdentificationDocuments.Add(model);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "ID document added successfully with encryption.";
                return RedirectToAction("Index", new { customerId = model.CustomerID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error saving document: " + ex.Message;
                var customer = _context.Customers.Find(model.CustomerID);
                ViewBag.Customer = customer;
                ViewBag.DocumentTypes = GetDocumentTypes();
                return View(model);
            }
        }

        // GET: Documents/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue)
            {
                TempData["ErrorMessage"] = "Document ID is required.";
                return RedirectToAction("Index", "Customer");
            }

            var document = _context.IdentificationDocuments.Find(id.Value);
            if (document == null)
            {
                TempData["ErrorMessage"] = "Document not found.";
                return RedirectToAction("Index", "Customer");
            }

            // Decrypt sensitive fields for editing
            if (!string.IsNullOrEmpty(document.IDNumber))
            {
                document.IDNumber = EncryptionHelper.DecryptString(document.IDNumber);
            }

            if (!string.IsNullOrEmpty(document.PassportNumber))
            {
                document.PassportNumber = EncryptionHelper.DecryptString(document.PassportNumber);
            }

            if (!string.IsNullOrEmpty(document.ProfessionalRegNumber))
            {
                document.ProfessionalRegNumber = EncryptionHelper.DecryptString(document.ProfessionalRegNumber);
            }

            var customer = _context.Customers.Find(document.CustomerID);
            ViewBag.Customer = customer;
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
                var customer = _context.Customers.Find(model.CustomerID);
                ViewBag.Customer = customer;
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

                existing.DocumentType = model.DocumentType;
                existing.IssuingCountry = model.IssuingCountry;
                existing.IssuingAuthority = model.IssuingAuthority;
                existing.IssueDate = model.IssueDate;
                existing.ExpiryDate = model.ExpiryDate;
                existing.IsVerified = model.IsVerified;
                existing.VerificationNotes = model.VerificationNotes;

                // Re-encrypt sensitive fields
                if (!string.IsNullOrEmpty(model.IDNumber))
                {
                    existing.IDNumber = EncryptionHelper.EncryptString(model.IDNumber);
                    existing.DocumentNumber = existing.IDNumber;
                }

                if (!string.IsNullOrEmpty(model.PassportNumber))
                {
                    existing.PassportNumber = EncryptionHelper.EncryptString(model.PassportNumber);
                    existing.DocumentNumber = existing.PassportNumber;
                }

                if (!string.IsNullOrEmpty(model.ProfessionalRegNumber))
                {
                    existing.ProfessionalRegNumber = EncryptionHelper.EncryptString(model.ProfessionalRegNumber);
                    existing.DocumentNumber = existing.ProfessionalRegNumber;
                }

                existing.ModifiedDate = DateTime.Now;

                _context.SaveChanges();

                TempData["SuccessMessage"] = "ID document updated successfully.";
                return RedirectToAction("Index", new { customerId = model.CustomerID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating document: " + ex.Message;
                var customer = _context.Customers.Find(model.CustomerID);
                ViewBag.Customer = customer;
                ViewBag.DocumentTypes = GetDocumentTypes();
                return View(model);
            }
        }

        // POST: Documents/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, int customerId)
        {
            try
            {
                var document = _context.IdentificationDocuments.Find(id);
                if (document != null)
                {
                    _context.IdentificationDocuments.Remove(document);
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "ID document deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Document not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting document: " + ex.Message;
            }

            return RedirectToAction("Index", new { customerId = customerId });
        }

        private SelectList GetDocumentTypes()
        {
            var types = new[]
            {
                "Driver's License",
                "Passport",
                "National ID Card",
                "Birth Certificate",
                "Social Security Card",
                "Other"
            };
            return new SelectList(types);
        }

        // GET: Documents/Download/5
        public ActionResult Download(int id)
        {
            try
            {
                var document = _context.IdentificationDocuments.Find(id);
                if (document == null)
                {
                    TempData["ErrorMessage"] = "Document not found.";
                    return RedirectToAction("Index", "Customer");
                }

                // Verify user has permission (add role check here later)
                var customer = _context.Customers.Find(document.CustomerID);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction("Index", "Customer");
                }

                // Get encrypted file path
                var uploadsPath = Server.MapPath("~/App_Data/DocumentUploads");
                var encryptedFilePath = Path.Combine(uploadsPath, document.DocumentFilePath);

                // USE System.IO.File HERE TOO
                if (!System.IO.File.Exists(encryptedFilePath))
                {
                    TempData["ErrorMessage"] = "Document file not found on server.";
                    return RedirectToAction("Index", new { customerId = document.CustomerID });
                }

                // Decrypt file
                byte[] decryptedBytes = EncryptionHelper.DecryptFile(encryptedFilePath);

                // Determine content type
                string contentType = "application/octet-stream";
                switch (document.FileType.ToLower())
                {
                    case "pdf":
                        contentType = "application/pdf";
                        break;
                    case "jpg":
                    case "jpeg":
                        contentType = "image/jpeg";
                        break;
                    case "png":
                        contentType = "image/png";
                        break;
                }

                // Return decrypted file (THIS File() is correct - it's the Controller method)
                return File(decryptedBytes, contentType, document.OriginalFileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error downloading document: " + ex.Message;
                return RedirectToAction("Index", "Customer");
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