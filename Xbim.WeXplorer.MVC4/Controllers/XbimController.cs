using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Xbim.IO;
using Xbim.WeXplorer.MVC4.Models;

namespace Xbim.WeXplorer.MVC4.Controllers
{
    public class XbimController : Controller
    {
        protected XbimModel GetModel(string name)
        {
            string modelLocation = Path.Combine(this.HttpContext.Request.PhysicalApplicationPath, "App_Data", name);
            string xbimFileName = Path.ChangeExtension(modelLocation, "xbim");

            if (System.IO.File.Exists(xbimFileName))
            {
                var result = new XbimModel();
                result.Open(xbimFileName, XbimExtensions.XbimDBAccess.ReadWrite);
                return result;
            }
            else
            {
                throw new Exception("Model was not created yet.");
            }
        }

        protected JsonResult CreateJsonResult(object data, JsonRequestBehavior behavior)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);

            var result = new JsonResult();
            result.Data = json;
            result.JsonRequestBehavior = behavior;
            result.ContentType = "application/json";

            return result;
        }

        protected JsonResult CreateJsonError(string message, JsonRequestBehavior behavior)
        {
            var obj = new XbimError(message);
            return CreateJsonResult(obj, behavior);
        }

    }
}
