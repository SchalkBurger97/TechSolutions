namespace TechSolutions.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CustomerAndDocumentsRedo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Customers", "IDType", c => c.String(maxLength: 10));
            AddColumn("dbo.Customers", "IDNumber", c => c.String(maxLength: 500));
            AddColumn("dbo.Customers", "PassportNumber", c => c.String(maxLength: 500));
            AlterColumn("dbo.IdentificationDocuments", "DocumentNumber", c => c.String(maxLength: 30));
            DropColumn("dbo.IdentificationDocuments", "IDNumber");
            DropColumn("dbo.IdentificationDocuments", "PassportNumber");
            DropColumn("dbo.IdentificationDocuments", "ProfessionalRegNumber");
        }
        
        public override void Down()
        {
            AddColumn("dbo.IdentificationDocuments", "ProfessionalRegNumber", c => c.String(maxLength: 100));
            AddColumn("dbo.IdentificationDocuments", "PassportNumber", c => c.String(maxLength: 50));
            AddColumn("dbo.IdentificationDocuments", "IDNumber", c => c.String(maxLength: 50));
            AlterColumn("dbo.IdentificationDocuments", "DocumentNumber", c => c.String(maxLength: 100));
            DropColumn("dbo.Customers", "PassportNumber");
            DropColumn("dbo.Customers", "IDNumber");
            DropColumn("dbo.Customers", "IDType");
        }
    }
}
