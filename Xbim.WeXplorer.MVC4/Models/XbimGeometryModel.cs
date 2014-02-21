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

namespace Xbim.WeXplorer.MVC4.Models
{
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
            if (EnsureGeometryExists())
                _hasGeometry = true;
        }

        private bool EnsureGeometryExists()
        {
            if (model.GeometrySupportLevel > 0) return true;
            Xbim3DModelContext context = new Xbim3DModelContext(model);
            try
            {
                context.CreateContext();
                return true;
            }
            catch (Exception )
            {
                
                return false;
            }
           
        }

        public String GetGeometryContext()
        {
            dynamic returndata;
            if (!EnsureGeometryExists())
            {
                returndata = new
                {
                    Model = Name,
                    Error = "Failed to create the geometry"
                };
            }
            else
            {
                switch (model.GeometrySupportLevel)
                {
                    case 1:
                        IfcProject project = model.IfcProject;
                        int projectId = 0;
                        if (project != null) projectId = Math.Abs(project.EntityLabel);
                        XbimGeometryData regionData = model.GetGeometryData(projectId, XbimGeometryType.Region).FirstOrDefault(); //get the region data should only be one
                        if (regionData != null)
                        {
                            returndata = new
                            {
                                MetreFactor = 1.0 / model.ModelFactors.OneMetre,
                                Regions = XbimRegionCollection.FromArray(regionData.ShapeData)
                            };
                        }
                        else
                            returndata = Enumerable.Empty<XbimRegion>();
                        break;
                    case 2:
                        Xbim3DModelContext context = new Xbim3DModelContext(model);
                        returndata = new
                        {
                            MetreFactor = 1.0 / model.ModelFactors.OneMetre,
                            Regions = context.GetRegions()
                        };
                        break;
                    default:
                        returndata = new { Error = String.Format("Unexpected Geometry Support Level {0}", model.GeometrySupportLevel) };
                        break;
                }
            }
            return JsonConvert.SerializeObject(returndata);
        }

        /// <summary>
        /// Returns meta data about shapes that are multiply used
        /// </summary>
        /// <returns></returns>
        public String GetShapeLibrary()
        {
            object returndata;

            Xbim3DModelContext context = new Xbim3DModelContext(model);

            if (context.IsGenerated)
            {
                var shapes = context.MappedShapes();
                returndata = new
                {
                    IDs = shapes.Select(d => d.Item1),
                    Counts = shapes.Select(d => d.Item2)
                };
            }
            else
            {
                returndata = Enumerable.Empty<Int32>();
            }

            return JsonConvert.SerializeObject(returndata);
        }


        public String GetStyleLibrary()
        {
            object returndata;

            Xbim3DModelContext context = new Xbim3DModelContext(model);

            XbimColourMap ColourMap = new XbimColourMap();

            List<XbimColour> Materials = new List<XbimColour>();

            foreach (var cm in ColourMap)
            {      
                Materials.Add(cm);
            }

            if (context.IsGenerated)
            {
                IEnumerable<Int32> labels = context.GetShapeStyleLabels();

                foreach (var label in labels)
                {
                    XbimColour m = new XbimColour(model.Instances[label] as IfcSurfaceStyle);
                    m.Name = label.ToString();
                    Materials.Add(m);
                }
            }
            returndata = Materials;
            return JsonConvert.SerializeObject(returndata);
        }

        public String GetProductShapes()
        {
            object returndata;

            Xbim3DModelContext context = new Xbim3DModelContext(model);

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

        public object GetMeshes(String ids)
        {
            object returndata;
            String[] stringids = ids.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            IEnumerable<Int32> intIds = stringids.Select(s => Int32.Parse(s));

            Xbim3DModelContext context = new Xbim3DModelContext(model);

            if (context.IsGenerated)
            {
                var shapes = context.Shapes(intIds, cache: false);
                returndata = shapes;
            }
            else
            {
                returndata = Enumerable.Empty<XbimShape>();
            }

            return JsonConvert.SerializeObject(returndata);
        }
    }
}