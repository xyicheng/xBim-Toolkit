using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Xbim.WeXplorer.MVC4.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {

            string modelLocation = Path.Combine(this.HttpContext.Request.PhysicalApplicationPath, "App_Data");
            var models = GetModelList(modelLocation);
            return View(models);
        }

        private static IEnumerable<String> GetModelList(string modelLocation)
        {
            DirectoryInfo d = new DirectoryInfo(modelLocation);
            var files = d.EnumerateFiles("*" + ".ifc");
            List<String> retvals = new List<string>();
            foreach (var f in files)
            {
                retvals.Add(f.Name);
            }
            return retvals;
        }

    }
}
