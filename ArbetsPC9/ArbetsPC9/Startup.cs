using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ArbetsPC9.Startup))]
namespace ArbetsPC9
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
