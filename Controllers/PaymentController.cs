using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TechSolutions.Helpers;
using TechSolutions.Models;
using TechSolutions.Services;
using Microsoft.AspNet.Identity;

namespace TechSolutions.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _audit;

        public PaymentController()
        {
            _context = new ApplicationDbContext();
            _audit = new AuditService(_context);
        }

        // GET: Payment/Index?customerId=5
        public ActionResult Index(int customerId)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction("Index", "Customer");
            }

            var payments = _context.PaymentInformation
                .Where(p => p.CustomerID == customerId)
                .OrderByDescending(p => p.IsPrimary)
                .ThenByDescending(p => p.CreatedDate)
                .ToList();

            ViewBag.Customer = customer;
            return View(payments);
        }

        // GET: Payment/Create?customerId=5
        public ActionResult Create(int customerId)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction("Index", "Customer");
            }

            ViewBag.Customer = customer;
            ViewBag.PaymentTypes = GetPaymentTypes();
            ViewBag.CardTypes = GetCardTypes();
            ViewBag.BankNames = GetSouthAfricanBanks();
            ViewBag.AccountTypes = GetAccountTypes();
            return View(new PaymentInformation { CustomerID = customerId, IsPrimary = false, IsActive = true });
        }

        // POST: Payment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PaymentInformation model, string FullAccountNumber, string FullCardNumber, string CVV)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                ViewBag.PaymentTypes = GetPaymentTypes();
                ViewBag.CardTypes = GetCardTypes();
                ViewBag.BankNames = GetSouthAfricanBanks();
                ViewBag.AccountTypes = GetAccountTypes();
                return View(model);
            }

            try
            {
                if ((model.PaymentMethodType == "Credit Card" || model.PaymentMethodType == "Debit Card")
                    && !string.IsNullOrEmpty(FullCardNumber))
                {
                    model.EncryptedCardNumber = EncryptionHelper.EncryptString(FullCardNumber);
                    if (FullCardNumber.Length >= 4)
                        model.CardLast4Digits = FullCardNumber.Substring(FullCardNumber.Length - 4);
                    if (!string.IsNullOrEmpty(CVV))
                        model.EncryptedCVV = EncryptionHelper.EncryptString(CVV);
                }

                if (model.PaymentMethodType == "Bank Account" && !string.IsNullOrEmpty(FullAccountNumber))
                {
                    model.BankAccountNumber = EncryptionHelper.EncryptString(FullAccountNumber);
                    if (FullAccountNumber.Length >= 4)
                        model.AccountLast4Digits = FullAccountNumber.Substring(FullAccountNumber.Length - 4);
                }

                if (model.IsPrimary)
                {
                    _context.PaymentInformation
                        .Where(p => p.CustomerID == model.CustomerID && p.IsPrimary)
                        .ToList()
                        .ForEach(p => p.IsPrimary = false);
                }

                model.CreatedDate = DateTime.Now;
                model.ModifiedDate = DateTime.Now;

                _context.PaymentInformation.Add(model);
                _context.SaveChanges();

                var customer = _context.Customers.Find(model.CustomerID);
                string entityDesc = customer != null
                    ? $"{customer.FullName} ({customer.CustomerCode})"
                    : $"CustomerID {model.CustomerID}";

                string summary = model.PaymentMethodType == "Bank Account"
                    ? $"Added bank account ending {model.AccountLast4Digits} ({model.BankName})"
                    : $"Added {model.CardType} {model.PaymentMethodType} ending {model.CardLast4Digits}";

                _audit.Log(this, "Create", "Payment", model.PaymentID, entityDesc, summary);

                TempData["SuccessMessage"] = "Payment method added successfully.";
                return RedirectToAction("Index", new { customerId = model.CustomerID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error saving payment method: " + ex.Message;
                ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                ViewBag.PaymentTypes = GetPaymentTypes();
                ViewBag.CardTypes = GetCardTypes();
                ViewBag.BankNames = GetSouthAfricanBanks();
                ViewBag.AccountTypes = GetAccountTypes();
                return View(model);
            }
        }

        // GET: Payment/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue) return RedirectToAction("Index", "Customer");

            var payment = _context.PaymentInformation.Find(id.Value);
            if (payment == null)
            {
                TempData["ErrorMessage"] = "Payment method not found.";
                return RedirectToAction("Index", "Customer");
            }

            ViewBag.Customer = _context.Customers.Find(payment.CustomerID);
            ViewBag.PaymentTypes = GetPaymentTypes();
            ViewBag.CardTypes = GetCardTypes();
            ViewBag.BankNames = GetSouthAfricanBanks();
            ViewBag.AccountTypes = GetAccountTypes();
            ViewBag.HasBankAccount = !string.IsNullOrEmpty(payment.BankAccountNumber);
            return View(payment);
        }

        // POST: Payment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(PaymentInformation model, string FullAccountNumber, string FullCardNumber, string CVV)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                ViewBag.PaymentTypes = GetPaymentTypes();
                ViewBag.CardTypes = GetCardTypes();
                ViewBag.BankNames = GetSouthAfricanBanks();
                ViewBag.AccountTypes = GetAccountTypes();
                return View(model);
            }

            try
            {
                var existing = _context.PaymentInformation.Find(model.PaymentID);
                if (existing == null)
                {
                    TempData["ErrorMessage"] = "Payment method not found.";
                    return RedirectToAction("Index", new { customerId = model.CustomerID });
                }

                var customer = _context.Customers.Find(model.CustomerID);
                string entityDesc = customer != null
                    ? $"{customer.FullName} ({customer.CustomerCode})"
                    : $"CustomerID {model.CustomerID}";

                // ── Build field changes ──
                var changes = new Dictionary<string, (string, string)>
                {
                    ["Card Holder"] = (existing.CardHolderName, model.CardHolderName),
                    ["Card Type"] = (existing.CardType, model.CardType),
                    ["Expiry Date"] = (existing.CardExpiryDate?.ToString("MM/yy"), model.CardExpiryDate?.ToString("MM/yy")),
                    ["Bank Name"] = (existing.BankName, model.BankName),
                    ["Account Type"] = (existing.AccountType, model.AccountType),
                    ["Branch Code"] = (existing.BranchCode, model.BranchCode),
                    ["Is Primary"] = (existing.IsPrimary.ToString(), model.IsPrimary.ToString()),
                    ["Is Active"] = (existing.IsActive.ToString(), model.IsActive.ToString()),
                    ["Is Verified"] = (existing.IsVerified.ToString(), model.IsVerified.ToString()),
                    ["Billing Address"] = (existing.BillingAddress, model.BillingAddress),
                    ["Billing City"] = (existing.BillingCity, model.BillingCity),
                    ["Billing Postal"] = (existing.BillingPostalCode, model.BillingPostalCode),
                };
                string fieldChanges = _audit.BuildFieldChanges(changes);

                // ── Update fields ──
                existing.PaymentMethodType = model.PaymentMethodType;
                existing.CardHolderName = model.CardHolderName;
                existing.CardLast4Digits = model.CardLast4Digits;
                existing.CardType = model.CardType;
                existing.CardExpiryDate = model.CardExpiryDate;
                existing.BankName = model.BankName;
                existing.AccountType = model.AccountType;
                existing.BranchCode = model.BranchCode;
                existing.BillingAddress = model.BillingAddress;
                existing.BillingSuburb = model.BillingSuburb;
                existing.BillingCity = model.BillingCity;
                existing.BillingPostalCode = model.BillingPostalCode;
                existing.IsPrimary = model.IsPrimary;
                existing.IsActive = model.IsActive;
                existing.IsVerified = model.IsVerified;

                if (!string.IsNullOrEmpty(FullCardNumber))
                {
                    existing.EncryptedCardNumber = EncryptionHelper.EncryptString(FullCardNumber);
                    if (FullCardNumber.Length >= 4)
                        existing.CardLast4Digits = FullCardNumber.Substring(FullCardNumber.Length - 4);
                }
                if (!string.IsNullOrEmpty(CVV))
                    existing.EncryptedCVV = EncryptionHelper.EncryptString(CVV);
                if (!string.IsNullOrEmpty(FullAccountNumber))
                {
                    existing.BankAccountNumber = EncryptionHelper.EncryptString(FullAccountNumber);
                    if (FullAccountNumber.Length >= 4)
                        existing.AccountLast4Digits = FullAccountNumber.Substring(FullAccountNumber.Length - 4);
                }

                if (model.IsPrimary)
                {
                    _context.PaymentInformation
                        .Where(p => p.CustomerID == model.CustomerID && p.PaymentID != model.PaymentID && p.IsPrimary)
                        .ToList()
                        .ForEach(p => p.IsPrimary = false);
                }

                existing.ModifiedDate = DateTime.Now;
                _context.SaveChanges();

                string summary = existing.PaymentMethodType == "Bank Account"
                    ? $"Updated bank account ending {existing.AccountLast4Digits}"
                    : $"Updated {existing.CardType} card ending {existing.CardLast4Digits}";

                _audit.Log(this, "Update", "Payment", existing.PaymentID,
                    entityDesc, summary, fieldChanges);

                TempData["SuccessMessage"] = "Payment method updated successfully.";
                return RedirectToAction("Index", new { customerId = model.CustomerID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating payment method: " + ex.Message;
                ViewBag.Customer = _context.Customers.Find(model.CustomerID);
                ViewBag.PaymentTypes = GetPaymentTypes();
                ViewBag.CardTypes = GetCardTypes();
                ViewBag.BankNames = GetSouthAfricanBanks();
                ViewBag.AccountTypes = GetAccountTypes();
                return View(model);
            }
        }

        // POST: Payment/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id, int customerId)
        {
            try
            {
                var payment = _context.PaymentInformation.Find(id);
                if (payment != null)
                {
                    var customer = _context.Customers.Find(customerId);
                    string entityDesc = customer != null
                        ? $"{customer.FullName} ({customer.CustomerCode})"
                        : $"CustomerID {customerId}";

                    string summary = payment.PaymentMethodType == "Bank Account"
                        ? $"Deleted bank account ending {payment.AccountLast4Digits}"
                        : $"Deleted {payment.CardType} card ending {payment.CardLast4Digits}";

                    _audit.Log(this, "Delete", "Payment", id, entityDesc, summary);

                    _context.PaymentInformation.Remove(payment);
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Payment method deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Payment method not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting payment method: " + ex.Message;
            }

            return RedirectToAction("Index", new { customerId });
        }

        // POST: Payment/SetPrimary
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetPrimary(int id, int customerId)
        {
            try
            {
                var allPayments = _context.PaymentInformation
                    .Where(p => p.CustomerID == customerId)
                    .ToList();

                foreach (var p in allPayments)
                    p.IsPrimary = (p.PaymentID == id);

                _context.SaveChanges();

                var changed = allPayments.FirstOrDefault(p => p.PaymentID == id);
                var customer = _context.Customers.Find(customerId);
                string entityDesc = customer != null
                    ? $"{customer.FullName} ({customer.CustomerCode})"
                    : $"CustomerID {customerId}";

                if (changed != null)
                {
                    string summary = changed.PaymentMethodType == "Bank Account"
                        ? $"Set bank account ending {changed.AccountLast4Digits} as primary"
                        : $"Set {changed.CardType} card ending {changed.CardLast4Digits} as primary";
                    _audit.Log(this, "Update", "Payment", id, entityDesc, summary);
                }

                TempData["SuccessMessage"] = "Primary payment method updated.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error setting primary payment: " + ex.Message;
            }

            return RedirectToAction("Index", new { customerId });
        }

        // POST: Payment/RevealCardNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult RevealCardNumber(int id)
        {
            // Employees cannot reveal encrypted payment numbers
            if (!User.IsInRole("Admin"))
                return Json(new { success = false, message = "You do not have permission to reveal sensitive payment data." });

            try
            {
                var payment = _context.PaymentInformation.Find(id);
                if (payment == null)
                    return Json(new { success = false, message = "Payment not found." });

                string fullNumber = !string.IsNullOrEmpty(payment.EncryptedCardNumber)
                    ? EncryptionHelper.DecryptString(payment.EncryptedCardNumber)
                    : !string.IsNullOrEmpty(payment.BankAccountNumber)
                        ? EncryptionHelper.DecryptString(payment.BankAccountNumber)
                        : "";

                var customer = _context.Customers.Find(payment.CustomerID);
                string entityDesc = customer != null
                    ? $"{customer.FullName} ({customer.CustomerCode})"
                    : $"CustomerID {payment.CustomerID}";

                string last4 = fullNumber?.Length >= 4
                    ? fullNumber.Substring(fullNumber.Length - 4)
                    : "****";

                string summary = payment.PaymentMethodType == "Bank Account"
                    ? $"Revealed bank account number ending {last4}"
                    : $"Revealed {payment.CardType} card number ending {last4}";

                _audit.Log(this, "Reveal", "Payment", id, entityDesc, summary);

                return Json(new { success = true, fullNumber });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        private SelectList GetPaymentTypes() => new SelectList(new[] { "Credit Card", "Debit Card", "Bank Account" });
        private SelectList GetCardTypes() => new SelectList(new[] { "Visa", "Mastercard" });
        private SelectList GetAccountTypes() => new SelectList(new[] { "Cheque Account", "Savings Account", "Credit Card" });
        private SelectList GetSouthAfricanBanks() => new SelectList(new[]
        {
            "FNB (First National Bank)", "Standard Bank", "ABSA", "Nedbank",
            "Capitec", "Investec", "African Bank", "Bidvest Bank", "Discovery Bank", "TymeBank"
        });

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _context.Dispose(); _audit.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
