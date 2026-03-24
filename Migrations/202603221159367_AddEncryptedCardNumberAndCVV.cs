namespace TechSolutions.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddEncryptedCardNumberAndCVV : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PaymentInformations", "EncryptedCardNumber", c => c.String(maxLength: 500));
            AddColumn("dbo.PaymentInformations", "EncryptedCVV", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PaymentInformations", "EncryptedCVV");
            DropColumn("dbo.PaymentInformations", "EncryptedCardNumber");
        }
    }
}
