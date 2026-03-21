using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSolutions.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        [StringLength(20)]
        public string CustomerCode { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
        [Display(Name = "First Name")]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "First name can only contain letters, spaces, hyphens and apostrophes")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
        [Display(Name = "Last Name")]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens and apostrophes")]
        public string LastName { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(50)]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Customer type is required")]
        [StringLength(50)]
        [Display(Name = "Customer Type")]
        public string CustomerType { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "Phone number must be at least 10 digits")]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Phone number must be 10 digits starting with 0 (e.g., 0123456789)")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Address must be between 5 and 200 characters")]
        [Display(Name = "Street Address")]
        public string Address { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "City must be between 2 and 100 characters")]
        public string City { get; set; }

        [Required(ErrorMessage = "Province is required")]
        [StringLength(100)]
        public string Province { get; set; }

        [Required(ErrorMessage = "Postal code is required")]
        [StringLength(10)]
        [Display(Name = "Postal Code")]
        [RegularExpression(@"^[0-9]{4}$", ErrorMessage = "Postal code must be exactly 4 digits")]
        public string PostalCode { get; set; }

        [Display(Name = "Data Quality Score")]
        public decimal DataQualityScore { get; set; } = 0;

        [Display(Name = "Risk Score")]
        public decimal RiskScore { get; set; } = 0;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Deleted")]
        public bool IsDeleted { get; set; } = false;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? CreatedBy { get; set; }

        [Display(Name = "Modified Date")]
        public DateTime? ModifiedDate { get; set; }

        public int? ModifiedBy { get; set; }

        // Computed property
        [NotMapped]
        public string FullName
        {
            get { return $"{FirstName} {LastName}"; }
        }

        // Navigation properties for related tables
        public virtual ICollection<IdentificationDocument> IdentificationDocuments { get; set; }
        public virtual ICollection<PaymentInformation> PaymentInformation { get; set; }
        public virtual ICollection<MedicalClearance> MedicalClearances { get; set; }
        public virtual ICollection<EnrollmentHistory> EnrollmentHistories { get; set; }
    }
}