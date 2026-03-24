using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSolutions.Models
{
    public class MedicalClearance
    {
        [Key]
        public int ClearanceID { get; set; }

        [Required]
        public int CustomerID { get; set; }

        [StringLength(200)]
        [Display(Name = "Healthcare Facility")]
        public string HealthcareFacility { get; set; } // Where they'll do on-site training

        [StringLength(50)]
        [Display(Name = "Clearance Status")]
        public string ClearanceStatus { get; set; } // Pending, Approved, Rejected

        [Display(Name = "TB Test Date")]
        public DateTime? TBTestDate { get; set; }

        [Display(Name = "TB Test Result")]
        public string TBTestResult { get; set; }

        [Display(Name = "TB Test Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? TBTestExpiryDate { get; set; }

        [Display(Name = "COVID Vaccination Status")]
        public string CovidVaccinationStatus { get; set; } // Fully Vaccinated, Partially, Not Vaccinated

        [Display(Name = "Last Vaccination Date")]
        public DateTime? LastVaccinationDate { get; set; }

        [Display(Name = "Vaccine Brand")]
        [StringLength(100)]
        public string VaccineBrand { get; set; }

        [Display(Name = "Background Check Status")]
        public string BackgroundCheckStatus { get; set; } // Pending, Cleared, Not Cleared

        [Display(Name = "Background Check Date")]
        public DateTime? BackgroundCheckDate { get; set; }

        [Display(Name = "Cleared for On-Site Training")]
        public bool ClearedForOnsiteTraining { get; set; } = false;

        [Display(Name = "Clearance Notes")]
        [StringLength(1000)]
        public string ClearanceNotes { get; set; }

        [Display(Name = "TB Test Completed")]
        public bool HasTBTest { get; set; }

        [Display(Name = "COVID Vaccination Completed")]
        public bool HasCOVIDVaccination { get; set; }

        [Display(Name = "Background Check Completed")]
        public bool HasBackgroundCheck { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        [StringLength(128)]
        public string ApprovedBy { get; set; }

        // Navigation property
        [ForeignKey("CustomerID")]
        public virtual Customer Customer { get; set; }
    }
}