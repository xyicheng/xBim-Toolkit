using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using Xbim.WebXplorer.Models;
using Xbim.WebXplorer.xbim;

namespace Xbim.WebXplorer.Controllers
{
    public class XbimModelController : Controller
    {
        private static String ModelPath = WebConfigurationManager.AppSettings["XbimModelLocation"].ToString();
        private static String ModelExt = WebConfigurationManager.AppSettings["XbimfileExtension"].ToString();

        public JsonResult ModelContext(String Model)
        {
            XbimSceneModel m = new XbimSceneModel(Model);
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            json.Data = m.GetModelContext();
            return json;
        }
        public JsonResult GeometrySupportLevel(String Model)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            XbimSceneModel m = new XbimSceneModel(Model);
            json.Data = m.GetGeometrySupportLevel();
            return json;
        }
        //public JsonResult Manifest(String Model)
        //{
        //    JsonResult json = new JsonResult();
        //    json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

        //    XbimSceneModel m = new XbimSceneModel(Model);
        //    json.Data = m.GetManifest();
        //    return json;
        //}
        public JsonResult LibraryShapes(String Model)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            XbimSceneModel m = new XbimSceneModel(Model);
            json.Data = m.GetLibraryShapes();
            return json;
        }
        public JsonResult ProductShapes(String Model)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            XbimSceneModel m = new XbimSceneModel(Model);
            json.Data = m.GetProductShapes();
            return json;
        }




        public JsonResult LibraryStyles(String Model)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            XbimSceneModel m = new XbimSceneModel(Model);
            json.Data = m.GetLibraryStyles();
            return json;
        }
        public JsonResult Geometry(String Model, String ids)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            XbimSceneModel m = new XbimSceneModel(Model);
            json.Data = m.GetMeshes(ids);
            return json;
        }
    }
}
