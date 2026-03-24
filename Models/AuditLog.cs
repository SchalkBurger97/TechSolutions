using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSolutions.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogID { get; set; }

        // Who
        [StringLength(128)]
        public string UserID { get; set; }

        [StringLength(256)]
        public string UserName { get; set; }

        [StringLength(50)]
        public string UserRole { get; set; }

        // What
        [Required]
        [StringLength(20)]
        public string Action { get; set; } // Create, Update, Delete, View, Reveal

        [Required]
        [StringLength(50)]
        public string EntityType { get; set; } // Customer, Document, Payment, Clearance, Enrollment

        public int? EntityID { get; set; }

        [StringLength(200)]
        public string EntityDescription { get; set; } // e.g. "John Smith (CUST-0001)"

        // Summary
        [StringLength(500)]
        public string Summary { get; set; } // e.g. "Updated customer profile"

        // Field-level changes stored as simple string
        // Format: "FieldName: 'OldValue' → 'NewValue'; FieldName2: ..."
        public string FieldChanges { get; set; }

        // Context
        [StringLength(45)]
        public string IPAddress { get; set; }

        [StringLength(500)]
        public string UserAgent { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
