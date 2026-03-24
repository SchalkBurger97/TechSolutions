namespace TechSolutions.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateSystemReports : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Reports",
                c => new
                    {
                        ReportID = c.Int(nullable: false, identity: true),
                        ReportName = c.String(nullable: false, maxLength: 100),
                        Description = c.String(maxLength: 500),
                        StoredProcedureName = c.String(nullable: false, maxLength: 100),
                        ColumnHeaders = c.String(maxLength: 2000),
                        ParameterDefinitions = c.String(maxLength: 2000),
                        ExportFormats = c.String(maxLength: 100),
                        Category = c.String(maxLength: 50),
                        IconClass = c.String(maxLength: 50),
                        BadgeVariant = c.String(maxLength: 20),
                        SortOrder = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ReportID);

            // STORED PROCEDURES FOR THE REPORT PORTAL

            // ── 1. Customer Quality Report ──
            Sql(@"
                CREATE PROCEDURE [dbo].[rpt_CustomerQuality]
                    @MinScore   INT  = 0,
                    @MaxScore   INT  = 100,
                    @Status     NVARCHAR(10) = 'All'
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT
                        c.CustomerCode          AS [Customer Code],
                        c.FirstName + ' ' + c.LastName AS [Full Name],
                        c.Email                 AS [Email],
                        c.Phone                 AS [Phone],
                        c.CustomerType          AS [Customer Type],
                        CAST(c.DataQualityScore AS DECIMAL(5,1)) AS [Quality Score (%)],
                        CAST(c.RiskScore AS DECIMAL(5,1))        AS [Risk Score],
                        CASE WHEN c.IsActive = 1 THEN 'Active' ELSE 'Inactive' END AS [Status],
                        FORMAT(c.CreatedDate, 'dd MMM yyyy')     AS [Enrolled Date]
                    FROM dbo.Customers c
                    WHERE c.IsDeleted = 0
                      AND c.DataQualityScore >= @MinScore
                      AND c.DataQualityScore <= @MaxScore
                      AND (@Status = 'All'
                           OR (@Status = 'Active'   AND c.IsActive = 1)
                           OR (@Status = 'Inactive' AND c.IsActive = 0))
                    ORDER BY c.DataQualityScore ASC, c.LastName ASC;
                END
            ");

            // ── 2. Compliance Status Report ──
            Sql(@"
                CREATE PROCEDURE [dbo].[rpt_ComplianceStatus]
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT
                        c.CustomerCode          AS [Customer Code],
                        c.FirstName + ' ' + c.LastName AS [Full Name],
                        c.CustomerType          AS [Type],
                        -- Documents
                        CASE WHEN EXISTS (
                            SELECT 1 FROM dbo.IdentificationDocuments d
                            WHERE d.CustomerID = c.CustomerID
                        ) THEN 'Yes' ELSE 'No' END                     AS [Has ID Doc],
                        CASE WHEN EXISTS (
                            SELECT 1 FROM dbo.IdentificationDocuments d
                            WHERE d.CustomerID = c.CustomerID AND d.IsVerified = 1
                        ) THEN 'Verified' ELSE 'Unverified/None' END    AS [Doc Status],
                        CASE WHEN EXISTS (
                            SELECT 1 FROM dbo.IdentificationDocuments d
                            WHERE d.CustomerID = c.CustomerID AND d.ExpiryDate < GETDATE()
                        ) THEN 'EXPIRED' ELSE 'OK' END                  AS [Doc Expiry],
                        -- Payments
                        CASE WHEN EXISTS (
                            SELECT 1 FROM dbo.PaymentInformation p
                            WHERE p.CustomerID = c.CustomerID AND p.IsActive = 1
                        ) THEN 'Yes' ELSE 'No' END                     AS [Has Payment],
                        CASE WHEN EXISTS (
                            SELECT 1 FROM dbo.PaymentInformation p
                            WHERE p.CustomerID = c.CustomerID AND p.IsActive = 1
                              AND p.CardExpiryDate < GETDATE()
                        ) THEN 'EXPIRED' ELSE 'OK' END                  AS [Payment Expiry],
                        -- Clearance
                        ISNULL((
                            SELECT TOP 1 mc.ClearanceStatus
                            FROM dbo.MedicalClearances mc
                            WHERE mc.CustomerID = c.CustomerID
                            ORDER BY mc.ClearanceID DESC
                        ), 'None')                                       AS [Clearance Status],
                        CAST(c.DataQualityScore AS DECIMAL(5,1))        AS [Quality Score (%)]
                    FROM dbo.Customers c
                    WHERE c.IsDeleted = 0
                    ORDER BY c.LastName ASC;
                END
            ");

            // ── 3. Enrollment & Revenue Report ──
            Sql(@"
                CREATE PROCEDURE [dbo].[rpt_EnrollmentRevenue]
                    @StartDate  DATE = NULL,
                    @EndDate    DATE = NULL,
                    @Status     NVARCHAR(20) = 'All'
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT
                        c.CustomerCode              AS [Customer Code],
                        c.FirstName + ' ' + c.LastName AS [Customer Name],
                        e.CourseName                AS [Course],
                        e.CourseCode                AS [Course Code],
                        FORMAT(e.EnrollmentDate, 'dd MMM yyyy') AS [Enrolled],
                        e.EnrollmentStatus          AS [Status],
                        FORMAT(e.CourseFee, 'N2')   AS [Course Fee (R)],
                        FORMAT(e.AmountPaid, 'N2')  AS [Amount Paid (R)],
                        FORMAT(e.CourseFee - e.AmountPaid, 'N2') AS [Outstanding (R)],
                        e.PaymentStatus             AS [Payment Status],
                        CASE WHEN e.CertificateIssued = 1 THEN 'Yes' ELSE 'No' END AS [Certificate Issued],
                        ISNULL(FORMAT(e.CompletionDate, 'dd MMM yyyy'), '-') AS [Completion Date]
                    FROM dbo.EnrollmentHistories e
                    INNER JOIN dbo.Customers c ON c.CustomerID = e.CustomerID
                    WHERE c.IsDeleted = 0
                      AND (@StartDate IS NULL OR e.EnrollmentDate >= @StartDate)
                      AND (@EndDate   IS NULL OR e.EnrollmentDate <= @EndDate)
                      AND (@Status = 'All' OR e.EnrollmentStatus = @Status)
                    ORDER BY e.EnrollmentDate DESC;
                END
            ");

            // ── 4. Expiry Alert Report ──
            Sql(@"
                CREATE PROCEDURE [dbo].[rpt_ExpiryAlerts]
                    @DaysAhead INT = 90
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DECLARE @AlertDate DATE = DATEADD(DAY, @DaysAhead, GETDATE());

                    -- Expired/expiring documents
                    SELECT
                        'ID Document'               AS [Alert Type],
                        c.CustomerCode              AS [Customer Code],
                        c.FirstName + ' ' + c.LastName AS [Customer Name],
                        d.DocumentType              AS [Detail],
                        FORMAT(d.ExpiryDate, 'dd MMM yyyy') AS [Expiry Date],
                        CASE
                            WHEN d.ExpiryDate < GETDATE() THEN 'EXPIRED'
                            WHEN d.ExpiryDate <= @AlertDate THEN 'Expiring Soon'
                            ELSE 'OK'
                        END                         AS [Status],
                        DATEDIFF(DAY, GETDATE(), d.ExpiryDate) AS [Days Remaining]
                    FROM dbo.IdentificationDocuments d
                    INNER JOIN dbo.Customers c ON c.CustomerID = d.CustomerID
                    WHERE c.IsDeleted = 0
                      AND d.ExpiryDate IS NOT NULL
                      AND d.ExpiryDate <= @AlertDate

                    UNION ALL

                    -- Expired/expiring payment methods
                    SELECT
                        'Payment Method'            AS [Alert Type],
                        c.CustomerCode              AS [Customer Code],
                        c.FirstName + ' ' + c.LastName AS [Customer Name],
                        ISNULL(p.CardType, p.BankName) + ' ending ' + ISNULL(p.CardLast4Digits, p.AccountLast4Digits) AS [Detail],
                        FORMAT(p.CardExpiryDate, 'dd MMM yyyy') AS [Expiry Date],
                        CASE
                            WHEN p.CardExpiryDate < GETDATE() THEN 'EXPIRED'
                            WHEN p.CardExpiryDate <= @AlertDate THEN 'Expiring Soon'
                            ELSE 'OK'
                        END                         AS [Status],
                        DATEDIFF(DAY, GETDATE(), p.CardExpiryDate) AS [Days Remaining]
                    FROM dbo.PaymentInformation p
                    INNER JOIN dbo.Customers c ON c.CustomerID = p.CustomerID
                    WHERE c.IsDeleted = 0
                      AND p.IsActive = 1
                      AND p.CardExpiryDate IS NOT NULL
                      AND p.CardExpiryDate <= @AlertDate

                    ORDER BY [Days Remaining] ASC;
                END
            ");

            // ── 5. Certificate Report ──
            Sql(@"
                CREATE PROCEDURE [dbo].[rpt_Certificates]
                    @StartDate DATE = NULL,
                    @EndDate   DATE = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT
                        c.CustomerCode              AS [Customer Code],
                        c.FirstName + ' ' + c.LastName AS [Customer Name],
                        c.Email                     AS [Email],
                        e.CourseName                AS [Course],
                        e.CourseCode                AS [Course Code],
                        ISNULL(e.Grade, '-')        AS [Grade],
                        FORMAT(e.CompletionDate, 'dd MMM yyyy')      AS [Completion Date],
                        FORMAT(e.CertificateIssueDate, 'dd MMM yyyy') AS [Certificate Date],
                        FORMAT(e.CourseFee, 'N2')   AS [Course Fee (R)]
                    FROM dbo.EnrollmentHistories e
                    INNER JOIN dbo.Customers c ON c.CustomerID = e.CustomerID
                    WHERE c.IsDeleted = 0
                      AND e.CertificateIssued = 1
                      AND (@StartDate IS NULL OR e.CertificateIssueDate >= @StartDate)
                      AND (@EndDate   IS NULL OR e.CertificateIssueDate <= @EndDate)
                    ORDER BY e.CertificateIssueDate DESC;
                END
            ");

            // ── 6. Data Completeness Report ──
            Sql(@"
                CREATE PROCEDURE [dbo].[rpt_DataCompleteness]
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT
                        c.CustomerCode  AS [Customer Code],
                        c.FirstName + ' ' + c.LastName AS [Full Name],
                        c.CustomerType  AS [Type],
                        CASE WHEN c.Email       IS NOT NULL AND c.Email       != '' THEN 'Yes' ELSE 'No' END AS [Has Email],
                        CASE WHEN c.Phone       IS NOT NULL AND c.Phone       != '' THEN 'Yes' ELSE 'No' END AS [Has Phone],
                        CASE WHEN c.DateOfBirth IS NOT NULL                         THEN 'Yes' ELSE 'No' END AS [Has DOB],
                        CASE WHEN c.Address     IS NOT NULL AND c.Address     != '' THEN 'Yes' ELSE 'No' END AS [Has Address],
                        CASE WHEN c.City        IS NOT NULL AND c.City        != '' THEN 'Yes' ELSE 'No' END AS [Has City],
                        CASE WHEN c.Province    IS NOT NULL AND c.Province    != '' THEN 'Yes' ELSE 'No' END AS [Has Province],
                        CASE WHEN c.PostalCode  IS NOT NULL AND c.PostalCode  != '' THEN 'Yes' ELSE 'No' END AS [Has Postal Code],
                        CAST(c.DataQualityScore AS DECIMAL(5,1)) AS [Quality Score (%)],
                        CAST(c.RiskScore        AS DECIMAL(5,1)) AS [Risk Score]
                    FROM dbo.Customers c
                    WHERE c.IsDeleted = 0
                    ORDER BY c.DataQualityScore ASC;
                END
            ");

            // SEED REPORT DEFINITIONS
            Sql(@"
                INSERT INTO dbo.Reports
                    (ReportName, Description, StoredProcedureName, ColumnHeaders, ParameterDefinitions, ExportFormats, Category, IconClass, BadgeVariant, SortOrder, IsActive, CreatedDate)
                VALUES
                (
                    'Customer Quality Report',
                    'Full customer list with data quality scores, risk scores and account status. Filter by score range or active status.',
                    'rpt_CustomerQuality',
                    'Customer Code,Full Name,Email,Phone,Customer Type,Quality Score (%),Risk Score,Status,Enrolled Date',
                    '[{""Name"":""MinScore"",""Type"":""number"",""Label"":""Min Quality Score"",""Default"":""0""},{""Name"":""MaxScore"",""Type"":""number"",""Label"":""Max Quality Score"",""Default"":""100""},{""Name"":""Status"",""Type"":""select"",""Label"":""Status"",""Options"":""All,Active,Inactive"",""Default"":""All""}]',
                    'Excel,PDF,HTML',
                    'Customer',
                    'fa-users',
                    'primary',
                    1, 1, GETDATE()
                ),
                (
                    'Compliance Status Report',
                    'Overview of each customer''s document verification, payment method status and medical clearance across the system.',
                    'rpt_ComplianceStatus',
                    'Customer Code,Full Name,Type,Has ID Doc,Doc Status,Doc Expiry,Has Payment,Payment Expiry,Clearance Status,Quality Score (%)',
                    '[]',
                    'Excel,PDF,HTML',
                    'Compliance',
                    'fa-shield-halved',
                    'info',
                    2, 1, GETDATE()
                ),
                (
                    'Enrollment & Revenue Report',
                    'All course enrollments with fees, payments and outstanding balances. Filter by date range or enrollment status.',
                    'rpt_EnrollmentRevenue',
                    'Customer Code,Customer Name,Course,Course Code,Enrolled,Status,Course Fee (R),Amount Paid (R),Outstanding (R),Payment Status,Certificate Issued,Completion Date',
                    '[{""Name"":""StartDate"",""Type"":""date"",""Label"":""From Date"",""Default"":""""},{""Name"":""EndDate"",""Type"":""date"",""Label"":""To Date"",""Default"":""""},{""Name"":""Status"",""Type"":""select"",""Label"":""Enrollment Status"",""Options"":""All,Active,Completed,Withdrawn,Deferred"",""Default"":""All""}]',
                    'Excel,PDF,HTML',
                    'Financial',
                    'fa-hand-holding-dollar',
                    'success',
                    3, 1, GETDATE()
                ),
                (
                    'Expiry Alert Report',
                    'Documents and payment methods that have expired or will expire within a specified number of days.',
                    'rpt_ExpiryAlerts',
                    'Alert Type,Customer Code,Customer Name,Detail,Expiry Date,Status,Days Remaining',
                    '[{""Name"":""DaysAhead"",""Type"":""number"",""Label"":""Days Ahead"",""Default"":""90""}]',
                    'Excel,PDF,HTML',
                    'Compliance',
                    'fa-triangle-exclamation',
                    'warning',
                    4, 1, GETDATE()
                ),
                (
                    'Certificate Report',
                    'All issued certificates with completion dates, grades and course details. Filter by date range.',
                    'rpt_Certificates',
                    'Customer Code,Customer Name,Email,Course,Course Code,Grade,Completion Date,Certificate Date,Course Fee (R)',
                    '[{""Name"":""StartDate"",""Type"":""date"",""Label"":""From Date"",""Default"":""""},{""Name"":""EndDate"",""Type"":""date"",""Label"":""To Date"",""Default"":""""}]',
                    'Excel,PDF,HTML',
                    'Training',
                    'fa-certificate',
                    'warning',
                    5, 1, GETDATE()
                ),
                (
                    'Data Completeness Report',
                    'Field-by-field breakdown showing which profile fields are missing for each customer.',
                    'rpt_DataCompleteness',
                    'Customer Code,Full Name,Type,Has Email,Has Phone,Has DOB,Has Address,Has City,Has Province,Has Postal Code,Quality Score (%),Risk Score',
                    '[]',
                    'Excel,PDF,HTML',
                    'Quality',
                    'fa-list-check',
                    'danger',
                    6, 1, GETDATE()
                );
            ");
        }
        
        public override void Down()
        {
            Sql("DROP PROCEDURE IF EXISTS [dbo].[rpt_CustomerQuality]");
            Sql("DROP PROCEDURE IF EXISTS [dbo].[rpt_ComplianceStatus]");
            Sql("DROP PROCEDURE IF EXISTS [dbo].[rpt_EnrollmentRevenue]");
            Sql("DROP PROCEDURE IF EXISTS [dbo].[rpt_ExpiryAlerts]");
            Sql("DROP PROCEDURE IF EXISTS [dbo].[rpt_Certificates]");
            Sql("DROP PROCEDURE IF EXISTS [dbo].[rpt_DataCompleteness]");
            DropTable("dbo.Reports");
        }
    }
}
