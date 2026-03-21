using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TechSolutions.Models;
using TechSolutions.Services;

namespace TechSolutions.Controllers
{
    [Authorize] // Require login for all actions
    public class CustomerController : Controller
    {
        private ICustomerService _customerService;

        public CustomerController()
        {
            _customerService = new CustomerService();
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

        // GET: Customer/Create (Start Wizard)
        public ActionResult Create()
        {
            // Redirect to wizard instead of old form
            return RedirectToAction("Wizard");
        }

        // GET: Customer/Wizard
        public ActionResult Wizard(int step = 1)
        {
            ViewBag.CurrentStep = step;
            ViewBag.CustomerTypes = GetCustomerTypes();
            ViewBag.Genders = GetGenders();
            ViewBag.Provinces = GetProvinces();

            // Use Session for better persistence
            Customer model;
            if (Session["WizardData"] != null)
            {
                model = Session["WizardData"] as Customer;
            }
            else
            {
                // Only create new if no session data exists
                model = new Customer
                {
                    CustomerCode = _customerService.GenerateCustomerCode(),
                    IsActive = true
                };
            }

            return View(model);
        }

        // POST: Customer/Wizard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Wizard(Customer model, int currentStep, string action)
        {
            // Get existing model from session or create new (SINGLE DECLARATION)
            Customer sessionModel;
            if (Session["WizardData"] != null)
            {
                sessionModel = Session["WizardData"] as Customer;
            }
            else
            {
                sessionModel = new Customer
                {
                    CustomerCode = _customerService.GenerateCustomerCode(),
                    IsActive = true
                };
            }

            // Update session model with posted data based on current step
            if (currentStep == 1)
            {
                sessionModel.FirstName = model.FirstName;
                sessionModel.LastName = model.LastName;
                sessionModel.Email = model.Email;
                sessionModel.Phone = model.Phone;
                sessionModel.CustomerType = model.CustomerType;
                sessionModel.DateOfBirth = model.DateOfBirth;
                sessionModel.Gender = model.Gender;
            }
            else if (currentStep == 2)
            {
                sessionModel.Address = model.Address;
                sessionModel.City = model.City;
                sessionModel.Province = model.Province;
                sessionModel.PostalCode = model.PostalCode;
            }

            // Save updated model back to session
            Session["WizardData"] = sessionModel;

            if (action == "Next")
            {
                // Validate current step before moving to next
                if (currentStep == 1)
                {
                    // Validate Step 1 fields
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

                    if (!ModelState.IsValid)
                    {
                        ViewBag.CurrentStep = currentStep;
                        ViewBag.CustomerTypes = GetCustomerTypes();
                        ViewBag.Genders = GetGenders();
                        ViewBag.Provinces = GetProvinces();
                        return View(sessionModel);
                    }
                }
                else if (currentStep == 2)
                {
                    // Validate Step 2 fields
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

                // Move to next step
                return RedirectToAction("Wizard", new { step = currentStep + 1 });
            }
            else if (action == "Previous")
            {
                // Move to previous step (no validation needed, data already saved)
                return RedirectToAction("Wizard", new { step = currentStep - 1 });
            }
            else if (action == "Submit")
            {
                // sessionModel already exists from top of method - NO NEED TO REDECLARE

                if (sessionModel == null)
                {
                    ModelState.AddModelError("", "Session expired. Please start again.");
                    return RedirectToAction("Wizard", new { step = 1 });
                }

                // Clear ModelState and manually validate the complete model
                ModelState.Clear();

                // Validate all required fields
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

                // Check for duplicate customer code
                if (_customerService.CustomerCodeExists(sessionModel.CustomerCode))
                {
                    ModelState.AddModelError("CustomerCode", "This customer code already exists");
                }

                // If validation failed, stay on review step and show errors
                if (!ModelState.IsValid)
                {
                    ViewBag.CurrentStep = 6;
                    ViewBag.CustomerTypes = GetCustomerTypes();
                    ViewBag.Genders = GetGenders();
                    ViewBag.Provinces = GetProvinces();
                    return View(sessionModel);
                }

                // All validation passed - create customer
                try
                {
                    _customerService.CreateCustomer(sessionModel);
                    TempData["SuccessMessage"] = "Customer enrolled successfully! All data validated and saved.";
                    Session.Remove("WizardData"); // Clear session data
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

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if customer code already exists
                    if (_customerService.CustomerCodeExists(customer.CustomerCode))
                    {
                        ModelState.AddModelError("CustomerCode", "This customer code already exists.");
                    }
                    else
                    {
                        _customerService.CreateCustomer(customer);

                        TempData["SuccessMessage"] = "Customer created successfully!";
                        return RedirectToAction("Details", new { id = customer.CustomerID });
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating customer: " + ex.Message);
                }
            }

            // If we got here, something failed, redisplay form
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
                    _customerService.UpdateCustomer(customer);

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

                _customerService.DeleteCustomer(id);
                TempData["SuccessMessage"] = $"Customer '{customer.FullName}' deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting customer: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // Helper methods for dropdown lists
        private SelectList GetCustomerTypes()
        {
            var types = new[]
            {
                new { Value = "Healthcare Professional", Text = "Healthcare Professional" },
                new { Value = "Hospital Administrator", Text = "Hospital Administrator" },
                new { Value = "Medical Student", Text = "Medical Student" },
                new { Value = "Healthcare Compliance Officer", Text = "Healthcare Compliance Officer" }
            };
            return new SelectList(types, "Value", "Text");
        }

        private SelectList GetGenders()
        {
            var genders = new[]
            {
                new { Value = "Male", Text = "Male" },
                new { Value = "Female", Text = "Female" },
                new { Value = "Other", Text = "Other" },
                new { Value = "Prefer not to say", Text = "Prefer not to say" }
            };
            return new SelectList(genders, "Value", "Text");
        }

        private SelectList GetProvinces()
        {
            var provinces = new[]
            {
                new { Value = "Gauteng", Text = "Gauteng" },
                new { Value = "Western Cape", Text = "Western Cape" },
                new { Value = "KwaZulu-Natal", Text = "KwaZulu-Natal" },
                new { Value = "Eastern Cape", Text = "Eastern Cape" },
                new { Value = "Free State", Text = "Free State" },
                new { Value = "Limpopo", Text = "Limpopo" },
                new { Value = "Mpumalanga", Text = "Mpumalanga" },
                new { Value = "North West", Text = "North West" },
                new { Value = "Northern Cape", Text = "Northern Cape" }
            };
            return new SelectList(provinces, "Value", "Text");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_customerService != null)
                {
                    ((CustomerService)_customerService).Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}