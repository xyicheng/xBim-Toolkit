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
using Xbim.ModelGeometry.Scene.Clustering;
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
            public int GeometryHash;
            public int ReferenceCount;
            public XbimRect3D BoundingBox;
            public int StyleLabel;
            public bool IsCopy;

            public GeometryReferenceCounter(int geometryHash)
            {
                GeometryHash = geometryHash;
            }

            public GeometryReferenceCounter(int mapHash, int styleLabel, GeometryReferenceCounter grc)
            {
               
            }

            internal GeometryReferenceCounter Copy()
            {
                GeometryReferenceCounter copy = new GeometryReferenceCounter(GeometryHash); 
                copy.GeometryId = GeometryId;
                copy.ReferenceCount = ReferenceCount;
                copy.BoundingBox = BoundingBox;
                copy.StyleLabel = StyleLabel;
                copy.IsCopy = true;
                return copy;
            }
        }
        private struct GeometryReferenceCounterComparer : IEqualityComparer<GeometryReferenceCounter>
        {
            public bool Equals(GeometryReferenceCounter x, GeometryReferenceCounter y)
            {
                return x.GeometryId == y.GeometryId;
            }

            public int GetHashCode(GeometryReferenceCounter obj)
            {
                return obj.GeometryId;
            }
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
                if (obj == null || !(obj is RepresentationItemGeometricHashKey)) return false;

                return Item.GeometricEquals(((RepresentationItemGeometricHashKey)obj).Item);
            }

            public override string ToString()
            {
                return Item.ToString() + " Hash[" + _hash + "]";
            }
        }

        private static readonly ILogger Logger = LoggerFactory.GetLogger();
        private XbimModel _model;
        private IXbimGeometryEngine _engine;
        private IfcGeometricRepresentationContext _context;
        private short _contextId;

        private bool _contextIsPersisted;

        /// <summary>
        /// The numeric rounding for points
        /// </summary>
        private int _roundingPrecision;


        private ConcurrentDictionary<int, XbimShapeGeometry> _cachedShapes = new ConcurrentDictionary<int, XbimShapeGeometry>();



        /// <summary>
        /// Initialises a model context from the model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="contextType"></param>
        /// <param name="contextIdentifier"></param>
        public Xbim3DModelContext(XbimModel model, string contextType = "model", string contextIdentifier = "body")
        {
            _model = model;
            _engine = _model.GeometryEngine();
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
                _contextId = (short)((Math.Abs(_context.EntityLabel) >> 16) ^ Math.Abs(_context.EntityLabel));
                _contextIsPersisted = ProductShapes.Any();
            }
        }

        public bool CreateContext(ReportProgressDelegate progDelegate = null, bool cacheOn = true)
        {
            if (_context == null) return false;
            if (_contextIsPersisted) return false; //already created it
            double precision = _engine.Precision;
            //cache booleans only to avoid converting them twice when we process the solid model components
            bool wasCaching = _engine.Caching;
            if (cacheOn) _engine.CacheStart(true);

           
            
            ParallelOptions pOpts = new ParallelOptions();
            List<IfcProduct> products = _model.Instances.OfType<IfcProduct>().Where(p => p.Representation != null ).ToList();
            List<IfcElement> elements = products.OfType<IfcElement>().ToList();

            //get all the shapes that are 3D
            IEnumerable<IfcRepresentationItem> allShapes = Model.Instances.OfType<IfcSolidModel>();
            allShapes = allShapes.Concat(Model.Instances.OfType<IfcBooleanResult>());
            allShapes = allShapes.Concat(Model.Instances.OfType<IfcFaceBasedSurfaceModel>());
            allShapes = allShapes.Concat(Model.Instances.OfType<IfcShellBasedSurfaceModel>());
           // allShapes = allShapes.Concat(Model.Instances.OfType<IfcBoundingBox>());
            allShapes = allShapes.Concat(Model.Instances.OfType<IfcSectionedSpine>());
            List<IfcRepresentationItem> listShapes = allShapes.ToList();
            int total = products.Count + elements.Count + listShapes.Count;

            int tally = 0;
            int percentageParsed = 0;

            //Do the elements with openings and projectsion first, cache any geometry shapes created 
             ConcurrentDictionary<int, GeometryReferenceCounter> shapeLookup = new ConcurrentDictionary<int, GeometryReferenceCounter>();
             Parallel.ForEach<IfcElement>(elements, pOpts, element =>
           //foreach (var element in elements)
            {
                //select representations that are in the required context
                //only want solid representations for this context, but rep type is optional so just filter identified 2d elements
                //we can only handle one representation in a context and this is in an implementers agreement
                IfcRepresentation rep = element.Representation.Representations.Where(r =>
                    r.ContextOfItems == _context &&
                    r.IsBodyRepresentation())
                    .FirstOrDefault();
                //write out the representation if it has one
                if (rep != null)
                {
                    //create the  net product records (holes cut etc)
                    if (element.HasOpenings.Any() || element.HasProjections.Any()) //is it an element with openings or projections
                    {
                        XbimMatrix3D placementTransform = element.ObjectPlacement.ToMatrix3D();
                        XbimRect3D productBounds = XbimRect3D.Empty;
                        IEnumerable<int> geomIds = (WriteElementGeometryToDB(_model, _context, element, XbimMatrix3D.Identity, ref productBounds));
                        //transform the bounds to WCS
                        productBounds = productBounds.Transform(placementTransform);
                        //Write a geometry record to map this geometry to the product
                        WriteProductMapToDB(_model, geomIds, _context, element, placementTransform, productBounds, true);
                        shapeLookup.TryAdd(element.EntityLabel,new GeometryReferenceCounter(0));
                        
                    }
                }
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
           );

            //turn caching off as we don't want to keep all geometries in memory
            if (!wasCaching && cacheOn) _engine.CacheStop(false);


            ConcurrentDictionary<RepresentationItemGeometricHashKey, GeometryReferenceCounter> geomHash = new
                            ConcurrentDictionary<RepresentationItemGeometricHashKey, GeometryReferenceCounter>();
           
          
           
            ConcurrentDictionary<IfcRepresentationItem, GeometryReferenceCounter> mapLookup = new ConcurrentDictionary<IfcRepresentationItem, GeometryReferenceCounter>();
            Parallel.ForEach<IfcRepresentationItem>(listShapes, pOpts, shape =>
           // foreach (var shape in listShapes)
           {
              
               RepresentationItemGeometricHashKey keyValue = new RepresentationItemGeometricHashKey(shape);
               GeometryReferenceCounter counter = geomHash.AddOrUpdate(keyValue, new GeometryReferenceCounter(keyValue.GetHashCode()),
                            (key, oldValue) => { keyValue = key; Interlocked.Increment(ref  oldValue.ReferenceCount); return oldValue; });

               bool isMap = counter.ReferenceCount > 0; //we have already written it                           
               if (isMap) //need to create a  record when we have finished if it has a different style
               {
                   mapLookup.TryAdd(shape, counter);
               }
               else //create shape now
               {
                   WritePolygonalGeometryToDB(_model, shape, keyValue.GetHashCode(), ref counter, 1);
                   shapeLookup.TryAdd(shape.EntityLabel, counter);
               }
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
           );
            geomHash = null; //don't need this anymore 
            //Now tidy up the maps
            Parallel.ForEach<KeyValuePair<IfcRepresentationItem, GeometryReferenceCounter>>(mapLookup, pOpts, mapKV =>
            // foreach (var mapKV in mapLookup)
            {
                IfcSurfaceStyle surfaceStyle = mapKV.Key.SurfaceStyle();
                if (surfaceStyle != null && mapKV.Value.StyleLabel != Math.Abs(surfaceStyle.EntityLabel)) //we need to create a map record
                {
                    int mapStyleLabel = Math.Abs(surfaceStyle.EntityLabel);
                    int mapHash = mapKV.Value.GeometryHash ^ mapStyleLabel;
                    //write a map, bounding box is the same, there is no transform and the hash refelcts the same geometry shape but a different style
                    int geomId = WritePolygonalMapToDB(_model, new int[] { mapKV.Value.GeometryId }, mapKV.Key, XbimMatrix3D.Identity,
                        mapKV.Value.BoundingBox, mapHash);
                    GeometryReferenceCounter mapRefCounter = mapKV.Value.Copy();
                    mapRefCounter.GeometryHash = mapHash;
                    mapRefCounter.StyleLabel = mapStyleLabel;

                    shapeLookup.TryAdd(Math.Abs(mapKV.Key.EntityLabel), mapRefCounter);
                }
                else //just reuse the shape we already have written
                {
                    shapeLookup.TryAdd(Math.Abs(mapKV.Key.EntityLabel), mapKV.Value);
                }
            }
            );
            mapLookup = null; //don't need this any more

            //set up a bag element bounds for clustering
            ConcurrentQueue<XbimBBoxClusterElement> elementsToCluster = new ConcurrentQueue<XbimBBoxClusterElement>();

            Parallel.ForEach<IfcProduct>(products, pOpts, product =>
            //   foreach (var product in products)
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

                    //write out any shapes it has                   
                    foreach (var shape in rep.Items)
                    {
                        if (shape is IfcMappedItem)
                        {
                            IfcMappedItem map = shape as IfcMappedItem;
                            List<int> mapShapes = new List<int>();
                            XbimRect3D mapBounds = XbimRect3D.Empty;
                            int mapHash = 0;
                            foreach (var mapShape in map.MappingSource.MappedRepresentation.Items)
                            {
                                GeometryReferenceCounter counter;
                                if (shapeLookup.TryGetValue(Math.Abs(mapShape.EntityLabel), out counter))
                                {
                                    Interlocked.Increment(ref counter.ReferenceCount);
                                    mapHash += counter.GeometryHash;
                                    mapShapes.Add(counter.GeometryId);
                                    if (mapBounds.IsEmpty) mapBounds = counter.BoundingBox;
                                    else mapBounds.Union(counter.BoundingBox);
                                }
                                else
                                    Logger.ErrorFormat("Failed to find shape #{0}", Math.Abs(mapShape.EntityLabel));

                            }
                            XbimMatrix3D cartesianTransform = map.MappingTarget.ToMatrix3D();
                            XbimMatrix3D localTransform = map.MappingSource.MappingOrigin.ToMatrix3D();
                            XbimMatrix3D mapTransform = XbimMatrix3D.Multiply(cartesianTransform, localTransform);
                            mapBounds = XbimRect3D.TransformBy(mapBounds, mapTransform);
                            mapHash ^= mapTransform.GetHashCode();
                            geomLabels.Add(WritePolygonalMapToDB(_model, mapShapes, map, mapTransform, mapBounds, mapHash));
                            if (productBounds.IsEmpty) productBounds = mapBounds;
                            else productBounds.Union(mapBounds);
                        }
                        else  //it is a direct reference to s geometry shape
                        {
                            GeometryReferenceCounter counter;
                            if (shapeLookup.TryGetValue(Math.Abs(shape.EntityLabel), out counter))
                            {
                                geomLabels.Add(counter.GeometryId);
                                if (productBounds.IsEmpty)
                                    productBounds = counter.BoundingBox;
                                else
                                    productBounds.Union(counter.BoundingBox);
                            }
                            else
                                Logger.ErrorFormat("Failed to find shape #{0}", Math.Abs(shape.EntityLabel));
                        }
                    }

                    productBounds = productBounds.Transform(placementTransform);
                    GeometryReferenceCounter ctr;
                    bool isNettShape = !shapeLookup.TryGetValue(product.EntityLabel, out ctr);//if we have written the element before then this is the shape minus openings etc
                    int gid = WriteProductMapToDB(_model, geomLabels, _context, product, placementTransform, productBounds, isNettShape);
                    elementsToCluster.Enqueue(new XbimBBoxClusterElement(gid, productBounds));
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
          );


            //Write out the actual representation item reference count
            //get the unique values first
            var distincts = shapeLookup.Values.Distinct(new GeometryReferenceCounterComparer());
            Parallel.ForEach<GeometryReferenceCounter>(distincts, pOpts, geomShapeRef =>
            {

                if (geomShapeRef.ReferenceCount > 0)
                {
                    int cost = WritePolygonalGeometryReferenceCountToDB(_model, geomShapeRef);

                }
            });
            //Write out the regions of the model
            WriteRegionsToDB(_model, elementsToCluster);
            _contextIsPersisted = true;

            return true;
        }

        /// <summary>
        /// Returns true if the context has been processed and stored in the model, if false call CreateContext
        /// </summary>
        public bool IsGenerated
        {
            get
            {
                return _contextIsPersisted;
            }
        }


        private int WriteRegionsToDB(XbimModel model, IEnumerable<XbimBBoxClusterElement> elementsToCluster)
        {
            int cost = 0;
            //set up a world to partition the model
            double metre = _model.ModelFactors.OneMetre;
            XbimGeometryCursor geomTable = model.GetGeometryTable();
            XbimRegionCollection regions = new XbimRegionCollection();
            // the XbimDBSCAN method adopted for clustering produces clusters of contiguous elements.
            // if the maximum size is a problem they could then be split using other algorithms that divide spaces equally
            //
            var v = XbimDBSCAN.GetClusters(elementsToCluster, 5 * metre); // .OrderByDescending(x => x.GeometryIds.Count);
            int i = 1;
            foreach (var item in v)
            {
                regions.Add(new XbimRegion("Region " + i++, item.Bound, item.GeometryIds.Count));
            }

            try
            {
                XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
                geomTable.AddGeometry(_context.EntityLabel, XbimGeometryType.Region, IfcMetaData.IfcTypeId(_context.GetType()), XbimMatrix3D.Identity.ToArray(), regions.ToArray());
                transaction.Commit();
            }
            finally
            {
                model.FreeTable(geomTable);
            }
            return cost;

        }

        private int WritePolygonalGeometryReferenceCountToDB(XbimModel model, GeometryReferenceCounter geometryReferenceCounter)
        {
            int cost = 0;
            XbimGeometryCursor geomTable = model.GetGeometryTable();
            try
            {
                XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
                cost = geomTable.UpdateReferenceCount(geometryReferenceCounter.GeometryId, geometryReferenceCounter.ReferenceCount);
                transaction.Commit();
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Failed to update reference count on geometry #{0}", geometryReferenceCounter.GeometryId);
            }
            finally
            {
                model.FreeTable(geomTable);
            }
            return cost;
        }

        private IEnumerable<int> WriteElementGeometryToDB(XbimModel model, IfcGeometricRepresentationContext ctxt, IfcElement element, XbimMatrix3D placementTransform, ref XbimRect3D productBounds)
        {
            XbimGeometryCursor geomTable = model.GetGeometryTable();
            List<int> result = new List<int>();
            try
            {

                XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
                short typeId = IfcMetaData.IfcTypeId(element);
                if (element.EntityLabel == 254336)
                {
                    Logger.ErrorFormat("Failed to add element geometry for entity #{0}, no geometry generated", element.EntityLabel);
                }
                IXbimGeometryModelGroup geomModelGrp = element.Geometry3D();
                foreach (var geomModel in geomModelGrp)
                {
                  
                    string shapeData = geomModel.WriteAsString(model.ModelFactors); //gets the polygonal geometry as a string
                    int surfaceStyleLabel = geomModel.SurfaceStyleLabel;
                    if (productBounds.IsEmpty) productBounds = geomModel.GetBoundingBox();
                    else productBounds.Union(geomModel.GetBoundingBox());
                    int geomId = geomTable.AddGeometry(element.EntityLabel, XbimExtensions.XbimGeometryType.Polyhedron,
                                                   typeId, System.Text.Encoding.ASCII.GetBytes(productBounds.ToString()), System.Text.Encoding.ASCII.GetBytes(shapeData), (short)0, surfaceStyleLabel, productBounds.GetHashCode());
                    result.Add(geomId);
                }
                transaction.Commit();
                productBounds.Round(_roundingPrecision);

                return result;
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Failed to add element geometry for entity #{0}, reason {1}", element.EntityLabel, e.Message);
                return Enumerable.Empty<int>();
            }
            finally
            {
                model.FreeTable(geomTable);
            }
        }
        /// <summary>
        /// Writes the geometry as a string into the database
        /// </summary>
        /// <param name="model"></param>
        /// <param name="geom"></param>
        /// <param name="refCount">Number of other references to this geometry</param>
        /// <returns></returns>
        int WriteProductMapToDB(XbimModel model, IEnumerable<int> shapes, IfcGeometricRepresentationContext ctxt, IfcProduct product, XbimMatrix3D placementTransform, XbimRect3D productBounds, bool nettShape)
        {
            XbimGeometryCursor geomTable = model.GetGeometryTable();
            try
            {

                XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
                short typeId = IfcMetaData.IfcTypeId(product);
                string data = placementTransform.ToString() + "," + string.Join(",", shapes);
                //IfcMaterialSelect material = null;// product.GetMaterial();
                //int materialLabel = material != null ? Math.Abs(material.EntityLabel) : 0;
                int geomId = geomTable.AddGeometry(product.EntityLabel,
                    XbimGeometryType.ProductPolyhedronMap,
                    typeId,
                    System.Text.Encoding.ASCII.GetBytes(XbimRect3D.Round(productBounds, _roundingPrecision).ToString()),
                    System.Text.Encoding.ASCII.GetBytes(data),
                    nettShape ? _contextId : (short)-_contextId, /*Negative comtect ID is used to determine a gross shape, i.e. holes not cut*/
                    0
                    );
                transaction.Commit();

                return geomId;
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Failed to add product geometry for entity #{0}, reason {1}", product.EntityLabel, e.Message);
                return -1;
            }
            finally
            {
                model.FreeTable(geomTable);
            }
        }

        int WritePolygonalGeometryToDB(XbimModel model, IfcRepresentationItem geom, int hash, ref GeometryReferenceCounter refCounter, int refCount = 1)
        {
            XbimGeometryCursor geomTable = model.GetGeometryTable();
            try
            {
                XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
                IXbimGeometryModel geomModel = geom.Geometry3D();
                string shapeData = geomModel.WriteAsString(model.ModelFactors); //gets the polygonal geometry as a string
                refCounter.StyleLabel = geomModel.SurfaceStyleLabel;
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
                    refCounter.StyleLabel, hash);
                transaction.Commit();

                refCounter.GeometryId = geomId;
                return geomId;
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Failed to add shape geometry for entity #{0}, reason {1}", geom.EntityLabel, e.Message);
                return -1;
            }
            finally
            {
                model.FreeTable(geomTable);
            }
        }

        private int WritePolygonalMapToDB(XbimModel model, IEnumerable<int> sourceLabels, IfcRepresentationItem map, XbimMatrix3D transform, XbimRect3D bb, int mapHash)
        {
            XbimGeometryCursor geomTable = model.GetGeometryTable();
            try
            {

                XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
                short typeId = IfcMetaData.IfcTypeId(map);
                string data = transform.ToString() + "," + string.Join(",", sourceLabels);
                IfcSurfaceStyle surfaceStyle = map.SurfaceStyle();
                int surfaceStyleLabel = surfaceStyle != null ? surfaceStyle.EntityLabel : 0;
                int geomId = geomTable.AddGeometry(map.EntityLabel, XbimGeometryType.PolyhedronMap,
                    typeId, System.Text.Encoding.ASCII.GetBytes(bb.ToString()), System.Text.Encoding.ASCII.GetBytes(data), 0, surfaceStyleLabel, mapHash);
                transaction.Commit();

                return geomId;
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Failed to add map geometry for entity #{0}, reason {1}", map.EntityLabel, e.Message);
                return -1;
            }
            finally
            {
                model.FreeTable(geomTable);
            }
        }

        public XbimProductShape GetProductShape(IfcProduct product)
        {
            //get the product shapes
           return  new XbimProductShape(this, _model.GetGeometryData(product,XbimGeometryType.ProductPolyhedronMap)
               .Where(d=>d.Counter==_contextId).FirstOrDefault());
        }

        public IEnumerable<XbimProductShape> ProductShapes
        {
            get
            {
                //get all the product shapes

                XbimGeometryCursor geomTable = _model.GetGeometryTable();
                try
                {
                    using (var transaction = geomTable.BeginReadOnlyTransaction())
                    {
                        foreach (var item in geomTable.GeometryData(XbimGeometryType.ProductPolyhedronMap))
                        {
                            if (item.Counter == _contextId)
                            {
                                yield return new XbimProductShape(this, item);
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

        internal IEnumerable<XbimShape> Shapes(IEnumerable<int> geomIDs, bool cache = true)
        {
            XbimGeometryCursor geomTable = Model.GetGeometryTable();
            try
            {

                using (var transaction = geomTable.BeginReadOnlyTransaction())
                {

                    foreach (var geomId in geomIDs)
                    {
                        XbimShapeGeometry shape;
                        if (_cachedShapes.TryGetValue(geomId, out shape))
                        {
                            yield return shape;
                        }
                        else
                        {
                            XbimGeometryData data = geomTable.GetGeometryData(geomId);
                            if (data.GeometryType == XbimGeometryType.Polyhedron)
                            {
                                yield return ParseGeometryData(data, cache);
                            }
                            else if (data.GeometryType == XbimGeometryType.PolyhedronMap)
                            {
                                //ADD EACH SHAPE IN THE MAP
                                string shapeString = System.Text.Encoding.ASCII.GetString(data.ShapeData);
                                string[] itms = shapeString.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                                XbimMatrix3D transform = XbimMatrix3D.FromString(itms[0]);
                                for (int i = 1; i < itms.Length; i++)
                                {
                                    int shapeId = Convert.ToInt32(itms[i]);
                                    data = geomTable.GetGeometryData(shapeId);
                                    shape = ParseGeometryData(data, cache);
                                    yield return new XbimShapeMap(geomId, shape, transform);

                                }

                            }
                            else
                                throw new XbimGeometryException("Unexpected geometry type " + Enum.GetName(typeof(XbimGeometryType), data.GeometryType));
                        }
                    }
                }
            }
            finally
            {
                Model.FreeTable(geomTable);
            }
        }


        private XbimShapeGeometry ParseGeometryData(XbimGeometryData data, bool cache)
        {
            int l = data.IfcProductLabel;
            Type type = IfcMetaData.GetType(data.IfcTypeId);
            string boundsString = System.Text.Encoding.ASCII.GetString(data.DataArray2);
            int hash = data.GeometryHash;
            XbimRect3D boundingBox = XbimRect3D.Parse(boundsString);
            string geometryString = System.Text.Encoding.ASCII.GetString(data.ShapeData);
            //  IXbimGeometryModel geometry = _engine.GetGeometry3D(geometryString, data.GeometryType);
            int stylelabel = data.StyleLabel;
            XbimShapeGeometry shape = new XbimShapeGeometry(data.GeometryLabel, l, type, boundingBox, geometryString, stylelabel, hash, data.Counter);
            if (cache) _cachedShapes.TryAdd(data.GeometryLabel, shape);
            return shape;
        }


        public XbimModel Model
        {
            get
            {
                return _model;
            }
        }

        public XbimRegion GetLargestRegion()
        {
            if (_context == null) return null; //nothing to do
            IEnumerable<XbimGeometryData> regionDataColl = _model.GetGeometryData(Math.Abs(_context.EntityLabel), XbimGeometryType.Region); 
            //get the region data should only be one
            if (regionDataColl != null && regionDataColl.Any())
            {
                XbimGeometryData regionData = regionDataColl.FirstOrDefault();
                XbimRegionCollection regions = XbimRegionCollection.FromArray(regionData.ShapeData);
                return regions.MostPopulated();
            }
            else
                return null;
        }
    }

}
