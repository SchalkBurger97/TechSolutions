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

        [Required]
        [StringLength(50)]
        [Display(Name = "Document Type")]
        public string DocumentType { get; set; } // Driver's License, Passport, etc.

        [StringLength(100)]
        [Display(Name = "Document Number")]
        public string DocumentNumber { get; set; } // Encrypted - can be ID, Passport, or any number

        [StringLength(50)]
        [Display(Name = "ID Number")]
        public string IDNumber { get; set; } // Encrypted

        [StringLength(50)]
        [Display(Name = "Passport Number")]
        public string PassportNumber { get; set; } // Encrypted

        [StringLength(100)]
        [Display(Name = "Professional Registration Number")]
        public string ProfessionalRegNumber { get; set; } // Encrypted

        [StringLength(100)]
        [Display(Name = "Issuing Authority")]
        public string IssuingAuthority { get; set; }

        [StringLength(100)]
        [Display(Name = "Issuing Country")]
        public string IssuingCountry { get; set; }

        [Display(Name = "Issue Date")]
        public DateTime? IssueDate { get; set; }

        [Display(Name = "Expiry Date")]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "Verified")]
        public bool IsVerified { get; set; } = false;

        [Display(Name = "Verified Date")]
        public DateTime? VerifiedDate { get; set; }

        public int? VerifiedBy { get; set; }

        [StringLength(500)]
        [Display(Name = "Verification Notes")]
        public string VerificationNotes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
        public int? CreatedBy { get; set; }

        [StringLength(500)]
        [Display(Name = "Document File Path")]
        public string DocumentFilePath { get; set; }

        [StringLength(200)]
        [Display(Name = "Original File Name")]
        public string OriginalFileName { get; set; }

        [StringLength(50)]
        [Display(Name = "File Type")]
        public string FileType { get; set; }

        public long? FileSizeBytes { get; set; }

        // Navigation property
        [ForeignKey("CustomerID")]
        public virtual Customer Customer { get; set; }
    }
}