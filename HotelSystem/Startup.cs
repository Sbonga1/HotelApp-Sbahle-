using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(HotelSystem.Startup))]
namespace HotelSystem
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
