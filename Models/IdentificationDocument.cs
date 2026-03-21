using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSolutions.Models
{
    public class IdentificationDocument
    {
        [Key]
        public int DocumentID { get; set; }

        [Required]
        public int CustomerID { get; set; }

        [StringLength(50)]
        [Display(Name = "ID Number")]
        public string IDNumber { get; set; } // Encrypted

        [StringLength(50)]
        [Display(Name = "Passport Number")]
        public string PassportNumber { get; set; } // Encrypted

        [StringLength(100)]
        [Display(Name = "Professional Registration Number")]
        public string ProfessionalRegNumber { get; set; } // Encrypted (e.g., Medical License, Nursing Council)

        [StringLength(100)]
        [Display(Name = "Issuing Authority")]
        public string IssuingAuthority { get; set; } // e.g., "HPCSA", "SANC"

        [Display(Name = "Issue Date")]
        public DateTime? IssueDate { get; set; }

        [Display(Name = "Expiry Date")]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "Verified")]
        public bool IsVerified { get; set; } = false;

        [Display(Name = "Verified Date")]
        public DateTime? VerifiedDate { get; set; }

        public int? VerifiedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? CreatedBy { get; set; }

        // Navigation property
        [ForeignKey("CustomerID")]
        public virtual Customer Customer { get; set; }
    }
}