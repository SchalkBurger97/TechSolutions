using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSolutions.Models
{
    public class PaymentInformation
    {
        [Key]
        public int PaymentID { get; set; }

        [Required]
        public int CustomerID { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } // Credit Card, Bank Transfer, etc.

        [StringLength(4)]
        [Display(Name = "Card Last 4 Digits")]
        public string CardLast4Digits { get; set; } // Only store last 4 digits!

        [StringLength(50)]
        [Display(Name = "Card Type")]
        public string CardType { get; set; } // Visa, Mastercard, etc.

        [StringLength(100)]
        [Display(Name = "Bank Account Number")]
        public string BankAccountNumber { get; set; } // Encrypted

        [StringLength(100)]
        [Display(Name = "Bank Name")]
        public string BankName { get; set; }

        [StringLength(200)]
        [Display(Name = "Billing Address")]
        public string BillingAddress { get; set; }

        [StringLength(100)]
        [Display(Name = "Billing City")]
        public string BillingCity { get; set; }

        [StringLength(20)]
        [Display(Name = "Billing Postal Code")]
        public string BillingPostalCode { get; set; }

        [Display(Name = "Primary Payment Method")]
        public bool IsPrimary { get; set; } = false;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        // Navigation property
        [ForeignKey("CustomerID")]
        public virtual Customer Customer { get; set; }
    }
}