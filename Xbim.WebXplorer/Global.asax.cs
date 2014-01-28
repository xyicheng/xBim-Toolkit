using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
//using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Xbim.WebXplorer.xbim;

namespace Xbim.WebXplorer
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Model", // Route name
                "XbimModel/{action}/{Model}/{Ids}", // URL with parameters
                new { controller = "XbimModel", Ids = UrlParameter.Optional }
            );
            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected void Application_Start()
        {
            String _path = String.Concat(System.Environment.GetEnvironmentVariable("PATH"), ";", ConfigurationManager.AppSettings["NativePath"]);
            System.Environment.SetEnvironmentVariable("PATH", _path, EnvironmentVariableTarget.Process);

            AreaRegistration.RegisterAllAreas();

            // Use LocalDB for Entity Framework by default
            //Database.DefaultConnectionFactory = new SqlConnectionFactory(@"Data Source=(localdb)\v11.0; Integrated Security=True; MultipleActiveResultSets=True");

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }
    }
}