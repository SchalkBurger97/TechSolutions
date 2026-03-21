using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSolutions.Models
{
    public class EnrollmentHistory
    {
        [Key]
        public int EnrollmentID { get; set; }

        [Required]
        public int CustomerID { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Course Name")]
        public string CourseName { get; set; }

        [StringLength(50)]
        [Display(Name = "Course Code")]
        public string CourseCode { get; set; }

        [Display(Name = "Enrollment Date")]
        public DateTime EnrollmentDate { get; set; } = DateTime.Now;

        [Display(Name = "Course Start Date")]
        public DateTime? CourseStartDate { get; set; }

        [Display(Name = "Course End Date")]
        public DateTime? CourseEndDate { get; set; }

        [Display(Name = "Course Fee")]
        public decimal CourseFee { get; set; }

        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; } = 0;

        [Display(Name = "Payment Status")]
        [StringLength(50)]
        public string PaymentStatus { get; set; } // Pending, Paid, Partial, Refunded

        [Display(Name = "Enrollment Status")]
        [StringLength(50)]
        public string EnrollmentStatus { get; set; } // Active, Completed, Withdrawn, Deferred

        [Display(Name = "Completion Date")]
        public DateTime? CompletionDate { get; set; }

        [Display(Name = "Grade/Score")]
        [StringLength(10)]
        public string Grade { get; set; }

        [Display(Name = "Certificate Issued")]
        public bool CertificateIssued { get; set; } = false;

        [Display(Name = "Certificate Issue Date")]
        public DateTime? CertificateIssueDate { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation property
        [ForeignKey("CustomerID")]
        public virtual Customer Customer { get; set; }
    }
}