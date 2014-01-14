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
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Common.Exceptions;
using Xbim.Common.Logging;
using System.Threading;
using Xbim.XbimExtensions;
namespace Xbim.ModelGeometry.Converter
{

    /// <summary>
    /// Represents a gemetric representation context, i.e. a 'Body' Representation
    /// </summary>
    public class Xbim3DModelContext
    {
        //private class to hold details of references to geometries
        private class GeometryReferenceCounter
        {
            public int GeometryId;
            public int ReferenceCount;
            public XbimRect3D BoundingBox;
        }

        private struct RepresentationItemGeometricHashKey
        {
            private int _hash;
            public IfcRepresentationItem Item;
            public RepresentationItemGeometricHashKey(IfcRepresentationItem item)
            {
                Item = item;
                _hash = Item.GetGeometryHashCode();
            }

            public override int GetHashCode()
            {
                return _hash;
            }

            public override bool Equals(object obj)
            {
                if(obj==null || !(obj is RepresentationItemGeometricHashKey)) return false;

                return Item.GeometricEquals(((RepresentationItemGeometricHashKey)obj).Item);
            }

            public override string ToString()
            {
                return Item.ToString() + " Hash[" + _hash + "]";
            }
        }

        private static readonly ILogger Logger = LoggerFactory.GetLogger();
        private XbimModel _model;
        private IfcGeometricRepresentationContext _context;
        /// <summary>
        /// The numeric rounding for points
        /// </summary>
        private int _roundingPrecision;


        private Dictionary<int, XbimShape> _shapes;
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
        /// Returns the Product Definition Shape in this context for the set of IfcProducts
        /// </summary>
        /// <param name="products"></param>
        public string GetShape(int libraryUseageLevel, params IfcProduct[] products)
        {
            return "";
        }


        /// <summary>
        /// Initialises a model context from the model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="contextType"></param>
        /// <param name="contextIdentifier"></param>
        public Xbim3DModelContext(XbimModel model, string contextType = "model", string contextIdentifier = "body")
        {
            _model = model;
            //Get the required context
            List<IfcGeometricRepresentationContext> contexts = model.Instances.OfType<IfcGeometricRepresentationContext>().Where(c => String.Compare(c.ContextType, contextType, true) == 0).ToList();

            if (contextIdentifier != null && contexts.Count > 1) //filter on the identifier if defined and we have more than one model context
                contexts = contexts.Where(c => c.ContextIdentifier.HasValue && contextIdentifier.ToLower()
                    .Contains(c.ContextIdentifier.Value.ToString().ToLower())).ToList();
            if (contexts.Count != 1)
                Logger.ErrorFormat("The Geometric Representation contexts are incorrectly defined. There should be one IfcGeometricRepresentationSubContext with ContextType = 'Model' and Identifier = 'Body'. This model has {0}", contexts.Count);
            else
            {
                _context = contexts.First();
                _roundingPrecision = Math.Abs((int)Math.Log10(_context.DefaultPrecision));
            }
        }

