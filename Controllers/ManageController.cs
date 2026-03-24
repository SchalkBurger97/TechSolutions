using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TechSolutions.Helpers;
using TechSolutions.Helpers;
using TechSolutions.Models;
using TechSolutions.Services;

namespace TechSolutions.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public ManageController()
        {
        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        private ApplicationRoleManager _roleManager;

        public ApplicationRoleManager RoleManager
        {
            get
            {
                return _roleManager ?? HttpContext.GetOwinContext().Get<ApplicationRoleManager>();
            }
            private set
            {
                _roleManager = value;
            }
        }

        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            var userId = User.Identity.GetUserId();
            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId)
            };
            return View(model);
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // GET: /Manage/AddPhoneNumber
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);
            if (UserManager.SmsService != null)
            {
                var message = new IdentityMessage
                {
                    Destination = model.Number,
                    Body = "Your security code is: " + code
                };
                await UserManager.SmsService.SendAsync(message);
            }
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            }
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (user != null)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        //
        // GET: /Manage/UserRoles
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UserRoles()
        {
            var users = UserManager.Users.ToList();
            var model = new List<UserRolesListViewModel>();

            foreach (var user in users)
            {
                var roles = await UserManager.GetRolesAsync(user.Id);

                // Resolve display name from claims (matches your Index view logic)
                var claims = await UserManager.GetClaimsAsync(user.Id);
                var fullName = claims.FirstOrDefault(c => c.Type == "FullName")?.Value
                               ?? user.Email
                               ?? user.UserName;

                var parts = (fullName ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string initials = parts.Length >= 2
                    ? $"{parts[0][0]}{parts[parts.Length - 1][0]}"
                    : parts.Length == 1 ? parts[0][0].ToString() : "?";

                model.Add(new UserRolesListViewModel
                {
                    UserId = user.Id,
                    UserName = user.Email ?? user.UserName,
                    FullName = fullName,
                    Initials = initials,
                    Roles = roles.ToList()
                });
            }

            return View(model);
        }

        //
        // GET: /Manage/AssignRoles/{userId}
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AssignRoles(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = await UserManager.FindByIdAsync(userId);
            if (user == null) return HttpNotFound();

            var allRoles = RoleManager.Roles.Select(r => r.Name).ToList();
            var userRoles = await UserManager.GetRolesAsync(userId);

            var claims = await UserManager.GetClaimsAsync(userId);
            var fullName = claims.FirstOrDefault(c => c.Type == "FullName")?.Value
                           ?? user.Email ?? user.UserName;

            var model = new AssignRolesViewModel
            {
                UserId = userId,
                UserName = user.Email ?? user.UserName,
                FullName = fullName,
                Roles = allRoles.Select(r => new RoleSelectionItem
                {
                    RoleName = r,
                    IsSelected = userRoles.Contains(r)
                }).ToList()
            };

            return View(model);
        }

        //
        // POST: /Manage/AssignRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AssignRoles(AssignRolesViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await UserManager.FindByIdAsync(model.UserId);
            if (user == null) return HttpNotFound();

            var currentRoles = await UserManager.GetRolesAsync(model.UserId);
            var selectedRoles = model.Roles
                                     .Where(r => r.IsSelected)
                                     .Select(r => r.RoleName)
                                     .ToList();

            // Only touch what changed
            var toRemove = currentRoles.Except(selectedRoles).ToArray();
            var toAdd = selectedRoles.Except(currentRoles).ToArray();

            if (toRemove.Any())
                await UserManager.RemoveFromRolesAsync(model.UserId, toRemove);

            if (toAdd.Any())
                await UserManager.AddToRolesAsync(model.UserId, toAdd);

            TempData["StatusMessage"] = $"Roles updated for {model.FullName ?? model.UserName}.";
            return RedirectToAction("UserRoles");
        }

        // POST: Manage/SeedData
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult SeedData()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var audit = new AuditService(context);
                    int created = 0;

                    // ── Name pools ──
                    var firstNamesF = new[] { "Sarah","Nomvula","Priya","Fatima","Anele","Zandile","Ayesha",
                "Melissa","Nokwanda","Chantelle","Amahle","Ntombi","Busisiwe","Nompumelelo",
                "Zanele","Lerato","Thulisile","Buhle","Khanyisile","Sibusisiwe","Lindie",
                "Sunelle","Wilhelmien","Hanlie","Marita","Annelize","Ronel","Charne","Ilse" };

                    var firstNamesM = new[] { "James","Thabo","Bongani","Sipho","Riaan","Mandla","Lungelo",
                "Musa","Sibonelo","Nhlanhla","Mthokozisi","Dumisani","Zwelakhe","Mduduzi",
                "Phiwayinkosi","Siyanda","Thamsanqa","Mvelase","Deon","Stefan","Adriaan",
                "Heinrich","Francois","Wessel","Gerhard","Andre","Christiaan","Pieter","Johan" };

                    var lastNames = new[] { "Nkosi","VanDerBerg","Molefe","Pillay","Dlamini","Khumalo",
                "Ismail","Zulu","Mokoena","Botha","Mthembu","Cele","Shabalala","Khoza",
                "Erasmus","Joubert","Swanepoel","Pretorius","LeRoux","Jacobs","Venter",
                "DuPlessis","Steyn","Cronje","Rossouw","Badenhorst","VanWyk","Olivier",
                "Sithole","Ndlovu","Mkhize","Hadebe","Buthelezi","Vilakazi","Mahlangu",
                "Ntuli","Gumbi","Zwane","Mabunda","Ngcobo","Radebe","Mhlongo","Ntanzi" };

                    // ── Display-only last names (for FullName, shown in UI) ──
                    var lastNamesDisplay = new[] { "Nkosi","Van der Berg","Molefe","Pillay","Dlamini","Khumalo",
                "Ismail","Zulu","Mokoena","Botha","Mthembu","Cele","Shabalala","Khoza",
                "Erasmus","Joubert","Swanepoel","Pretorius","Le Roux","Jacobs","Venter",
                "Du Plessis","Steyn","Cronje","Rossouw","Badenhorst","Van Wyk","Olivier",
                "Sithole","Ndlovu","Mkhize","Hadebe","Buthelezi","Vilakazi","Mahlangu",
                "Ntuli","Gumbi","Zwane","Mabunda","Ngcobo","Radebe","Mhlongo","Ntanzi" };

                    var streets = new[] { "14 Protea Street","88 Rivonia Road","5 Jacaranda Ave",
                "22 Acacia Avenue","45 Voortrekker Road","7 Musgrave Road","300 Bree Street",
                "100 Church Street","1 Union Buildings Ave","55 Kloof Street","12 Oak Road",
                "33 Loop Street","19 Rose Street","9 Atterbury Road","66 Khumalo Street",
                "8 Stadium Road","77 Smith Street","44 Nkosi Street","29 Sobukwe Road",
                "18 Bhekani Road","3 Beach Road","50 Jan Smuts Drive","200 Grayston Drive" };

                    var cities = new[] { "Sandton","Johannesburg","Pretoria","Cape Town","Durban",
                "Pietermaritzburg","Stellenbosch","Bloemfontein","Port Elizabeth","East London",
                "Polokwane","Nelspruit","Kimberley","Upington","Richards Bay","Empangeni","Westville" };

                    var provinces = new[] { "Gauteng","Gauteng","Gauteng","Western Cape","KwaZulu-Natal",
                "KwaZulu-Natal","Western Cape","Free State","Eastern Cape","Eastern Cape",
                "Limpopo","Mpumalanga","Northern Cape","Northern Cape","KwaZulu-Natal","KwaZulu-Natal","KwaZulu-Natal" };

                    var postalCodes = new[] { "2196","2001","0002","8001","4001","3200","7600","9300",
                "6001","5201","0699","1200","8301","8800","3900","3880","3629" };

                    var types = new[] { "Healthcare Professional","Hospital Administrator",
                "Medical Student","Healthcare Compliance Officer" };

                    var courseNames = new[] {
                "Advanced Healthcare IT Training","Medical Data Management",
                "Healthcare Fundamentals","Corporate Data Governance",
                "Clinical Systems Integration","Patient Data Privacy & Compliance",
                "Healthcare Analytics Essentials","Digital Health Transformation"
            };

                    var courseCodes = new[] {
                "AHIT-2024","MDM-2024","HCF-2024","CDG-2024",
                "CSI-2024","PDPC-2024","HAE-2024","DHT-2024"
            };

                    var bankNames = new[] { "FNB (First National Bank)","Standard Bank","ABSA",
                "Nedbank","Capitec","Discovery Bank" };

                    var rng = new Random();
                    int existing = context.Customers.Count(c => !c.IsDeleted);
                    int toCreate = Math.Max(0, 50 - existing);

                    if (toCreate == 0)
                    {
                        TempData["StatusMessage"] = "Database already has 50 or more customers. No seed data added.";
                        return RedirectToAction("Index");
                    }

                    for (int i = 0; i < toCreate; i++)
                    {
                        bool isFemale = rng.Next(2) == 0;

                        string firstName = isFemale
                            ? firstNamesF[rng.Next(firstNamesF.Length)]
                            : firstNamesM[rng.Next(firstNamesM.Length)];

                        int lastNameIdx = rng.Next(lastNames.Length);
                        string lastNameSlug = lastNames[lastNameIdx];        // no spaces — safe for email
                        string lastNameDisplay = lastNamesDisplay[lastNameIdx]; // spaces preserved — for DB

                        string gender = isFemale ? "Female" : (rng.Next(10) == 0 ? "Other" : "Male");
                        string custType = types[rng.Next(types.Length)];
                        int cityIdx = rng.Next(cities.Length);
                        string city = cities[cityIdx];
                        string province = provinces[Math.Min(cityIdx, provinces.Length - 1)];
                        string postalCode = postalCodes[Math.Min(cityIdx, postalCodes.Length - 1)];

                        string emailFirst = firstName.ToLower().Replace(" ", "");
                        string emailLast = lastNameSlug.ToLower(); // already has no spaces or apostrophes
                        string email = $"{emailFirst}.{emailLast}_{rng.Next(100, 999)}@email.co.za";

                        if (email.Length > 100)
                            email = email.Substring(0, 100);

                        // Vary quality — mostly medium/high with some low
                        int qualityTier = rng.Next(10);
                        decimal quality = qualityTier < 2
                            ? rng.Next(20, 55)      // 20% low quality
                            : qualityTier < 5
                                ? rng.Next(55, 75)  // 30% medium
                                : rng.Next(75, 98); // 50% high

                        decimal risk = quality >= 80 ? rng.Next(5, 25)
                                     : quality >= 60 ? rng.Next(25, 55)
                                     : rng.Next(55, 90);

                        // Vary created date over the past 2 years
                        var createdDate = DateTime.Now.AddDays(-rng.Next(0, 730));

                        string phone = $"0{rng.Next(60, 90)}{rng.Next(1000000, 9999999):D7}";

                        var dob = new DateTime(
                            rng.Next(1970, 2000),
                            rng.Next(1, 13),
                            rng.Next(1, 29)  // 1–28 inclusive, safe for all months
                        );

                        // ── Generate realistic SA ID number (13 digits) ──
                        // Format: YYMMDD GGGG C A Z  (DOB + gender + citizenship + checksum)
                        // We'll use the customer's DOB for the first 6 digits
                        string dobPart = dob.ToString("yyMMdd");
                        string genderNum = isFemale ? rng.Next(0, 5000).ToString("D4")      // 0000–4999 = female
                                                    : rng.Next(5000, 10000).ToString("D4");   // 5000–9999 = male
                        string saIdPlain = $"{dobPart}{genderNum}08{rng.Next(0, 10)}";       // citizenship=0 (SA), 8=fixed, random check

                        // ── Generate realistic passport number ──
                        // SA passport format: A + 8 digits
                        string passportPlain = $"A{rng.Next(10000000, 99999999)}";

                        int idTier = rng.Next(10);
                        string idType = idTier < 8 ? "ID" : idTier < 10 ? "Passport" : null;
                        string encryptedId = idType == "ID" ? EncryptionHelper.EncryptString(saIdPlain) : null;
                        string encryptedPP = idType == "Passport" ? EncryptionHelper.EncryptString(passportPlain) : null;

                        var customer = new Customer
                        {
                            CustomerCode = $"SEED-{(existing + i + 1):D4}",
                            FirstName = firstName,
                            LastName = lastNameDisplay,   
                            Gender = gender,
                            CustomerType = custType,
                            Email = email,             
                            Phone = phone,             
                            Address = streets[rng.Next(streets.Length)],
                            City = city,
                            Province = province,
                            PostalCode = postalCode,        
                            DateOfBirth = dob,               
                            DataQualityScore = quality,
                            RiskScore = risk,
                            IsActive = rng.Next(10) > 1, 
                            IsDeleted = false,
                            CreatedDate = createdDate,
                            CreatedBy = User.Identity.GetUserId(),
                            IDType = idType,
                            IDNumber = encryptedId,
                            PassportNumber = encryptedPP
                        };

                        context.Customers.Add(customer);
                        context.SaveChanges();
                        created++;

                        // ── Add 1-2 enrollments per customer ──
                        int enrollCount = rng.Next(1, 3);
                        for (int e = 0; e < enrollCount; e++)
                        {
                            int courseIdx = rng.Next(courseNames.Length);
                            decimal fee = new[] { 4500m, 6500m, 8500m, 12000m }[rng.Next(4)];
                            string enrolStatus = new[] { "Active", "Active", "Completed", "Withdrawn", "Deferred" }[rng.Next(5)];

                            decimal amountPaid = enrolStatus == "Completed" ? fee
                                               : enrolStatus == "Withdrawn" ? fee * 0.5m
                                               : rng.Next(3) == 0 ? 0m
                                               : rng.Next(2) == 0 ? fee
                                               : fee * (decimal)rng.NextDouble() * 0.9m;
                            amountPaid = Math.Round(amountPaid, 2);

                            string payStatus = amountPaid >= fee ? "Paid"
                                             : amountPaid > 0 ? "Partial"
                                             : "Pending";

                            var enrolDate = createdDate.AddDays(rng.Next(0, 30));
                            var startDate = enrolDate.AddDays(rng.Next(7, 21));
                            var endDate = startDate.AddMonths(rng.Next(2, 6));

                            DateTime? completionDate = null;
                            bool certIssued = false;
                            DateTime? certIssueDate = null;
                            string grade = null;

                            if (enrolStatus == "Completed")
                            {
                                completionDate = endDate.AddDays(-rng.Next(0, 14));
                                certIssued = rng.Next(4) > 0; // 75% get cert
                                if (certIssued)
                                {
                                    certIssueDate = completionDate.Value.AddDays(rng.Next(3, 21));
                                    grade = new[] { "A", "B", "B+", "C", "A-", "Dist." }[rng.Next(6)];
                                }
                            }

                            context.EnrollmentHistories.Add(new EnrollmentHistory
                            {
                                CustomerID = customer.CustomerID,
                                CourseName = courseNames[courseIdx],
                                CourseCode = $"{courseCodes[courseIdx]}-{rng.Next(10, 99)}",
                                EnrollmentDate = enrolDate,
                                CourseStartDate = startDate,
                                CourseEndDate = endDate,
                                CourseFee = fee,
                                AmountPaid = amountPaid,
                                PaymentStatus = payStatus,
                                EnrollmentStatus = enrolStatus,
                                CompletionDate = completionDate,
                                Grade = grade,
                                CertificateIssued = certIssued,
                                CertificateIssueDate = certIssueDate,
                                Notes = null
                            });
                        }
                        
                        // ── Add payment method for ~80% of customers ──
                        if (rng.Next(10) < 8)
                        {
                            bool isCard = rng.Next(3) > 0;
                            var visaCards = new[] { "4111111111111111", "4012888888881881", "4917610000000000" };
                            var mastercardCards = new[] { "5500005555555559", "5105105105105100", "5425233430109903" };
                            var bankAccNumbers = new[] { "1234567890", "9876543210", "4567891230", "7890123456" };

                            string rawCardNumber = null;
                            string rawBankNumber = null;

                            if (isCard)
                            {
                                bool isVisa = rng.Next(2) == 0;
                                rawCardNumber = isVisa
                                    ? visaCards[rng.Next(visaCards.Length)]
                                    : mastercardCards[rng.Next(mastercardCards.Length)];
                            }
                            else
                            {
                                rawBankNumber = bankAccNumbers[rng.Next(bankAccNumbers.Length)];
                            }


                            context.PaymentInformation.Add(new PaymentInformation
                            {
                                CustomerID = customer.CustomerID,
                                PaymentMethodType = isCard ? (rng.Next(2) == 0 ? "Credit Card" : "Debit Card") : "Bank Account",
                                CardType = isCard ? (rng.Next(2) == 0 ? "Visa" : "Mastercard") : null,
                                CardHolderName = isCard ? $"{firstName} {lastNameDisplay}" : null,
                                CardLast4Digits = isCard ? rng.Next(1000, 9999).ToString() : null,
                                CardExpiryDate = isCard ? (DateTime?)DateTime.Now.AddMonths(rng.Next(-6, 36)) : null,
                                EncryptedCardNumber = isCard ? EncryptionHelper.EncryptString(rawCardNumber) : null,
                                BankName = !isCard ? bankNames[rng.Next(bankNames.Length)] : null,
                                AccountLast4Digits = !isCard ? rng.Next(1000, 9999).ToString() : null,
                                BankAccountNumber = !isCard ? EncryptionHelper.EncryptString(rawBankNumber) : null,
                                AccountType = !isCard ? (rng.Next(2) == 0 ? "Cheque Account" : "Savings Account") : null,
                                BranchCode = !isCard ? rng.Next(100000, 999999).ToString() : null,
                                IsPrimary = true,
                                IsActive = true,
                                IsVerified = rng.Next(3) > 0,
                                CreatedDate = createdDate,
                                ModifiedDate = createdDate
                            });
                        }

                        // ── Add medical clearance for ~70% ──
                        if (rng.Next(10) < 7)
                        {
                            string clearStatus = new[] { "Approved", "Approved", "Approved", "Pending", "In Review", "Rejected" }[rng.Next(6)];
                            bool hasTB = rng.Next(4) > 0;
                            bool hasCovid = rng.Next(3) > 0;
                            bool hasBG = rng.Next(3) > 0;
                            bool cleared = clearStatus == "Approved" && hasTB && hasCovid && hasBG;

                            var tbDate = hasTB ? (DateTime?)createdDate.AddDays(-rng.Next(30, 180)) : null;
                            var tbExpiry = tbDate.HasValue ? (DateTime?)tbDate.Value.AddMonths(rng.Next(6, 14)) : null;

                            context.MedicalClearances.Add(new MedicalClearance
                            {
                                CustomerID = customer.CustomerID,
                                ClearanceStatus = clearStatus,
                                HasTBTest = hasTB,
                                TBTestDate = tbDate,
                                TBTestExpiryDate = tbExpiry,
                                TBTestResult = hasTB ? (rng.Next(5) > 0 ? "Negative" : "Positive") : null,
                                HasCOVIDVaccination = hasCovid,
                                CovidVaccinationStatus = hasCovid ? "Fully Vaccinated" : "Not Vaccinated",
                                LastVaccinationDate = hasCovid ? (DateTime?)createdDate.AddDays(-rng.Next(60, 400)) : null,
                                VaccineBrand = hasCovid ? new[] { "Pfizer", "Johnson & Johnson", "Moderna", "AstraZeneca" }[rng.Next(4)] : null,
                                HasBackgroundCheck = hasBG,
                                BackgroundCheckStatus = hasBG ? "Cleared" : "Pending",
                                BackgroundCheckDate = hasBG ? (DateTime?)createdDate.AddDays(-rng.Next(10, 90)) : null,
                                ClearedForOnsiteTraining = cleared,
                                ClearanceNotes = clearStatus == "Rejected" ? "Failed background check screening." : null,
                                CreatedDate = createdDate,
                                ApprovedBy = clearStatus == "Approved" ? User.Identity.GetUserId() : null
                            });
                        }

                        context.SaveChanges();
                    }

                    audit.Log(
                        User.Identity.GetUserId(),
                        User.Identity.Name,
                        "Admin",
                        "Create",
                        "System",
                        null,
                        "Seed Data",
                        $"Admin seeded {created} customers with enrollments, payments and clearances.",
                        null,
                        Request
                    );

                    TempData["StatusMessage"] = $"Successfully seeded {created} customer{(created == 1 ? "" : "s")} with enrollments, payment methods and clearances.";
                }
            }
            catch (DbEntityValidationException ex)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var eve in ex.EntityValidationErrors)
                {
                    sb.AppendLine($"Entity: {eve.Entry.Entity.GetType().Name}");
                    foreach (var ve in eve.ValidationErrors)
                    {
                        sb.AppendLine($"  → {ve.PropertyName}: {ve.ErrorMessage}");
                    }
                }
                TempData["StatusMessage"] = "SEED ERROR: " + sb.ToString();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    msg += " | INNER: " + inner.Message;
                    inner = inner.InnerException;
                }
                TempData["StatusMessage"] = "SEED ERROR: " + msg;
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            // ADD THIS:
            if (disposing && _roleManager != null)
            {
                _roleManager.Dispose();
                _roleManager = null;
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

#endregion
    }
}