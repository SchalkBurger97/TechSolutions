# TechSolutions
### Secure Data Management System - Technical Assessment

This project is a submission for the following assignment:

> "You have been assigned to develop a data management system for a fictitious company
> called TechSolutions. TechSolutions handles sensitive customer information, and it is
> crucial to ensure the security and integrity of the data. Your task is to design and
> implement a secure data management system."

---

## Overview

A full-stack web application for managing the enrollment, compliance, and training
history of healthcare professionals. The system goes beyond the core CRUD requirements
to include encrypted identity storage, document management, audit logging, and
role-based access control - demonstrating a solid approach to handling
sensitive customer data securely.

---

## Design Decisions

**Database Schema**
The schema is built around a central Customers table with related tables for
IdentificationDocuments, PaymentInformation, MedicalClearances, and
EnrollmentHistories. Entity Framework Code First was used so the schema is
version-controlled via migrations and auto-deployed on first run.

**Security**
Sensitive identity fields (SA ID numbers and Passport numbers) are encrypted at the
application layer using AES encryption before being persisted to the database. Uploaded
document files are also encrypted at rest on the server. This means a compromised
database alone is not sufficient to expose sensitive customer data.

**Audit Logging**
Every create, update, delete, and sensitive data access operation is written to an
AuditLogs table capturing the user, timestamp, IP address, action type, and
field-level changes. This provides a complete tamper-evident trail of all data
interactions.

**Role-Based Access**
Two roles are implemented - Admin and Employee. Employees can perform day-to-day
data entry and updates but cannot access encrypted identity numbers.
This follows the principle of least privilege.

**Multi-Step Enrollment Wizard**
Rather than a single flat form, customer enrollment is handled via a 6-step wizard.
This improves data quality by guiding users through each section and validating
each step before proceeding.

**Data Quality Scoring**
Each customer record has an automatically calculated data quality score based on
field completeness. This surfaces incomplete records and encourages staff to
maintain high-quality data.

---

## Features

- **Customer Management**
  Full enrollment wizard with multi-step onboarding, profile editing, and automatic
  data quality scoring

- **Identity Verification**
  SA ID and Passport numbers captured during enrollment and stored with AES encryption

- **Document Management**
  Upload and manage identification documents with auto-generated reference numbers,
  expiry tracking, and encrypted file storage

- **Payment Management**
  Track credit/debit cards and EFT banking details per customer

- **Medical Clearance**
  TB test, COVID vaccination, and background check tracking for on-site training
  eligibility

- **Enrollment and Training History**
  Track course enrollments, completions, and certificate issuance per customer

- **Audit Logging**
  Every create, update, delete, and sensitive data access is logged with the
  requesting user, timestamp, IP address, and field-level change tracking

- **Role-Based Access Control**
  Admin and Employee roles with permission-based access throughout the application

---

## Tech Stack

| Layer      | Technology                              |
|------------|-----------------------------------------|
| Framework  | ASP.NET MVC 5 (.NET Framework)          |
| Language   | C#                                      |
| ORM        | Entity Framework 6 (Code First)         |
| Database   | SQL Server                              |
| Auth       | ASP.NET Identity                        |
| Frontend   | Bootstrap, jQuery, Font Awesome         |
| Security   | AES Encryption (fields + file storage)  |

---

## Getting Started

### Requirements

- Visual Studio 2019 or later
- SQL Server (any edition)
- .NET Framework 4.x

### Setup

**1. Clone the repository**

```
git clone https://github.com/SchalkBurger97/TechSolutions.git
```

**2. Open the solution**

Open TechSolutions.sln in Visual Studio

**3. Configure your database connection**

Open Web.config and replace YOUR_SERVER_NAME with your SQL Server instance name:

```xml
<add name="DefaultConnection"
     connectionString="Server=YOUR_SERVER_NAME;Database=TechSolutionsDB;Trusted_Connection=True;MultipleActiveResultSets=true"
     providerName="System.Data.SqlClient" />
```

Common instance names:

| Setup                          | Server Name          |
|--------------------------------|----------------------|
| SQL Server Express (default)   | .\SQLEXPRESS         |
| Named instance                 | .\YOURINSTANCENAME   |
| Default local instance         | .                    |

Tip: Open SQL Server Management Studio - the server name shown in the
login dialog is exactly what goes here.

**4. Build the solution**

Visual Studio will restore NuGet packages automatically on build. 
If you encounter a missing roslyn/csc.exe error, run the following in the Package Manager Console:
```
Update-Package Microsoft.CodeDom.Providers.DotNetCompilerPlatform -r
```
You can click cancel on the dialog window that pops up.

Then clean and rebuild the solution via Build -> Clean Solution, then Build -> Rebuild Solution.

**5. Run the application**

Press F5 - Entity Framework will automatically create the database, apply
all migrations, and seed the default admin account on first launch.

---

## Default Login

| Role     | Email                        | Password   |
|----------|------------------------------|------------|
| Admin    | admin@techsolutions.com      | Admin@123  |

Admin has full access to all features.
Employee role has restricted access and cannot delete records.

---

## Project Structure

```
TechSolutions/
|-- Controllers/          MVC Controllers
|-- Models/               Entity Framework models and Identity
|-- Views/                Razor views organised by controller
|-- Services/             Business logic layer
|-- Helpers/              Encryption and audit utilities
|-- Migrations/           EF Code First migrations and seed data
|-- App_Data/
|   |-- DocumentUploads/  Encrypted document file storage
```

---

## Security Notes

- SA ID numbers and Passport numbers are encrypted with AES before being stored
- Uploaded document files are encrypted at rest on the server
- All sensitive data access is audit logged with user, timestamp, and IP address
- Employee accounts cannot delete records or access encrypted identity numbers

---

## Notes for Reviewer

- The database and schema are created automatically on first run via EF migrations
- All migrations are included in the repository under the Migrations folder
- The only manual step required is updating the server name in Web.config
- A demo data seeder is included to populate the system with realistic data. To use it:
  1. Log in with the admin account
  2. Click your email address in the top right corner of the navigation bar
  3. Click "Seed Data" under the Demo Data Seeder section
  4. This will generate 50 customers with related payment, clearance, and enrollment
     records so the full functionality of the system can be explored immediately
  - Note: Documents are excluded from the seed as these require real file uploads

