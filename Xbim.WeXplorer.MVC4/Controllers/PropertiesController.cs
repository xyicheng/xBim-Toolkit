using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Extensions;
using Xbim.IO;
using System.IO;
using Xbim.WeXplorer.MVC4.Models;
using Xbim.Ifc2x3.PropertyResource;

namespace Xbim.WeXplorer.MVC4.Controllers
{
    public class PropertiesController : XbimController
    {
        public JsonResult GetProperties(string model, int entityLabel)
        {
            var m = GetModel(model);
            var label = entityLabel;
            var obj = m.Instances.Where<IfcObject>(o =>  o.EntityLabel == label).FirstOrDefault();
            if (obj != null)
            {
                var pSets = obj.GetAllPropertySets();
                var result = new List<XbimPropertyModel>();
                foreach (var pSet in pSets)
                    foreach (var prop in pSet.HasProperties.OfType<IfcPropertySingleValue>())
                        result.Add(new XbimPropertyModel(prop));
                return CreateJsonResult(result, JsonRequestBehavior.AllowGet);
            }
            else
                return CreateJsonResult(new { }, JsonRequestBehavior.AllowGet);
        }

        
    }
}
