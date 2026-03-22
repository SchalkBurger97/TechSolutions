namespace TechSolutions.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIdentificationDocumentFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.IdentificationDocuments", "DocumentType", c => c.String(nullable: false, maxLength: 50));
            AddColumn("dbo.IdentificationDocuments", "DocumentNumber", c => c.String(maxLength: 100));
            AddColumn("dbo.IdentificationDocuments", "IssuingCountry", c => c.String(maxLength: 100));
            AddColumn("dbo.IdentificationDocuments", "VerificationNotes", c => c.String(maxLength: 500));
            AddColumn("dbo.IdentificationDocuments", "ModifiedDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.IdentificationDocuments", "ModifiedDate");
            DropColumn("dbo.IdentificationDocuments", "VerificationNotes");
            DropColumn("dbo.IdentificationDocuments", "IssuingCountry");
            DropColumn("dbo.IdentificationDocuments", "DocumentNumber");
            DropColumn("dbo.IdentificationDocuments", "DocumentType");
        }
    }
}