        public void CreateContext(ReportProgressDelegate progDelegate = null, bool cacheOn = true)
        {
            if (_context == null) return;
           
            
            //get the Geometry engine so that we can control caching
            IXbimGeometryEngine engine = _model.GeometryEngine();
            double precision = engine.Precision;
            //cache booleans only to avoid converting them twice when we process the solid model components
            bool wasCaching = engine.Caching;
            if(cacheOn) engine.CacheStart(true);

            //Do the BooleanResults first
            ParallelOptions pOpts = new ParallelOptions();

            //create the product records and record the reference counts

            ConcurrentDictionary<RepresentationItemGeometricHashKey, GeometryReferenceCounter> geomHash = new
                            ConcurrentDictionary<RepresentationItemGeometricHashKey, GeometryReferenceCounter>();
            int tally = 0;
            int percentageParsed = 0;
            List<IfcProduct> products = _model.Instances.OfType<IfcProduct>().Where(p => p.Representation != null).ToList();
            int total = products.Count;
        //    Parallel.ForEach<IfcProduct>(products, pOpts, product =>
            foreach (var product in _model.Instances.OfType<IfcProduct>().Where(p => p.Representation != null))
            {
                //select representations that are in the required context
                //only want solid representations for this context, but rep type is optional so just filter identified 2d elements
                //we can only handle one representation in a context and this is in an implementers agreement
                IfcRepresentation rep = product.Representation.Representations.Where(r =>
                    r.ContextOfItems == _context &&
                    r.IsBodyRepresentation())
                    .FirstOrDefault();
                //write out the representation if it has one
                if (rep != null)
                {
                    XbimMatrix3D placementTransform = product.ObjectPlacement.ToMatrix3D();
                    List<int> geomLabels = new List<int>(rep.Items.Count);    //prepare a list for actual keys                   
                    XbimRect3D productBounds = XbimRect3D.Empty;
                    //create the  net product records (holes cut etc)
                    IfcElement element = product as IfcElement;
                    if (element != null && (element.HasOpenings.Any() || element.HasProjections.Any())) //is it an element with openings or projections
                    {
                        geomLabels.Add(WriteElementGeometryToDB(_model, _context, element, placementTransform, ref productBounds));
                        //  change the shape ref to  be the label for this geeometry Id            
                    }
                    else
                    {
                        //write out any shapes it has                   
                        foreach (var shape in rep.Items)
                        {
                            if (shape is IfcMappedItem)
                            {
                                IfcMappedItem map = shape as IfcMappedItem;
                                List<int> mapShapes = new List<int>();
                                XbimRect3D mapBounds = XbimRect3D.Empty;
                                foreach (var mapShape in map.MappingSource.MappedRepresentation.Items)
                                {
                                    if (mapShape is IfcSolidModel
                                        || mapShape is IfcBooleanResult
                                        || mapShape is IfcFaceBasedSurfaceModel
                                        || shape is IfcShellBasedSurfaceModel
                                        || shape is IfcBoundingBox
                                        || shape is IfcSectionedSpine)
                                    {
                                        RepresentationItemGeometricHashKey keyValue = new RepresentationItemGeometricHashKey(mapShape);
                                        GeometryReferenceCounter counter = geomHash.AddOrUpdate(keyValue, new GeometryReferenceCounter(),
                                            (key, oldValue) => { keyValue = key; Interlocked.Increment(ref  oldValue.ReferenceCount); return oldValue; });
                                        bool isMap = counter.ReferenceCount > 0; //we have already written it
                                        if (!isMap) //need to write the shape
                                        {
                                            WritePolygonalGeometryToDB(_model, mapShape,keyValue.GetHashCode(), ref counter, 1);
                                        }
                                        mapShapes.Add(counter.GeometryId);
                                        if (mapBounds.IsEmpty) mapBounds = counter.BoundingBox;
                                        else mapBounds.Union(counter.BoundingBox);
                                    }
                                    else
                                        Logger.Warn("Unsupported geometry type " + shape.GetType().Name + " ignored");

                                }
                                XbimMatrix3D cartesianTransform = map.MappingTarget.ToMatrix3D();
                                XbimMatrix3D localTransform = map.MappingSource.MappingOrigin.ToMatrix3D();
                                XbimMatrix3D mapTransform = XbimMatrix3D.Multiply(cartesianTransform, localTransform);
                                mapBounds = XbimRect3D.TransformBy(mapBounds, mapTransform);
                                geomLabels.Add(WritePolygonalMapToDB(_model, mapShapes, map, mapTransform, mapBounds));
                                if (productBounds.IsEmpty) productBounds = mapBounds;
                                else productBounds.Union(mapBounds);
                            }
                            else if (shape is IfcSolidModel
                                || shape is IfcBooleanResult
                                || shape is IfcFaceBasedSurfaceModel
                                || shape is IfcShellBasedSurfaceModel
                                || shape is IfcBoundingBox
                                || shape is IfcSectionedSpine)
                            {
                                RepresentationItemGeometricHashKey keyValue = new RepresentationItemGeometricHashKey(shape);
                                GeometryReferenceCounter counter = geomHash.AddOrUpdate(keyValue, new GeometryReferenceCounter(),
                                             (key, oldValue) => { keyValue = key; Interlocked.Increment(ref  oldValue.ReferenceCount); return oldValue; });
                                bool isMap = counter.ReferenceCount > 0; //we have already written it                           
                                if (!isMap) //need to create a  record
                                    WritePolygonalGeometryToDB(_model, shape, keyValue.GetHashCode(), ref counter, 1);
                                geomLabels.Add(counter.GeometryId);
                                if (productBounds.IsEmpty) productBounds = counter.BoundingBox;
                                else productBounds.Union(counter.BoundingBox);
                            }
                            else
                                Logger.Warn("Unsupported geometry type " + shape.GetType().Name + " ignored");

                        }
                    }
                    productBounds = productBounds.Transform(placementTransform);
                    WriteProductMapToDB(_model, geomLabels, _context, product, placementTransform, productBounds);
                    Interlocked.Increment(ref tally);
                    if (progDelegate != null)
                    {
                        int newPercentage = Convert.ToInt32((double)tally / total * 100.0);
                        if (newPercentage > percentageParsed)
                        {
                            percentageParsed = newPercentage;
                            progDelegate(percentageParsed, "Meshing");
                        }
                    }
                }
            }
        //  );
           
            //turn caching off as we don't want to keep all geometries in memory
            if (!wasCaching && cacheOn)  engine.CacheStop(false);
            int totalMapCost = 0;
            //Write out the actual representation item reference count
            Parallel.ForEach<KeyValuePair<RepresentationItemGeometricHashKey, GeometryReferenceCounter>>(geomHash, pOpts, geom =>
            {

                if (geom.Value.ReferenceCount > 0)
                {
                    int cost = WritePolygonalGeometryReferenceCountToDB(_model, geom.Value);
                    Interlocked.Add(ref totalMapCost, cost);
                   
                }
            });


        }

