using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(TechSolutions.Startup))]
namespace TechSolutions
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
