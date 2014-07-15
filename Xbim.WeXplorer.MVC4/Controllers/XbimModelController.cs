using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Xbim.IO;
using Xbim.ModelGeometry.Converter;
using Xbim.WeXplorer.MVC4.Models;

namespace Xbim.WeXplorer.MVC4.Controllers
{
    public class XbimModelController : Controller
    {
        //
        /// <summary>
        /// Returns an XbimModel, if the xbim file has not been created it is created
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ext"></param>
        /// <returns></returns>
        private XbimDataModel GetXbimDataModel(string name, string ext)
        {
            try
            {
                string modelLocation = Path.Combine(this.HttpContext.Request.PhysicalApplicationPath, "App_Data", name);
                string fileName = Path.ChangeExtension(modelLocation, ext);
                string xbimFileName = Path.ChangeExtension(modelLocation, "xbim");

                if (System.IO.File.Exists(xbimFileName))
                {
                    return new XbimDataModel(xbimFileName);
                }
                else
                {
                    using (XbimModel tmpModel = new XbimModel())
                    {
                        tmpModel.CreateFrom(fileName); //it is created and closed
                    }
                    return new XbimDataModel(xbimFileName);
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Unable to access {0}\n{1}", name, e.Message));
            }
        }

        private XbimGeometryModel GetXbimGeometryModel(string name, string ext)
        {
            try
            {
                string modelLocation = Path.Combine(this.HttpContext.Request.PhysicalApplicationPath, "App_Data", name);
                string fileName = Path.ChangeExtension(modelLocation, ext);
                string xbimFileName = Path.ChangeExtension(modelLocation, "xbim");

                if (System.IO.File.Exists(xbimFileName))
                {
                    return new XbimGeometryModel(xbimFileName);
                }
                else
                {
                    using (XbimModel tmpModel = new XbimModel())
                    {
                        tmpModel.CreateFrom(fileName,null,null,true); //it is created and open
                        //now create the geometry
                        //Xbim3DModelContext ctxt = new Xbim3DModelContext(tmpModel);
                        //ctxt.CreateContext();
                        tmpModel.Close();
                    }
                    return new XbimGeometryModel(xbimFileName);
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Unable to access {0}\n{1}", name, e.Message));
            }
        }

        public JsonResult GetBoundsInstances(string name, string ext)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            json.MaxJsonLength = Int32.MaxValue;
            try
            {
                using (XbimGeometryModel model = GetXbimGeometryModel(name, ext))
                {
                    json.Data = model.GetShapeInstances();
                }
            }
            catch (Exception e)
            {
                json.Data = new { Model = name, Error = e.Message };
            }
            return json;
        }

        public JsonResult Summary(string name, string ext)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            try
            {
                using (XbimGeometryModel model = GetXbimGeometryModel(name, ext))
                {
                    json.Data = model.Summary();
                }
            }
            catch (Exception e)
            {
                json.Data = new { Model = name, Error = e.Message };
            }
            return json;
        }

        public JsonResult GeometryContext(string name, string ext)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            try
            {
                using (XbimGeometryModel model = GetXbimGeometryModel(name, ext))
                {
                    json.Data = model.GetGeometryContext();
                }
            }
            catch (Exception e)
            {
                json.Data = new { Model = name, Error = e.Message };
            }
            return json;
        }



        public JsonResult Styles(string name, string ext)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            try
            {
                using (XbimGeometryModel model = GetXbimGeometryModel(name, ext))
                {
                    json.Data = model.GetStyles();
                }
            }
            catch (Exception e)
            {
                json.Data = new { Model = name, Error = e.Message };
            }
            return json;
        }

        public JsonResult ShapeInstances(string name, string ext)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            json.MaxJsonLength = Int32.MaxValue;
            try
            {
                using (XbimGeometryModel model = GetXbimGeometryModel(name, ext))
                {
                    json.Data = model.GetShapeInstances();
                }
            }
            catch (Exception e)
            {
                json.Data = new { Model = name, Error = e.Message };
            }
            return json;
        }

        public JsonResult Meshes(string name, string ext, String ids)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            try
            {
                using (XbimGeometryModel model = GetXbimGeometryModel(name, ext))
                {
                    json.Data = model.GetMeshes(ids);
                }
            }
            catch (Exception e)
            {
                json.Data = new { Model = name, Error = e.Message };
            }
            return json;
        }

        public JsonResult GeometryVersion(string name, string ext, String ids)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            try
            {
                using (XbimGeometryModel model = GetXbimGeometryModel(name, ext))
                {
                    json.Data = model.GetGeometryVersion();
                }
            }
            catch (Exception e)
            {
                json.Data = new { Model = name, Error = e.Message };
            }
            return json;
        }
        public ActionResult View3D(string name, string ext)
        {
            try
            {
                using (XbimGeometryModel model = GetXbimGeometryModel(name, ext)) //make sure the model is compiled etc
                {
                    return View("Model3DView",model.Name as object);
                }
            }
            catch (Exception e)
            {
                return View("ModelError", e.Message);
            }
        }

        public JsonResult GetSceneOutline(string name, string ext)
        {
            JsonResult json = new JsonResult();
            json.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            try
            {
                using (XbimGeometryModel model = GetXbimGeometryModel(name, ext))
                {
                    json.Data = model.GetSceneOutline(); 
                }
            }
            catch (Exception e)
            {
                json.Data = new { Model = name, Error = e.Message };
            }
            return json;
        }
        
    }
}