        private int WritePolygonalGeometryReferenceCountToDB(XbimModel model, GeometryReferenceCounter geometryReferenceCounter)
        {
            XbimGeometryCursor geomTable = model.GetGeometryTable();
            XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
            int cost = geomTable.UpdateReferenceCount(geometryReferenceCounter.GeometryId, geometryReferenceCounter.ReferenceCount);
            transaction.Commit();
            model.FreeTable(geomTable);
            return cost;
        }

        private int WriteElementGeometryToDB(XbimModel model, IfcGeometricRepresentationContext ctxt, IfcElement element, XbimMatrix3D placementTransform, ref XbimRect3D productBounds)
        {
            try
            {
                XbimGeometryCursor geomTable = model.GetGeometryTable();
                XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
                short typeId = IfcMetaData.IfcTypeId(element);
                IXbimGeometryModel geomModel = element.Geometry3D();
                string shapeData = geomModel.WriteAsString(model.ModelFactors); //gets the polygonal geometry as a string
                int surfaceStyleLabel = geomModel.SurfaceStyleLabel;
                productBounds = geomModel.GetBoundingBox();
                productBounds.Round(_roundingPrecision);
                int geomId = geomTable.AddGeometry(element.EntityLabel, XbimExtensions.XbimGeometryType.Polyhedron,
                                                   typeId, System.Text.Encoding.ASCII.GetBytes(productBounds.ToString()), System.Text.Encoding.ASCII.GetBytes(shapeData), (short)0, surfaceStyleLabel, Math.Abs(ctxt.EntityLabel));
                transaction.Commit();
                model.FreeTable(geomTable);
                return geomId;
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Failed to add element geometry for entity #{0}, reason {1}", element.EntityLabel, e.Message);
                return -1;
            }
        }
        /// <summary>
        /// Writes the geometry as a string into the database
        /// </summary>
        /// <param name="model"></param>
        /// <param name="geom"></param>
        /// <param name="refCount">Number of other references to this geometry</param>
        /// <returns></returns>
        int WriteProductMapToDB(XbimModel model, IEnumerable<int> shapes, IfcGeometricRepresentationContext ctxt, IfcProduct product, XbimMatrix3D placementTransform, XbimRect3D productBounds)
        {
            try
            {
                XbimGeometryCursor geomTable = model.GetGeometryTable();
                XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
                short typeId = IfcMetaData.IfcTypeId(product);
                string data = placementTransform.ToString() + "," + string.Join(",", shapes);
                IfcMaterialSelect material = product.GetMaterial();
                int materialLabel = material != null ? Math.Abs(material.EntityLabel) : 0;
                int geomId = geomTable.AddGeometry(product.EntityLabel, 
                    XbimGeometryType.ProductPolyhedronMap, 
                    typeId, 
                    System.Text.Encoding.ASCII.GetBytes(XbimRect3D.Round(productBounds, _roundingPrecision).ToString()),
                    System.Text.Encoding.ASCII.GetBytes(data),
                    0, 
                    materialLabel, 
                    Math.Abs(ctxt.EntityLabel));
                transaction.Commit();
                model.FreeTable(geomTable);
                return geomId;
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Failed to add product geometry for entity #{0}, reason {1}", product.EntityLabel, e.Message);
                return -1;
            }
        }

