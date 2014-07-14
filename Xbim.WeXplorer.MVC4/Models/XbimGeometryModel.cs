using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.ModelGeometry.Converter;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;
using Xbim.WeXplorer.MVC4.Helpers;
using Xbim.IO;
using System.IO;
using Xbim.Common.Geometry;

namespace Xbim.WeXplorer.MVC4.Models
{
    public enum Commands : byte
    {
        Error = 0x1,
        GetInstances = 0x2
    }

    /// <summary>
    /// A specialised Data Model that contains pre-processed geometry
    /// </summary>
    public class XbimGeometryModel : XbimDataModel
    {

        bool _hasGeometry;

        public bool HasGeometry
        {
            get { return _hasGeometry; }
        }

        public XbimGeometryModel(string xbimFileName)
            : base(xbimFileName)
        {
            //if (EnsureGeometryExists())
            //    _hasGeometry = true;
        }

        private bool EnsureGeometryExists()
        {
            //if (model.GeometrySupportLevel > 0) return true;
            //Xbim3DModelContext context = new Xbim3DModelContext(model);
            //try
            //{
            //    context.CreateContext();
                return true;
            //}
            //catch (Exception)
            //{

            //    return false;
            //}

        }

        public String GetGeometryContext()
        {
            dynamic returndata;
            //if (!EnsureGeometryExists())
            //{
            //    returndata = new
            //    {
            //        Model = Name,
            //        Error = "Failed to create the geometry"
            //    };
            //}
            //else
            //{
            //    switch (model.GeometrySupportLevel)
            //    {
            //        case 1:
            //            IfcProject project = model.IfcProject;
            //            int projectId = 0;
            //            if (project != null) projectId = Math.Abs(project.EntityLabel);
            //            XbimGeometryData regionData = model.GetGeometryData(projectId, XbimGeometryType.Region).FirstOrDefault(); //get the region data should only be one
            //            if (regionData != null)
            //            {
            //                returndata = new
            //                {
            //                    MetreFactor = 1.0 / model.ModelFactors.OneMetre,
            //                    Regions = XbimRegionCollection.FromArray(regionData.ShapeData)
            //                };
            //            }
            //            else
            //                returndata = Enumerable.Empty<XbimRegion>();
            //            break;
            //        case 2:
                        Xbim3DModelContext context = new Xbim3DModelContext(model);
                        returndata = new
                        {
                            MetreFactor = 1.0 / model.ModelFactors.OneMetre,
                            Regions = context.GetRegions()
                        };
            //            break;
            //        default:
            //            returndata = new { Error = String.Format("Unexpected Geometry Support Level {0}", model.GeometrySupportLevel) };
            //            break;
            //    }
            //}
            return JsonConvert.SerializeObject(returndata);
        }

        public string GetShapeInstances()
        {

            Xbim3DModelContext m3d = new Xbim3DModelContext(model);
            var sbs = m3d.ShapeInstances().Where(si=>si.RepresentationType==XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded);
            var message = new
            {
                command = Commands.GetInstances,
                instances = sbs
            };
            return JsonConvert.SerializeObject(message);

        }

        /// <summary>
        /// Returns meta data about shapes that are multiply used
        /// </summary>
        /// <returns></returns>
        public String GetShapeGeometry()
        {
            object returndata;

            Xbim3DModelContext context = new Xbim3DModelContext(model);

            if (context.IsGenerated)
            {
                returndata = context.MappedShapes().ToList();
            }
            else
            {
                returndata = Enumerable.Empty<Int32>();
            }

            return JsonConvert.SerializeObject(returndata);
        }


        public String GetStyles()
        {
            Xbim3DModelContext context = new Xbim3DModelContext(model);
            return JsonConvert.SerializeObject(context.SurfaceStyles());
        }

       

        public object GetMeshes(String ids)
        {
            
            object returndata;

            String[] stringids = ids.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            IEnumerable<UInt32> intIds = stringids.Select(s => UInt32.Parse(s));

            Xbim3DModelContext context = new Xbim3DModelContext(model);

            //if (context.IsGenerated)
            //{
            List<object> shapes = new List<object>(intIds.Count());
            foreach (var id in intIds)
            {
                XbimShapeGeometry g = context.ShapeGeometry(id);
                dynamic d = new { id = g.ShapeLabel, data = g.ShapeData };
                shapes.Add(d);
            }
            returndata = shapes.ToList();
            //}
            //else
            //{
            //    returndata = Enumerable.Empty<XbimShape>();
            //}

            return JsonConvert.SerializeObject(returndata);
        }

        public object GetGeometryVersion()
        {
            object data = new
            {
                GeometryVersion = model.GeometrySupportLevel
            };
            return JsonConvert.SerializeObject(data);
        }

        public String GetSceneOutline()
        {
            Xbim3DModelContext context = new Xbim3DModelContext(model);
            return context.CreateSceneJS(); ;
        }
    }
}