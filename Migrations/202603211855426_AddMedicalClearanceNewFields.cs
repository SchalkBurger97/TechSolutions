namespace TechSolutions.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMedicalClearanceNewFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.MedicalClearances", "TBTestExpiryDate", c => c.DateTime());
            AddColumn("dbo.MedicalClearances", "VaccineBrand", c => c.String(maxLength: 100));
            AddColumn("dbo.MedicalClearances", "HasTBTest", c => c.Boolean(nullable: false));
            AddColumn("dbo.MedicalClearances", "HasCOVIDVaccination", c => c.Boolean(nullable: false));
            AddColumn("dbo.MedicalClearances", "HasBackgroundCheck", c => c.Boolean(nullable: false));
            AlterColumn("dbo.Customers", "CustomerCode", c => c.String(maxLength: 20));
            AlterColumn("dbo.Customers", "FirstName", c => c.String(nullable: false, maxLength: 50));
            AlterColumn("dbo.Customers", "LastName", c => c.String(nullable: false, maxLength: 50));
            AlterColumn("dbo.Customers", "Gender", c => c.String(maxLength: 50));
            AlterColumn("dbo.Customers", "Email", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.Customers", "Address", c => c.String(nullable: false, maxLength: 200));
            AlterColumn("dbo.Customers", "City", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.Customers", "Province", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.Customers", "PostalCode", c => c.String(nullable: false, maxLength: 10));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Customers", "PostalCode", c => c.String(maxLength: 20));
            AlterColumn("dbo.Customers", "Province", c => c.String(maxLength: 100));
            AlterColumn("dbo.Customers", "City", c => c.String(maxLength: 100));
            AlterColumn("dbo.Customers", "Address", c => c.String(maxLength: 200));
            AlterColumn("dbo.Customers", "Email", c => c.String(nullable: false, maxLength: 255));
            AlterColumn("dbo.Customers", "Gender", c => c.String(maxLength: 20));
            AlterColumn("dbo.Customers", "LastName", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.Customers", "FirstName", c => c.String(nullable: false, maxLength: 100));
            AlterColumn("dbo.Customers", "CustomerCode", c => c.String(nullable: false, maxLength: 20));
            DropColumn("dbo.MedicalClearances", "HasBackgroundCheck");
            DropColumn("dbo.MedicalClearances", "HasCOVIDVaccination");
            DropColumn("dbo.MedicalClearances", "HasTBTest");
            DropColumn("dbo.MedicalClearances", "VaccineBrand");
            DropColumn("dbo.MedicalClearances", "TBTestExpiryDate");
        }
    }
}
