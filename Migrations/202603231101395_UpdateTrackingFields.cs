namespace TechSolutions.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateTrackingFields : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Customers", "CreatedBy", c => c.String(maxLength: 128));
            AlterColumn("dbo.Customers", "ModifiedBy", c => c.String(maxLength: 128));
            AlterColumn("dbo.IdentificationDocuments", "VerifiedBy", c => c.String(maxLength: 128));
            AlterColumn("dbo.IdentificationDocuments", "CreatedBy", c => c.String(maxLength: 128));
            AlterColumn("dbo.MedicalClearances", "ApprovedBy", c => c.String(maxLength: 128));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.MedicalClearances", "ApprovedBy", c => c.Int());
            AlterColumn("dbo.IdentificationDocuments", "CreatedBy", c => c.Int());
            AlterColumn("dbo.IdentificationDocuments", "VerifiedBy", c => c.Int());
            AlterColumn("dbo.Customers", "ModifiedBy", c => c.Int());
            AlterColumn("dbo.Customers", "CreatedBy", c => c.Int());
        }
    }
}
