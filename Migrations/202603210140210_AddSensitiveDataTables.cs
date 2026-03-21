namespace TechSolutions.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSensitiveDataTables : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.EnrollmentHistories",
                c => new
                    {
                        EnrollmentID = c.Int(nullable: false, identity: true),
                        CustomerID = c.Int(nullable: false),
                        CourseName = c.String(nullable: false, maxLength: 200),
                        CourseCode = c.String(maxLength: 50),
                        EnrollmentDate = c.DateTime(nullable: false),
                        CourseStartDate = c.DateTime(),
                        CourseEndDate = c.DateTime(),
                        CourseFee = c.Decimal(nullable: false, precision: 10, scale: 2),
                        AmountPaid = c.Decimal(nullable: false, precision: 10, scale: 2),
                        PaymentStatus = c.String(maxLength: 50),
                        EnrollmentStatus = c.String(maxLength: 50),
                        CompletionDate = c.DateTime(),
                        Grade = c.String(maxLength: 10),
                        CertificateIssued = c.Boolean(nullable: false),
                        CertificateIssueDate = c.DateTime(),
                        Notes = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.EnrollmentID)
                .ForeignKey("dbo.Customers", t => t.CustomerID, cascadeDelete: true)
                .Index(t => t.CustomerID);
            
            CreateTable(
                "dbo.IdentificationDocuments",
                c => new
                    {
                        DocumentID = c.Int(nullable: false, identity: true),
                        CustomerID = c.Int(nullable: false),
                        IDNumber = c.String(maxLength: 50),
                        PassportNumber = c.String(maxLength: 50),
                        ProfessionalRegNumber = c.String(maxLength: 100),
                        IssuingAuthority = c.String(maxLength: 100),
                        IssueDate = c.DateTime(),
                        ExpiryDate = c.DateTime(),
                        IsVerified = c.Boolean(nullable: false),
                        VerifiedDate = c.DateTime(),
                        VerifiedBy = c.Int(),
                        CreatedDate = c.DateTime(nullable: false),
                        CreatedBy = c.Int(),
                    })
                .PrimaryKey(t => t.DocumentID)
                .ForeignKey("dbo.Customers", t => t.CustomerID, cascadeDelete: true)
                .Index(t => t.CustomerID);
            
            CreateTable(
                "dbo.MedicalClearances",
                c => new
                    {
                        ClearanceID = c.Int(nullable: false, identity: true),
                        CustomerID = c.Int(nullable: false),
                        HealthcareFacility = c.String(maxLength: 200),
                        ClearanceStatus = c.String(maxLength: 50),
                        TBTestDate = c.DateTime(),
                        TBTestResult = c.String(),
                        CovidVaccinationStatus = c.String(),
                        LastVaccinationDate = c.DateTime(),
                        BackgroundCheckStatus = c.String(),
                        BackgroundCheckDate = c.DateTime(),
                        ClearedForOnsiteTraining = c.Boolean(nullable: false),
                        ClearanceNotes = c.String(maxLength: 1000),
                        CreatedDate = c.DateTime(nullable: false),
                        ModifiedDate = c.DateTime(),
                        ApprovedBy = c.Int(),
                    })
                .PrimaryKey(t => t.ClearanceID)
                .ForeignKey("dbo.Customers", t => t.CustomerID, cascadeDelete: true)
                .Index(t => t.CustomerID);
            
            CreateTable(
                "dbo.PaymentInformations",
                c => new
                    {
                        PaymentID = c.Int(nullable: false, identity: true),
                        CustomerID = c.Int(nullable: false),
                        PaymentMethod = c.String(nullable: false, maxLength: 50),
                        CardLast4Digits = c.String(maxLength: 4),
                        CardType = c.String(maxLength: 50),
                        BankAccountNumber = c.String(maxLength: 100),
                        BankName = c.String(maxLength: 100),
                        BillingAddress = c.String(maxLength: 200),
                        BillingCity = c.String(maxLength: 100),
                        BillingPostalCode = c.String(maxLength: 20),
                        IsPrimary = c.Boolean(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        ModifiedDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.PaymentID)
                .ForeignKey("dbo.Customers", t => t.CustomerID, cascadeDelete: true)
                .Index(t => t.CustomerID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PaymentInformations", "CustomerID", "dbo.Customers");
            DropForeignKey("dbo.MedicalClearances", "CustomerID", "dbo.Customers");
            DropForeignKey("dbo.IdentificationDocuments", "CustomerID", "dbo.Customers");
            DropForeignKey("dbo.EnrollmentHistories", "CustomerID", "dbo.Customers");
            DropIndex("dbo.PaymentInformations", new[] { "CustomerID" });
            DropIndex("dbo.MedicalClearances", new[] { "CustomerID" });
            DropIndex("dbo.IdentificationDocuments", new[] { "CustomerID" });
            DropIndex("dbo.EnrollmentHistories", new[] { "CustomerID" });
            DropTable("dbo.PaymentInformations");
            DropTable("dbo.MedicalClearances");
            DropTable("dbo.IdentificationDocuments");
            DropTable("dbo.EnrollmentHistories");
        }
    }
}
