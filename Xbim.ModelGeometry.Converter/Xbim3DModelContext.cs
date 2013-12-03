using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.Ifc2x3.Extensions;
namespace Xbim.ModelGeometry.Converter
{
    /// <summary>
    /// Represents a gemetric representation context, i.e. a 'Body' Representation
    /// </summary>
    public class Xbim3DModelContext
    {
        private Dictionary<int,XbimShape> _shapes;
        private HashSet<XbimRepresentationItem> _representationItems;
        private HashSet<XbimShapeMap> _shapeMaps;
        private List<XbimProductShape> _productDefinitions;
        private List<IfcBooleanResult> _booleanResults;

        public IEnumerable<IfcBooleanResult> BooleanResults
        {
            get { return _booleanResults; }
        }

        public IEnumerable<XbimShape> Shapes
        {
            get
            {
                return _shapes.Values;
            }
        }

        public IEnumerable<XbimShapeMap> ShapeMaps
        {
            get
            {
                return _shapeMaps;
            }
        }

        public IEnumerable<IXbimGeometryModel> RepresentationItems
        {
            get
            {
                return _representationItems;
            }
        }

        public IEnumerable<XbimProductShape> ProductDefinitions
        {
            get
            {
                return _productDefinitions;
            }
        }
        /// <summary>
        /// Initialises a model context from the model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="contextType"></param>
        /// <param name="contextIdentifier"></param>
        public Xbim3DModelContext(XbimModel model, string contextIdentifier = "Body", string contextType = "Model")
        {
            
            IfcGeometricRepresentationContext context = model.Instances.OfType<IfcGeometricRepresentationContext>()
                                                        .Where(c => String.Compare(c.ContextType, contextType, true) == 0 &&
                                                                    String.Compare(c.ContextIdentifier, contextIdentifier, true) == 0)
                                                                    .FirstOrDefault();
            if (context != null)
            {
                List<IfcShapeRepresentation> shapes = context.RepresentationsInContext.OfType<IfcShapeRepresentation>()
                    .ToList();
                _productDefinitions = new List<XbimProductShape>(shapes.Count); //should always be a bit smaller
                _shapes = new Dictionary<int, XbimShape>();
                _representationItems = new HashSet<XbimRepresentationItem>();
                _shapeMaps = new HashSet<XbimShapeMap>();
                _booleanResults = new List<IfcBooleanResult>();
                double tolerance = model.ModelFactors.DeflectionTolerance;
                
                foreach (var shape in shapes)
                {
                    XbimMatrix3D m3D = XbimMatrix3D.Identity;
                    XbimShape xbimShape = new XbimShape(Math.Abs(shape.EntityLabel));
                    _shapes.Add(xbimShape.ShapeLabel, xbimShape);
                    var prodDef = shape.OfProductRepresentation.OfType<IfcProductDefinitionShape>().FirstOrDefault();
                    if (prodDef != null)
                    {
                        IfcProduct prod = prodDef.ShapeOfProduct.FirstOrDefault();
                        XbimProductShape prodShape = new XbimProductShape(Math.Abs(prod.EntityLabel), xbimShape.ShapeLabel);
                        m3D = prod.ObjectPlacement.ToMatrix3D();
                        _productDefinitions.Add(prodShape);
                    }
                    foreach (var item in shape.Items)
                    {
                        if (item is IfcMappedItem) //if it is a map then resolve to shape
                        {
                           
                            IfcMappedItem map = (IfcMappedItem)item;
                            int shapeLabel = Math.Abs(map.MappingSource.MappedRepresentation.EntityLabel);
                            XbimShapeMap mapItem = new XbimShapeMap(Math.Abs(item.EntityLabel), shapeLabel);
                            _shapeMaps.Add(mapItem);
                            xbimShape.Add(mapItem.ShapeMapLabel);

                        }
                        else //otherwise treat as standard
                        {
                            IXbimGeometryModelGroup geomGrp = item.Geometry3D();
                            
                            foreach (var geom in geomGrp)
                            {
                               // double vol = geom.Volume;
                               // XbimPoint3D com = geom.CentreOfMass;
                               // XbimPoint3D wcscom = m3D.Transform(com);
                               //// XbimRect3D r3D = geom.Get
                                String s = geom.WriteAsString();
                                //XbimTriangulatedModelCollection tm = geomGrp.Mesh(10);
                                //Console.WriteLine("String " + s.Length);
                                //MemoryStream ms = new MemoryStream();
                                //DeflateStream gz = new DeflateStream(ms,CompressionMode.Compress, false);
                                //byte[] buffer = Encoding.UTF8.GetBytes(s);

                                //gz.Write(buffer,0,buffer.Length);
                                //gz.Flush();
                                //gz.Close();
                                //Console.WriteLine("Zip " + ms.ToArray().Length);
                               
                                //foreach (XbimTriangulatedModel b in tm)
                                //{
                                //    Console.WriteLine("Triangle " + b.Triangles.Length);
                                //    ms = new MemoryStream();
                                //    GZipStream gz2 = new GZipStream(ms,CompressionMode.Compress, false);


                                //    gz2.Write(b.Triangles, 0, b.Triangles.Length);
                                //gz2.Flush();
                                //gz2.Close();
                                //Console.WriteLine("Zip " + ms.ToArray().Length);
                               // }
                                
                                Console.WriteLine(s);
                                XbimMeshGeometry3D mesh = new XbimMeshGeometry3D();
                                mesh.Read(s);
                                Console.WriteLine(mesh.PositionCount);

                            }
                            XbimRepresentationItem repItem = new XbimRepresentationItem(Math.Abs(item.EntityLabel));
                            _representationItems.Add(repItem);
                            xbimShape.Add(repItem.RepresentationItemLabel);
                            if (item is IfcBooleanResult)
                            {
                                _booleanResults.Add((IfcBooleanResult)item);
                            }
                        }
                    }
                    
                }

               
            }
            
        }
    }
}
