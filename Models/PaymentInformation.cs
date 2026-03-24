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
        [Display(Name = "Payment Method Type")]
        public string PaymentMethodType { get; set; }

        [StringLength(100)]
        [Display(Name = "Card Holder Name")]
        public string CardHolderName { get; set; }

        [StringLength(500)]
        [Display(Name = "Card Number")]
        public string EncryptedCardNumber { get; set; }

        [StringLength(4)]
        [Display(Name = "Card Last 4 Digits")]
        public string CardLast4Digits { get; set; }

        [StringLength(50)]
        [Display(Name = "Card Type")]
        public string CardType { get; set; }

        [Display(Name = "Card Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? CardExpiryDate { get; set; }

        [StringLength(100)]
        [Display(Name = "CVV")]
        public string EncryptedCVV { get; set; }

        [StringLength(255)]
        [Display(Name = "Bank Account Number")]
        public string BankAccountNumber { get; set; }

        [StringLength(4)]
        [Display(Name = "Account Last 4 Digits")]
        public string AccountLast4Digits { get; set; }

        [StringLength(100)]
        [Display(Name = "Bank Name")]
        public string BankName { get; set; }

        [StringLength(50)]
        [Display(Name = "Account Type")]
        public string AccountType { get; set; }

        [StringLength(20)]
        [Display(Name = "Branch Code")]
        public string BranchCode { get; set; }

        [StringLength(200)]
        [Display(Name = "Billing Address")]
        public string BillingAddress { get; set; }

        [StringLength(100)]
        [Display(Name = "Billing Suburb")]
        public string BillingSuburb { get; set; }

        [StringLength(100)]
        [Display(Name = "Billing City")]
        public string BillingCity { get; set; }

        [StringLength(10)]
        [Display(Name = "Billing Postal Code")]
        public string BillingPostalCode { get; set; }

        [Display(Name = "Primary Payment Method")]
        public bool IsPrimary { get; set; } = false;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Verified")]
        public bool IsVerified { get; set; } = false;

        [Display(Name = "Verified Date")]
        public DateTime? VerifiedDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        [ForeignKey("CustomerID")]
        public virtual Customer Customer { get; set; }

        [NotMapped]
        public bool IsExpired => CardExpiryDate.HasValue && CardExpiryDate.Value < DateTime.Now;

        [NotMapped]
        public bool IsExpiringSoon => CardExpiryDate.HasValue && CardExpiryDate.Value < DateTime.Now.AddMonths(3) && !IsExpired;
    }
}  // ← Closing brace for namespace