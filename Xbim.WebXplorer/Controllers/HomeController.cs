using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using Xbim.WebXplorer.Models;

namespace Xbim.WebXplorer.Controllers
{
    public class HomeController : Controller
    {
        private static String ModelLocation  = WebConfigurationManager.AppSettings["XbimModelLocation"].ToString();
        private static String ModelExtension = WebConfigurationManager.AppSettings["XbimfileExtension"].ToString();

        private static IEnumerable<String> GetModelList()
        {
            DirectoryInfo d = new DirectoryInfo(ModelLocation);
            var files = d.EnumerateFiles("*" + ModelExtension);
            List<String> retvals = new List<string>();
            foreach (var f in files)
            {
                retvals.Add(f.Name.ToLower().Replace(ModelExtension.ToLower(), ""));
            }
            return retvals;
        }

        public ActionResult Index()
        {
            var models = GetModelList();
            return View(models);
        }
        public ActionResult ViewModel(String id)
        {
            return View("ViewModel", id as Object);
        }
    }
}