        int WritePolygonalGeometryToDB(XbimModel model, IfcRepresentationItem geom, int hash, ref GeometryReferenceCounter refCounter,  int refCount = 1)
        {
            try
            {
                
                XbimGeometryCursor geomTable = model.GetGeometryTable();
                XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
                IXbimGeometryModel geomModel = geom.Geometry3D();
                string shapeData = geomModel.WriteAsString(model.ModelFactors); //gets the polygonal geometry as a string
                int surfaceStyleLabel = geomModel.SurfaceStyleLabel;
                XbimRect3D bb = geomModel.GetBoundingBox();
                bb.Round(_roundingPrecision);
                refCounter.BoundingBox = bb;
                short typeId = IfcMetaData.IfcTypeId(geom);
                int geomId = geomTable.AddGeometry(geom.EntityLabel, 
                    XbimGeometryType.Polyhedron,
                    typeId, 
                    System.Text.Encoding.ASCII.GetBytes(bb.ToString()), 
                    System.Text.Encoding.ASCII.GetBytes(shapeData),
                    0, 
                    surfaceStyleLabel, hash);
                transaction.Commit();
                model.FreeTable(geomTable);
                refCounter.GeometryId = geomId;
                return geomId;
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Failed to add shape geometry for entity #{0}, reason {1}", geom.EntityLabel, e.Message);
                return -1;
            }
        }

        private int WritePolygonalMapToDB(XbimModel model, IEnumerable<int> sourceLabels, IfcRepresentationItem map, XbimMatrix3D transform, XbimRect3D bb)
        {
            try
            {
                XbimGeometryCursor geomTable = model.GetGeometryTable();
                XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
                short typeId = IfcMetaData.IfcTypeId(map);
                string data = transform.ToString() + "," + string.Join(",", sourceLabels);
                IfcSurfaceStyle surfaceStyle = map.SurfaceStyle();
                int surfaceStyleLabel = surfaceStyle != null ? surfaceStyle.EntityLabel : 0;
                int geomId = geomTable.AddGeometry(map.EntityLabel, XbimGeometryType.PolyhedronMap,
                    typeId, System.Text.Encoding.ASCII.GetBytes(bb.ToString()), System.Text.Encoding.ASCII.GetBytes(data), 0, surfaceStyleLabel);
                transaction.Commit();
                model.FreeTable(geomTable);
                return geomId;
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Failed to add map geometry for entity #{0}, reason {1}", map.EntityLabel, e.Message);
                return -1;
            }
        }

 
        public IEnumerable<XbimProductShape> ProductShapes
        {
            get
            {
                //get all the product shapes
                int contextLabel = Math.Abs(_context.EntityLabel);
                XbimGeometryCursor geomTable = _model.GetGeometryTable();
                try
                {
                    using (var transaction = geomTable.BeginReadOnlyTransaction())
                    {
                        foreach (var item in geomTable.GeometryData(XbimGeometryType.ProductPolyhedronMap))
                        {
                            if (item.GeometryHash == contextLabel)
                            {
                                yield return new XbimProductShape(_model, item);
                            }
                        }
                    }
                }
                finally
                {
                    _model.FreeTable(geomTable);
                }
            }
        }

       
    }
}
