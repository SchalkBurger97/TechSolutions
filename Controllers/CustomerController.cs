using System;
using System.Collections.Generic;
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
    public class CustomerController : Controller
    {
        private ICustomerService _customerService;
        private readonly AuditService _audit;

        public CustomerController()
        {
            _customerService = new CustomerService();
            _audit = new AuditService();
        }

        // GET: Customer
        public ActionResult Index(string search, int page = 1)
        {
            int pageSize = 10;
            var customers = _customerService.GetCustomers(search, page, pageSize);
            var totalCustomers = _customerService.GetTotalCustomers(search);
            var totalPages = (int)Math.Ceiling((double)totalCustomers / pageSize);

            ViewBag.SearchTerm = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCustomers = totalCustomers;

            return View(customers);
        }

        // GET: Customer/Details/5
        public ActionResult Details(int id)
        {
            var customer = _customerService.getCustomerById(id);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction("Index");
            }
            return View(customer);
        }

        // GET: Customer/Create
        public ActionResult Create()
        {
            return RedirectToAction("Wizard");
        }

        // GET: Customer/Wizard
        public ActionResult Wizard(int step = 1)
        {
            ViewBag.CurrentStep = step;
            ViewBag.CustomerTypes = GetCustomerTypes();
            ViewBag.Genders = GetGenders();
            ViewBag.Provinces = GetProvinces();

            Customer model;
            if (Session["WizardData"] != null)
                model = Session["WizardData"] as Customer;
            else
                model = new Customer
                {
                    CustomerCode = _customerService.GenerateCustomerCode(),
                    IsActive = true
                };

            // Decrypt for display on step 1 if values exist in session
            if (step == 1 && model != null)
            {
                if (!string.IsNullOrEmpty(model.IDNumber))
                    model.IDNumber = TryDecrypt(model.IDNumber);
                if (!string.IsNullOrEmpty(model.PassportNumber))
                    model.PassportNumber = TryDecrypt(model.PassportNumber);
            }

            return View(model);
        }

        // POST: Customer/Wizard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Wizard(Customer model, int currentStep, string action)
        {
            Customer sessionModel;
            if (Session["WizardData"] != null)
                sessionModel = Session["WizardData"] as Customer;
            else
                sessionModel = new Customer
                {
                    CustomerCode = _customerService.GenerateCustomerCode(),
                    IsActive = true
                };

            if (currentStep == 1)
            {
                sessionModel.FirstName = model.FirstName;
                sessionModel.LastName = model.LastName;
                sessionModel.Email = model.Email;
                sessionModel.Phone = model.Phone;
                sessionModel.CustomerType = model.CustomerType;
                sessionModel.DateOfBirth = model.DateOfBirth;
                sessionModel.Gender = model.Gender;
                sessionModel.IDType = model.IDType;

                // Encrypt identity number based on type selected
                if (model.IDType == "ID")
                {
                    sessionModel.IDNumber = !string.IsNullOrWhiteSpace(model.IDNumber)
                                                    ? EncryptionHelper.EncryptString(model.IDNumber)
                                                    : null;
                    sessionModel.PassportNumber = null;
                }
                else if (model.IDType == "Passport")
                {
                    sessionModel.PassportNumber = !string.IsNullOrWhiteSpace(model.PassportNumber)
                                                    ? EncryptionHelper.EncryptString(model.PassportNumber)
                                                    : null;
                    sessionModel.IDNumber = null;
                }
            }
            else if (currentStep == 2)
            {
                sessionModel.Address = model.Address;
                sessionModel.City = model.City;
                sessionModel.Province = model.Province;
                sessionModel.PostalCode = model.PostalCode;
            }

            Session["WizardData"] = sessionModel;

            if (action == "Next")
            {
                if (currentStep == 1)
                {
                    ModelState.Clear();
                    if (string.IsNullOrWhiteSpace(sessionModel.FirstName))
                        ModelState.AddModelError("FirstName", "First name is required");
                    if (string.IsNullOrWhiteSpace(sessionModel.LastName))
                        ModelState.AddModelError("LastName", "Last name is required");
                    if (string.IsNullOrWhiteSpace(sessionModel.Email))
                        ModelState.AddModelError("Email", "Email is required");
                    else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(sessionModel.Email))
                        ModelState.AddModelError("Email", "Invalid email format");
                    if (string.IsNullOrWhiteSpace(sessionModel.Phone))
                        ModelState.AddModelError("Phone", "Phone number is required");
                    else if (!System.Text.RegularExpressions.Regex.IsMatch(sessionModel.Phone, @"^0[0-9]{9}$"))
                        ModelState.AddModelError("Phone", "Phone must be 10 digits starting with 0");
                    if (string.IsNullOrWhiteSpace(sessionModel.CustomerType))
                        ModelState.AddModelError("CustomerType", "Customer type is required");
                    if (string.IsNullOrWhiteSpace(sessionModel.IDType))
                        ModelState.AddModelError("IDType", "Please select an ID type");
                    else if (sessionModel.IDType == "ID" && string.IsNullOrWhiteSpace(sessionModel.IDNumber))
                        ModelState.AddModelError("IDNumber", "SA ID number is required");
                    else if (sessionModel.IDType == "Passport" && string.IsNullOrWhiteSpace(sessionModel.PassportNumber))
                        ModelState.AddModelError("PassportNumber", "Passport number is required");

                    if (!ModelState.IsValid)
                    {
                        // Decrypt back for re-display
                        var display = CloneForDisplay(sessionModel);
                        ViewBag.CurrentStep = currentStep;
                        ViewBag.CustomerTypes = GetCustomerTypes();
                        ViewBag.Genders = GetGenders();
                        ViewBag.Provinces = GetProvinces();
                        return View(display);
                    }
                }
                else if (currentStep == 2)
                {
                    ModelState.Clear();
                    if (string.IsNullOrWhiteSpace(sessionModel.Address))
                        ModelState.AddModelError("Address", "Address is required");
                    if (string.IsNullOrWhiteSpace(sessionModel.City))
                        ModelState.AddModelError("City", "City is required");
                    if (string.IsNullOrWhiteSpace(sessionModel.Province))
                        ModelState.AddModelError("Province", "Province is required");
                    if (string.IsNullOrWhiteSpace(sessionModel.PostalCode))
                        ModelState.AddModelError("PostalCode", "Postal code is required");
                    else if (!System.Text.RegularExpressions.Regex.IsMatch(sessionModel.PostalCode, @"^[0-9]{4}$"))
                        ModelState.AddModelError("PostalCode", "Postal code must be 4 digits");

                    if (!ModelState.IsValid)
                    {
                        ViewBag.CurrentStep = currentStep;
                        ViewBag.CustomerTypes = GetCustomerTypes();
                        ViewBag.Genders = GetGenders();
                        ViewBag.Provinces = GetProvinces();
                        return View(sessionModel);
                    }
                }

                return RedirectToAction("Wizard", new { step = currentStep + 1 });
            }
            else if (action == "Previous")
            {
                return RedirectToAction("Wizard", new { step = currentStep - 1 });
            }
            else if (action == "Submit")
            {
                if (sessionModel == null)
                {
                    ModelState.AddModelError("", "Session expired. Please start again.");
                    return RedirectToAction("Wizard", new { step = 1 });
                }

                ModelState.Clear();

                if (string.IsNullOrWhiteSpace(sessionModel.FirstName))
                    ModelState.AddModelError("FirstName", "First name is required");
                if (string.IsNullOrWhiteSpace(sessionModel.LastName))
                    ModelState.AddModelError("LastName", "Last name is required");
                if (string.IsNullOrWhiteSpace(sessionModel.Email))
                    ModelState.AddModelError("Email", "Email is required");
                else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(sessionModel.Email))
                    ModelState.AddModelError("Email", "Invalid email format");
                if (string.IsNullOrWhiteSpace(sessionModel.Phone))
                    ModelState.AddModelError("Phone", "Phone number is required");
                else if (!System.Text.RegularExpressions.Regex.IsMatch(sessionModel.Phone, @"^0[0-9]{9}$"))
                    ModelState.AddModelError("Phone", "Phone must be 10 digits starting with 0");
                if (string.IsNullOrWhiteSpace(sessionModel.CustomerType))
                    ModelState.AddModelError("CustomerType", "Customer type is required");
                if (string.IsNullOrWhiteSpace(sessionModel.Address))
                    ModelState.AddModelError("Address", "Address is required");
                if (string.IsNullOrWhiteSpace(sessionModel.City))
                    ModelState.AddModelError("City", "City is required");
                if (string.IsNullOrWhiteSpace(sessionModel.Province))
                    ModelState.AddModelError("Province", "Province is required");
                if (string.IsNullOrWhiteSpace(sessionModel.PostalCode))
                    ModelState.AddModelError("PostalCode", "Postal code is required");
                else if (!System.Text.RegularExpressions.Regex.IsMatch(sessionModel.PostalCode, @"^[0-9]{4}$"))
                    ModelState.AddModelError("PostalCode", "Postal code must be 4 digits");

                if (_customerService.CustomerCodeExists(sessionModel.CustomerCode))
                    ModelState.AddModelError("CustomerCode", "This customer code already exists");

                if (!ModelState.IsValid)
                {
                    ViewBag.CurrentStep = 6;
                    ViewBag.CustomerTypes = GetCustomerTypes();
                    ViewBag.Genders = GetGenders();
                    ViewBag.Provinces = GetProvinces();
                    return View(sessionModel);
                }

                try
                {
                    sessionModel.CreatedBy = User.Identity.GetUserId();
                    _customerService.CreateCustomer(sessionModel);

                    _audit.Log(this, "Create", "Customer", sessionModel.CustomerID,
                        $"{sessionModel.FullName} ({sessionModel.CustomerCode})",
                        $"New customer created via wizard. Type: {sessionModel.CustomerType}.");

                    TempData["SuccessMessage"] = "Customer enrolled successfully!";
                    Session.Remove("WizardData");
                    return RedirectToAction("Details", new { id = sessionModel.CustomerID });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating customer: " + ex.Message);
                    ViewBag.CurrentStep = 6;
                    ViewBag.CustomerTypes = GetCustomerTypes();
                    ViewBag.Genders = GetGenders();
                    ViewBag.Provinces = GetProvinces();
                    return View(sessionModel);
                }
            }

            return RedirectToAction("Wizard", new { step = currentStep });
        }

        // POST: Customer/Create (non-wizard fallback)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (_customerService.CustomerCodeExists(customer.CustomerCode))
                    {
                        ModelState.AddModelError("CustomerCode", "This customer code already exists.");
                    }
                    else
                    {
                        EncryptCustomerIdentity(customer);
                        customer.CreatedBy = User.Identity.GetUserId();
                        _customerService.CreateCustomer(customer);

                        _audit.Log(this, "Create", "Customer", customer.CustomerID,
                            $"{customer.FullName} ({customer.CustomerCode})",
                            $"Customer created. Type: {customer.CustomerType}.");

                        TempData["SuccessMessage"] = "Customer created successfully!";
                        return RedirectToAction("Details", new { id = customer.CustomerID });
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating customer: " + ex.Message);
                }
            }

            ViewBag.CustomerTypes = GetCustomerTypes();
            ViewBag.Genders = GetGenders();
            ViewBag.Provinces = GetProvinces();
            return View(customer);
        }

        // GET: Customer/Edit/5
        public ActionResult Edit(int id)
        {
            var customer = _customerService.getCustomerById(id);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction("Index");
            }

            // Decrypt identity numbers for display
            if (!string.IsNullOrEmpty(customer.IDNumber))
                customer.IDNumber = TryDecrypt(customer.IDNumber);
            if (!string.IsNullOrEmpty(customer.PassportNumber))
                customer.PassportNumber = TryDecrypt(customer.PassportNumber);

            ViewBag.CustomerTypes = GetCustomerTypes();
            ViewBag.Genders = GetGenders();
            ViewBag.Provinces = GetProvinces();
            return View(customer);
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existing = _customerService.getCustomerById(customer.CustomerID);
                    string entityDesc = $"{customer.FullName} ({customer.CustomerCode})";

                    var changes = new Dictionary<string, (string, string)>
                    {
                        ["First Name"] = (existing?.FirstName, customer.FirstName),
                        ["Last Name"] = (existing?.LastName, customer.LastName),
                        ["Email"] = (existing?.Email, customer.Email),
                        ["Phone"] = (existing?.Phone, customer.Phone),
                        ["Customer Type"] = (existing?.CustomerType, customer.CustomerType),
                        ["Address"] = (existing?.Address, customer.Address),
                        ["City"] = (existing?.City, customer.City),
                        ["Province"] = (existing?.Province, customer.Province),
                        ["Postal Code"] = (existing?.PostalCode, customer.PostalCode),
                        ["Is Active"] = (existing?.IsActive.ToString(), customer.IsActive.ToString()),
                        ["ID Type"] = (existing?.IDType, customer.IDType),
                    };
                    string fieldChanges = _audit.BuildFieldChanges(changes);

                    // Re-encrypt identity numbers before saving
                    EncryptCustomerIdentity(customer);

                    customer.ModifiedBy = User.Identity.GetUserId();
                    customer.ModifiedDate = DateTime.Now;
                    _customerService.UpdateCustomer(customer);

                    _audit.Log(this, "Update", "Customer", customer.CustomerID,
                        entityDesc,
                        "Customer profile updated.",
                        fieldChanges);

                    TempData["SuccessMessage"] = "Customer updated successfully!";
                    return RedirectToAction("Details", new { id = customer.CustomerID });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating customer: " + ex.Message);
                }
            }

            ViewBag.CustomerTypes = GetCustomerTypes();
            ViewBag.Genders = GetGenders();
            ViewBag.Provinces = GetProvinces();
            return View(customer);
        }

        // POST: Customer/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            try
            {
                var customer = _customerService.getCustomerById(id);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction("Index");
                }

                _audit.Log(this, "Delete", "Customer", id,
                    $"{customer.FullName} ({customer.CustomerCode})",
                    $"Customer deleted. Type: {customer.CustomerType}.");

                _customerService.DeleteCustomer(id);
                TempData["SuccessMessage"] = $"Customer '{customer.FullName}' deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting customer: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private void EncryptCustomerIdentity(Customer customer)
        {
            if (customer.IDType == "ID")
            {
                if (!string.IsNullOrWhiteSpace(customer.IDNumber))
                    customer.IDNumber = EncryptionHelper.EncryptString(customer.IDNumber);
                customer.PassportNumber = null;
            }
            else if (customer.IDType == "Passport")
            {
                if (!string.IsNullOrWhiteSpace(customer.PassportNumber))
                    customer.PassportNumber = EncryptionHelper.EncryptString(customer.PassportNumber);
                customer.IDNumber = null;
            }
            else
            {
                // No type selected — clear both
                customer.IDNumber = null;
                customer.PassportNumber = null;
            }
        }

        private Customer CloneForDisplay(Customer source)
        {
            return new Customer
            {
                CustomerID = source.CustomerID,
                CustomerCode = source.CustomerCode,
                FirstName = source.FirstName,
                LastName = source.LastName,
                Email = source.Email,
                Phone = source.Phone,
                CustomerType = source.CustomerType,
                DateOfBirth = source.DateOfBirth,
                Gender = source.Gender,
                IDType = source.IDType,
                IDNumber = TryDecrypt(source.IDNumber),
                PassportNumber = TryDecrypt(source.PassportNumber),
                Address = source.Address,
                City = source.City,
                Province = source.Province,
                PostalCode = source.PostalCode,
                IsActive = source.IsActive,
                IsDeleted = source.IsDeleted,
                DataQualityScore = source.DataQualityScore,
                RiskScore = source.RiskScore,
                CreatedDate = source.CreatedDate,
                CreatedBy = source.CreatedBy,
                ModifiedDate = source.ModifiedDate,
                ModifiedBy = source.ModifiedBy
            };
        }

        private string TryDecrypt(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            try { return EncryptionHelper.DecryptString(value); }
            catch { return value; } // already plain-text (e.g. fresh from form)
        }

        private SelectList GetCustomerTypes() => new SelectList(new[]
        {
            new { Value = "Healthcare Professional",       Text = "Healthcare Professional" },
            new { Value = "Hospital Administrator",        Text = "Hospital Administrator" },
            new { Value = "Medical Student",               Text = "Medical Student" },
            new { Value = "Healthcare Compliance Officer", Text = "Healthcare Compliance Officer" }
        }, "Value", "Text");

        private SelectList GetGenders() => new SelectList(new[]
        {
            new { Value = "Male",              Text = "Male" },
            new { Value = "Female",            Text = "Female" },
            new { Value = "Other",             Text = "Other" },
            new { Value = "Prefer not to say", Text = "Prefer not to say" }
        }, "Value", "Text");

        private SelectList GetProvinces() => new SelectList(new[]
        {
            new { Value = "Gauteng",       Text = "Gauteng" },
            new { Value = "Western Cape",  Text = "Western Cape" },
            new { Value = "KwaZulu-Natal", Text = "KwaZulu-Natal" },
            new { Value = "Eastern Cape",  Text = "Eastern Cape" },
            new { Value = "Free State",    Text = "Free State" },
            new { Value = "Limpopo",       Text = "Limpopo" },
            new { Value = "Mpumalanga",    Text = "Mpumalanga" },
            new { Value = "North West",    Text = "North West" },
            new { Value = "Northern Cape", Text = "Northern Cape" }
        }, "Value", "Text");

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_customerService != null)
                    ((CustomerService)_customerService).Dispose();
                _audit?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
