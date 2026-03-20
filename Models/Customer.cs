using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace TechSolutions.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        [Required(ErrorMessage = "Customer code is required")]
        [StringLength(20)]
        public string CustomerCode { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string Gender { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Customer Type")]
        public string CustomerType { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(100)]
        public string Province { get; set; }

        [StringLength(20)]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }

        [Display(Name = "Data Quality Score")]
        public decimal DataQualityScore { get; set; } = 100;

        [Display(Name = "Risk Score")]
        public decimal RiskScore { get; set; } = 0;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? CreatedBy { get; set; }

        [Display(Name = "Modified Date")]
        public DateTime? ModifiedDate { get; set; }

        public int? ModifiedBy { get; set; }
        public string FullName
        {
            get { return FirstName + " " + LastName; }
        }

    }
}