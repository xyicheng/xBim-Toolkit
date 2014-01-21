using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Xbim.WebXplorer.Models;

namespace Xbim.WebXplorer.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var models = ModelManager.GetModelList();
            return View(models);
        }
        public ActionResult ViewModel(String id)
        {
            return View("ViewModel",id as Object);
        }
    }
}
