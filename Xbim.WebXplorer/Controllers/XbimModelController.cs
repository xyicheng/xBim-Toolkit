using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Xbim.WebXplorer.Models;
using Xbim.WebXplorer.xbim;

namespace Xbim.WebXplorer.Controllers
{
    public class XbimModelController : Controller
    {
        private const String ModelPath = "c:\\";
        private const String ModelExt = ".xbim";
        private XbimSceneModel GetModel(String Model)
        {
            XbimSceneModel model = null;
            try
            {
                model = Session["XbimModel"] as XbimSceneModel;
            } catch (Exception){}

            if (model == null)
            {
                var xbim = new XbimModelHandler(ModelPath + Model + ModelExt);
                xbim.Init();
                model = new XbimSceneModel(xbim);

                Session["XbimModel"] = model;
            }
            return model;
        }

        public JsonResult ModelBounds(String Model)
        {
            XbimSceneModel m = GetModel(Model);
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            json.Data = m.GetModelBounds();
            return json;
        }
        public JsonResult Manifest(String Model)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            XbimSceneModel m = GetModel(Model);
            json.Data = m.GetManifest();
            return json;
        }
        public JsonResult Materials(String Model)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            XbimSceneModel m = GetModel(Model);
            json.Data = m.GetMaterials();
            return json;
        }
        public JsonResult Geometry(String Model, String ids)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            XbimSceneModel m = GetModel(Model);
            json.Data = m.GetGeometry(ids);
            return json;
        }
    }
}
