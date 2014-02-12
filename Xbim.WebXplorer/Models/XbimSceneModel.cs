using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using Xbim.Common.Geometry;
using Xbim.WebXplorer.xbim;
using Xbim.IO;
using System.IO;
using System.Web.Configuration;
using Xbim.ModelGeometry.Converter;
using Xbim.ModelGeometry.Scene;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.PresentationAppearanceResource;

namespace Xbim.WebXplorer.Models
{
    public class XbimSceneModel: IDisposable
    {
        private static String ModelPath = WebConfigurationManager.AppSettings["XbimModelLocation"].ToString();
        private static String ModelExt = WebConfigurationManager.AppSettings["XbimfileExtension"].ToString();
        private XbimModel _model = null;
        public XbimSceneModel(String ModelName)
        {
            var fullpath = Path.ChangeExtension(Path.Combine(ModelPath,ModelName), ModelExt);
            _model = new XbimModel();
            _model.Open(fullpath, XbimExtensions.XbimDBAccess.Read);
        }

        public String GetGeometrySupportLevel()
        {
            object data = new { 
                GeometrySupportLevel = _model.GeometrySupportLevel
            };
            return JsonConvert.SerializeObject(data);
        }

        public String GetModelContext()
        {
            object returndata;
            switch(_model.GeometrySupportLevel)
            {
                case 1:
                     IfcProject project = _model.IfcProject;
                    int projectId = 0;
                    if (project != null) projectId = Math.Abs(project.EntityLabel);
                    XbimGeometryData regionData = _model.GetGeometryData(projectId, XbimGeometryType.Region).FirstOrDefault(); //get the region data should only be one
                    if (regionData != null)
                    {
                        returndata = new {
                            MetreFactor = 1.0 / _model.ModelFactors.OneMetre,
                            Regions = XbimRegionCollection.FromArray(regionData.ShapeData)
                        };
                    }
                    else
                        returndata = Enumerable.Empty<XbimRegion>();
                    break;
                case 2:
                    Xbim3DModelContext context = new Xbim3DModelContext(_model);
                    returndata = new 
                    {
                        MetreFactor = 1.0 / _model.ModelFactors.OneMetre,
                        Regions = context.GetRegions()
                    };
                    break;
                default:
                    returndata = new { Error = String.Format("Unexpected Geometry Support Level {0}", _model.GeometrySupportLevel) };
                    break;
            }

            return JsonConvert.SerializeObject(returndata);
        }

        public String GetLibraryShapes()
        {
            object returndata;

            Xbim3DModelContext context = new Xbim3DModelContext(_model);

            if (context.IsGenerated)
            {
                var shapes = context.MappedShapes();
                returndata = new {
                    IDs = shapes.Select(d=>d.Item1),
                    Counts = shapes.Select(d=>d.Item2)
                };
            }
            else {
                returndata = Enumerable.Empty<Int32>();
            }

            return JsonConvert.SerializeObject(returndata);
        }
        public String GetProductShapes()
        {
            object returndata;

            Xbim3DModelContext context = new Xbim3DModelContext(_model);

            if (context.IsGenerated)
            {
                var shapes = context.ProductShapes;
                returndata = shapes.Select(s => s.GetSimpleMetadataObject());
            }
            else
            {
                returndata = Enumerable.Empty<Int32>();
            }

            return JsonConvert.SerializeObject(returndata);
        }
        public String GetLibraryStyles()
        {
            object returndata;

            Xbim3DModelContext context = new Xbim3DModelContext(_model);

            Xbim.WebXplorer.xbim.ColourMap ColourMap = new Xbim.WebXplorer.xbim.ColourMap();

            List<XBimMaterial> Materials = new List<XBimMaterial>();

            foreach (var cm in ColourMap)
            {
                XbimTexture t = new XbimTexture().CreateTexture(cm);
                XBimMaterial m = new XBimMaterial();
                m.CreateMaterial(t);
                m.Material.MaterialID = cm.Name;
                Materials.Add(m);
            }

            if (context.IsGenerated)
            {
                IEnumerable<Int32> labels = context.GetShapeStyleLabels();

                foreach(var label in labels)
                {
                    XbimTexture t = new XbimTexture().CreateTexture(_model.Instances[label] as IfcSurfaceStyle);
                    XBimMaterial m = new XBimMaterial();
                    m.CreateMaterial(t);
                    m.Material.MaterialID = label.ToString();
                    Materials.Add(m);
                }
            }
            returndata = Materials;
            return JsonConvert.SerializeObject(returndata);
        }
        #region older functions
        
        public String GetMeshes(String Ids)
        {
            object returndata;
            String[] stringids = Ids.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);

            IEnumerable<Int32> ids = stringids.Select(s => Int32.Parse(s));

            Xbim3DModelContext context = new Xbim3DModelContext(_model);

            if (context.IsGenerated)
            {
                var shapes = context.Shapes(ids, cache:false);
                returndata = shapes;
            }
            else
            {
                returndata = Enumerable.Empty<XbimShape>();
            }

            //var data = _model.GetGeometry(Ids);
            
            //foreach (var i in data)
            //{
            //    retval.Add(new { 
            //        //id = i.GeometryLabel,
            //        //prod = i.IfcProductLabel,
            //        //geo = i.ShapeData,
            //        //matrix = XbimMatrix3D.FromArray(i.DataArray2)
            //    });
            //}

            return JsonConvert.SerializeObject(returndata);
        }
        #endregion
        public void Dispose()
        {
            if (_model != null) _model.Dispose();
        }
    }
}