using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSolutions.Models
{
    public class Report
    {
        [Key]
        public int ReportID { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Report Name")]
        public string ReportName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Stored Procedure")]
        public string StoredProcedureName { get; set; }

        // Comma-separated list of column headers for display
        [StringLength(2000)]
        public string ColumnHeaders { get; set; }

        // JSON string of parameter definitions e.g. [{"Name":"StartDate","Type":"date","Label":"From Date"}]
        [StringLength(2000)]
        public string ParameterDefinitions { get; set; }

        // Comma-separated: Excel,PDF,HTML
        [StringLength(100)]
        public string ExportFormats { get; set; }

        [StringLength(50)]
        public string Category { get; set; }

        [StringLength(50)]
        public string IconClass { get; set; }

        [StringLength(20)]
        public string BadgeVariant { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
