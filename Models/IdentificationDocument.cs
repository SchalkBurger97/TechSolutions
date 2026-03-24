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
        public string DocumentType { get; set; } // Driver's License, Passport, National ID Card, Other

        /// <summary>
        /// Auto-generated system reference number — not a sensitive identity number.
        /// Format: {PREFIX}-{YYYY}-{SEQUENCE:D5}  e.g. DRV-2025-00031
        /// Prefixes: DRV (Driver's License), PSP (Passport), NID (National ID Card), OTH (Other)
        /// This field is NOT encrypted.
        /// </summary>
        [StringLength(30)]
        [Display(Name = "Document Reference")]
        public string DocumentNumber { get; set; }

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

        [StringLength(128)]
        public string VerifiedBy { get; set; }

        [StringLength(500)]
        [Display(Name = "Verification Notes")]
        public string VerificationNotes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }

        [StringLength(128)]
        public string CreatedBy { get; set; }

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
