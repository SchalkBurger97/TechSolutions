namespace TechSolutions.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAuditAndRoles : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AuditLogs",
                c => new
                    {
                        AuditLogID = c.Int(nullable: false, identity: true),
                        UserID = c.String(maxLength: 128),
                        UserName = c.String(maxLength: 256),
                        UserRole = c.String(maxLength: 50),
                        Action = c.String(nullable: false, maxLength: 20),
                        EntityType = c.String(nullable: false, maxLength: 50),
                        EntityID = c.Int(),
                        EntityDescription = c.String(maxLength: 200),
                        Summary = c.String(maxLength: 500),
                        FieldChanges = c.String(),
                        IPAddress = c.String(maxLength: 45),
                        UserAgent = c.String(maxLength: 500),
                        Timestamp = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.AuditLogID);

            CreateIndex("dbo.AuditLogs", "EntityType");
            CreateIndex("dbo.AuditLogs", "EntityID");
            CreateIndex("dbo.AuditLogs", "UserID");
            CreateIndex("dbo.AuditLogs", "Timestamp");

            Sql(@"
                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Admin')
                    INSERT INTO AspNetRoles (Id, Name) VALUES (NEWID(), 'Admin');
 
                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Employee')
                    INSERT INTO AspNetRoles (Id, Name) VALUES (NEWID(), 'Employee');
 
                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Auditor')
                    INSERT INTO AspNetRoles (Id, Name) VALUES (NEWID(), 'Auditor');
            ");

            // ── Audit log stored procedure report ──
            Sql(@"
                CREATE PROCEDURE [dbo].[rpt_AuditLog]
                    @StartDate   DATE         = NULL,
                    @EndDate     DATE         = NULL,
                    @Action      NVARCHAR(20) = 'All',
                    @EntityType  NVARCHAR(50) = 'All',
                    @UserName    NVARCHAR(256)= NULL
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT
                        FORMAT(a.Timestamp, 'dd MMM yyyy HH:mm:ss') AS [Timestamp],
                        a.UserName                                   AS [User],
                        a.UserRole                                   AS [Role],
                        a.Action                                     AS [Action],
                        a.EntityType                                 AS [Module],
                        ISNULL(CAST(a.EntityID AS NVARCHAR), '-')   AS [Record ID],
                        ISNULL(a.EntityDescription, '-')             AS [Record],
                        ISNULL(a.Summary, '-')                       AS [Summary],
                        ISNULL(a.FieldChanges, '-')                  AS [Field Changes],
                        ISNULL(a.IPAddress, '-')                     AS [IP Address]
                    FROM dbo.AuditLogs a
                    WHERE
                        (@StartDate  IS NULL OR CAST(a.Timestamp AS DATE) >= @StartDate)
                        AND (@EndDate IS NULL OR CAST(a.Timestamp AS DATE) <= @EndDate)
                        AND (@Action  = 'All'  OR a.Action     = @Action)
                        AND (@EntityType = 'All' OR a.EntityType = @EntityType)
                        AND (@UserName IS NULL OR a.UserName LIKE '%' + @UserName + '%')
                    ORDER BY a.Timestamp DESC;
                END
            ");

            // ── Seed the audit log report into Reports table ──
            Sql(@"
                IF NOT EXISTS (SELECT 1 FROM dbo.Reports WHERE StoredProcedureName = 'rpt_AuditLog')
                INSERT INTO dbo.Reports
                    (ReportName, Description, StoredProcedureName, ColumnHeaders,
                     ParameterDefinitions, ExportFormats, Category, IconClass, BadgeVariant, SortOrder, IsActive, CreatedDate)
                VALUES (
                    'Audit Log Report',
                    'Full system audit trail showing all create, update, delete and sensitive data access events with field-level change tracking.',
                    'rpt_AuditLog',
                    'Timestamp,User,Role,Action,Module,Record ID,Record,Summary,Field Changes,IP Address',
                    '[{""Name"":""StartDate"",""Type"":""date"",""Label"":""From Date"",""Default"":""""},{""Name"":""EndDate"",""Type"":""date"",""Label"":""To Date"",""Default"":""""},{""Name"":""Action"",""Type"":""select"",""Label"":""Action"",""Options"":""All,Create,Update,Delete,View,Reveal"",""Default"":""All""},{""Name"":""EntityType"",""Type"":""select"",""Label"":""Module"",""Options"":""All,Customer,Document,Payment,Clearance,Enrollment"",""Default"":""All""},{""Name"":""UserName"",""Type"":""text"",""Label"":""User"",""Default"":""""}]',
                    'Excel,PDF,HTML',
                    'Compliance',
                    'fa-shield-halved',
                    'danger',
                    7, 1, GETDATE()
                );
            ");

        }
        
        public override void Down()
        {
            Sql("DROP PROCEDURE IF EXISTS [dbo].[rpt_AuditLog]");
            Sql("DELETE FROM dbo.Reports WHERE StoredProcedureName = 'rpt_AuditLog'");
            Sql("DELETE FROM AspNetRoles WHERE Name IN ('Admin','Employee','Auditor')");
            DropIndex("dbo.AuditLogs", new[] { "Timestamp" });
            DropIndex("dbo.AuditLogs", new[] { "UserID" });
            DropIndex("dbo.AuditLogs", new[] { "EntityID" });
            DropIndex("dbo.AuditLogs", new[] { "EntityType" });
            DropTable("dbo.AuditLogs");
        }
    }
}
