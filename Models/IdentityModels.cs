using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace TechSolutions.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
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