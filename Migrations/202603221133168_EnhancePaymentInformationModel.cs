namespace TechSolutions.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EnhancePaymentInformationModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PaymentInformations", "PaymentMethodType", c => c.String(nullable: false, maxLength: 50));
            AddColumn("dbo.PaymentInformations", "CardHolderName", c => c.String(maxLength: 100));
            AddColumn("dbo.PaymentInformations", "CardExpiryDate", c => c.DateTime());
            AddColumn("dbo.PaymentInformations", "AccountLast4Digits", c => c.String(maxLength: 4));
            AddColumn("dbo.PaymentInformations", "AccountType", c => c.String(maxLength: 50));
            AddColumn("dbo.PaymentInformations", "BranchCode", c => c.String(maxLength: 20));
            AddColumn("dbo.PaymentInformations", "BillingSuburb", c => c.String(maxLength: 100));
            AddColumn("dbo.PaymentInformations", "IsVerified", c => c.Boolean(nullable: false));
            AddColumn("dbo.PaymentInformations", "VerifiedDate", c => c.DateTime());
            AlterColumn("dbo.PaymentInformations", "BankAccountNumber", c => c.String(maxLength: 255));
            AlterColumn("dbo.PaymentInformations", "BillingPostalCode", c => c.String(maxLength: 10));
            DropColumn("dbo.PaymentInformations", "PaymentMethod");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PaymentInformations", "PaymentMethod", c => c.String(nullable: false, maxLength: 50));
            AlterColumn("dbo.PaymentInformations", "BillingPostalCode", c => c.String(maxLength: 20));
            AlterColumn("dbo.PaymentInformations", "BankAccountNumber", c => c.String(maxLength: 100));
            DropColumn("dbo.PaymentInformations", "VerifiedDate");
            DropColumn("dbo.PaymentInformations", "IsVerified");
            DropColumn("dbo.PaymentInformations", "BillingSuburb");
            DropColumn("dbo.PaymentInformations", "BranchCode");
            DropColumn("dbo.PaymentInformations", "AccountType");
            DropColumn("dbo.PaymentInformations", "AccountLast4Digits");
            DropColumn("dbo.PaymentInformations", "CardExpiryDate");
            DropColumn("dbo.PaymentInformations", "CardHolderName");
            DropColumn("dbo.PaymentInformations", "PaymentMethodType");
        }
    }
}
