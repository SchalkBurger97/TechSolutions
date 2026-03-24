using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace TechSolutions.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string FullName
        {
            get { return FirstName + " " + LastName; }
        }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            userIdentity.AddClaim(new Claim("FullName", FirstName + " " + LastName));
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
            Database.SetInitializer<ApplicationDbContext>(null);
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<IdentificationDocument> IdentificationDocuments { get; set; }
        public DbSet<PaymentInformation> PaymentInformation { get; set; }
        public DbSet<MedicalClearance> MedicalClearances { get; set; }
        public DbSet<EnrollmentHistory> EnrollmentHistories { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>().Property(c => c.DataQualityScore).HasPrecision(5, 2);
            modelBuilder.Entity<Customer>().Property(c => c.RiskScore).HasPrecision(5, 2);
            modelBuilder.Entity<EnrollmentHistory>().Property(e => e.CourseFee).HasPrecision(10, 2);
            modelBuilder.Entity<EnrollmentHistory>().Property(e => e.AmountPaid).HasPrecision(10, 2);
        }
    }
}
