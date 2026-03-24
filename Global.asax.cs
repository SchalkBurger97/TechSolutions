using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using TechSolutions.Models;

namespace TechSolutions
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<ApplicationDbContext, TechSolutions.Migrations.Configuration>());
            try
            {
                var migrator = new System.Data.Entity.Migrations.DbMigrator(new TechSolutions.Migrations.Configuration());
                migrator.Update();
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText(@"C:\Temp\startup_error.txt", ex.ToString());
            }
        }
    }
}
