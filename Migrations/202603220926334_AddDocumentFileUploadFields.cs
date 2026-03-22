namespace TechSolutions.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDocumentFileUploadFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.IdentificationDocuments", "DocumentFilePath", c => c.String(maxLength: 500));
            AddColumn("dbo.IdentificationDocuments", "OriginalFileName", c => c.String(maxLength: 200));
            AddColumn("dbo.IdentificationDocuments", "FileType", c => c.String(maxLength: 50));
            AddColumn("dbo.IdentificationDocuments", "FileSizeBytes", c => c.Long());
        }
        
        public override void Down()
        {
            DropColumn("dbo.IdentificationDocuments", "FileSizeBytes");
            DropColumn("dbo.IdentificationDocuments", "FileType");
            DropColumn("dbo.IdentificationDocuments", "OriginalFileName");
            DropColumn("dbo.IdentificationDocuments", "DocumentFilePath");
        }
    }
}
