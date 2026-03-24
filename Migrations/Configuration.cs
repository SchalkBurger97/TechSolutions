namespace TechSolutions.Migrations
{
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using TechSolutions.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<TechSolutions.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(TechSolutions.Models.ApplicationDbContext context)
        {
            // ── Roles ────────────────────────────────────────────────────────────
            var roleManager = new RoleManager<IdentityRole>(
                new RoleStore<IdentityRole>(context));

            foreach (var roleName in new[] { "Admin", "Employee" })
            {
                if (!roleManager.RoleExists(roleName))
                    roleManager.Create(new IdentityRole(roleName));
            }

            // ── Default Admin User ───────────────────────────────────────────────
            var userManager = new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(context));

            string adminEmail = "admin@techsolutions.com";
            string adminPassword = "Admin@123";

            if (userManager.FindByEmail(adminEmail) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = userManager.Create(user, adminPassword);

                if (result.Succeeded)
                    userManager.AddToRole(user.Id, "Admin");
            }
        }
    }
}
