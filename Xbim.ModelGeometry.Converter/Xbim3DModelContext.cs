using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.RepresentationResource;
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
using System.Collections.ObjectModel;
using Newtonsoft.Json;
namespace Xbim.ModelGeometry.Converter
{

    /// <summary>
    /// Represents a gemetric representation context, i.e. a 'Body' and 'Model'Representation
    /// Note a 3DModelContext may contain multiple IfcGeometricRepresentationContexts
    /// </summary>
    public class Xbim3DModelContext
    {
        #region Helper classes


        //private struct to hold details of references to geometries
        private struct GeometryReference
        {
            public int GeometryId;
            public XbimRect3D BoundingBox;
            public int StyleLabel;
        }

        private struct RepresentationItemGeometricHashKey
        {
            private int _hash;
            public IfcRepresentationItem Item;
            public RepresentationItemGeometricHashKey(IfcRepresentationItem item)
            {
                Item = item;
                try
                {
                    _hash = Item.GetGeometryHashCode();
                }
                catch (XbimGeometryException eg)
                {

                    Logger.WarnFormat("HashCode error in representation of type {0}, err = {1}", item.GetType().Name, eg.Message);
                    _hash = 0;
                }

            }

            public override int GetHashCode()
            {
                return _hash;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is RepresentationItemGeometricHashKey)) return false;
                try
                {
                    return Item.GeometricEquals(((RepresentationItemGeometricHashKey)obj).Item);
                }
                catch (XbimGeometryException eg)
                {
                    Logger.WarnFormat("Equality error in representation of type {0}, err = {1}", Item.GetType().Name, eg.Message);
                    return false;
                }

            }

            public override string ToString()
            {
                return Item.ToString() + " Hash[" + _hash + "]";
            }
        }
        private class IfcRepresentationContextCollection : KeyedCollection<int, IfcRepresentationContext>
        {
            protected override int GetKeyForItem(IfcRepresentationContext item)
            {
                return (int)item.EntityLabel;
            }
        }
        #endregion
        private static readonly ILogger Logger = LoggerFactory.GetLogger();
        private XbimModel _model;
        private IXbimGeometryEngine _engine;
        private IfcRepresentationContextCollection _contexts;
        private Dictionary<short, IfcRepresentationContext> _contextLookup;
        private Dictionary<IfcRepresentationContext, int> _roundingPrecisions;
        private bool _contextIsPersisted;

        /// <summary>
        /// The numeric rounding for points
        /// </summary>


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
            //check for old versions
            IEnumerable<IfcGeometricRepresentationContext> contexts = model.Instances.OfType<IfcGeometricRepresentationContext>().Where(c => String.Compare(c.ContextType, contextType, true) == 0 || String.Compare(c.ContextType, "design", true) == 0); //allow for incorrect older models
           

            if (contextIdentifier != null && contexts.Any()) //filter on the identifier if defined and we have more than one model context
            {
                IEnumerable<IfcGeometricRepresentationContext> subContexts = contexts.Where(c => c.ContextIdentifier.HasValue && contextIdentifier.ToLower()
                    .Contains(c.ContextIdentifier.Value.ToString().ToLower())).ToList();
                if (subContexts.Any()) contexts = subContexts; //filter to use body if specified, if not just strtick with the generat model context (avoids problems with earlier Revit exports where sub contexts were not given)
            }
            if (!contexts.Any())
            {

                //have a look for older standards
                contexts = model.Instances.OfType<IfcGeometricRepresentationContext>().Where(c => String.Compare(c.ContextType, "design", true) == 0 || String.Compare(c.ContextType, "model", true) == 0 );
                if (contexts.Any())
                {
                    Logger.InfoFormat("Unable to find any Geometric Representation contexts with Context Type = {0} and Context Identifier = {1}, using Context Type = 'Design' instead. NB This does not comply with IFC 2x3 or greater, the schema is {2}", contextType, contextIdentifier, string.Join(",", model.Header.FileSchema.Schemas));
                }
                else
                {
                    contexts = model.Instances.OfType<IfcGeometricRepresentationContext>();
                    if (contexts.Any())
                    {
                        string ctxtString = "";
                        foreach (var ctxt in contexts)
                        {
                            ctxtString += ctxt.ContextType + " ";
                        }
                        if (string.IsNullOrWhiteSpace(ctxtString)) ctxtString = "$";
                        Logger.InfoFormat("Unable to find any Geometric Representation contexts with Context Type = {0} and Context Identifier = {1}, using  available Context Types '{2}'. NB This does not comply with IFC 2x2 or greater", contextType, contextIdentifier, ctxtString.TrimEnd(' '));
                    }
                    else
                    {
                        Logger.ErrorFormat("Unable to find any Geometric Representation contexts in this file, it is illegal and does not comply with IFC 2x2 or greater");
                    }
                }
            }

            if (contexts.Any())
            {
                _contexts = new IfcRepresentationContextCollection();
                _roundingPrecisions = new Dictionary<IfcRepresentationContext, int>();
                _contextLookup = new Dictionary<short, IfcRepresentationContext>();
                foreach (var context in contexts)
                {
                    _contexts.Add(context);
                    if (context.DefaultPrecision == 0)
                        _roundingPrecisions.Add(context, 0);
                    else
                        _roundingPrecisions.Add(context, Math.Abs((int)Math.Log10(context.DefaultPrecision)));
                    _contextLookup.Add(GetContextID(context), context);
                }
                _contextIsPersisted = false;
            }
        }

        private static short GetContextID(IfcRepresentationContext context)
        {
            return (short)((context.EntityLabel >> 16) ^ context.EntityLabel);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="includeOpeningsAndProjections">If true Openings and projections and the uncut bodies of elements are written out separately otherwise only elements with openings and projections applied to geometries are written</param>
        /// <param name="progDelegate"></param>
        /// <param name="cacheOn"></param>
        /// <returns></returns>
        public bool CreateContext(bool includeOpeningsAndProjections = false, ReportProgressDelegate progDelegate = null, bool cacheOn = true)
        {
            if (_contexts == null || _engine == null) return false;
            if (_contextIsPersisted) return false; //already created it
            double precision = _engine.Precision;
            //cache booleans only to avoid converting them twice when we process the solid model components
            bool wasCaching = _engine.Caching;
            if (cacheOn) _engine.CacheStart(true);
            ParallelOptions pOpts = new ParallelOptions();
            
            HashSet<int> mappedShapeIds; 
            HashSet<int> productShapeIds = GetProductShapeIds(out mappedShapeIds);
            List<IGrouping<IfcElement, IfcFeatureElement>> openingsAndProjections = GetOpeningsAndProjections();

            //HashSet<int> elementIDsForVoidsandProjectionOps = GetElementIDsForVoidandProjectionOperations();
            ConcurrentDictionary<int, IXbimGeometryModel> curvedShapes = new ConcurrentDictionary<int, IXbimGeometryModel>();
            //
            int total = productShapeIds.Count() + openingsAndProjections.Count();
            int tally = 0;
            int percentageParsed = 0;

            //Do the elements with openings and projectsion first, cache any geometry shapes created 
            ConcurrentDictionary<int, GeometryReference> shapeLookup = new ConcurrentDictionary<int, GeometryReference>();

            //turn caching off as we don't want to keep all geometries in memory
            if (!wasCaching && cacheOn) _engine.CacheStop(false);

            //Get the aurface styles
            Dictionary<int, int> surfaceStyles = GetSurfaceStyles();
            //init clusters
            //set up a bag element bounds for clustering for each context
            Dictionary<IfcRepresentationContext, ConcurrentQueue<XbimBBoxClusterElement>> clusters = new Dictionary<IfcRepresentationContext, ConcurrentQueue<XbimBBoxClusterElement>>();
            foreach (var context in _contexts) clusters.Add(context, new ConcurrentQueue<XbimBBoxClusterElement>());
            //keep a list of maps written as some tools incorrectly reuse the product definition shape and reuse the mapped item this way
            ConcurrentDictionary<int, List<GeometryReference>> mapsWritten = new ConcurrentDictionary<int, List<GeometryReference>>();
            ConcurrentDictionary<int, XbimRect3D> allMapBounds = new ConcurrentDictionary<int, XbimRect3D>();
        
            using (BlockingCollection<XbimShapeGeometry> shapeGeometries = new BlockingCollection<XbimShapeGeometry>())
            {
                // A simple blocking consumer with no cancellation that takes shapes of the queue and stores them in the database
                using (Task writeToDB = Task.Factory.StartNew(() => WriteShapeGeometriesToDatabase(shapeLookup, surfaceStyles, shapeGeometries)))
                {
                    try
                    {
                        
                        //remove any implicit duplicate geometries by comparing their IFC definitions
                        int deduplicateCount = WriteShapeGeometries(productShapeIds, shapeLookup, surfaceStyles, total, ref tally, ref percentageParsed, progDelegate, pOpts, shapeGeometries, curvedShapes);
                        
                    }
                    finally
                    {
                        //wait for the geometry shapes to be written
                        shapeGeometries.CompleteAdding();
                        writeToDB.Wait();
                    }
                }
            }
            WriteMappedItems(pOpts, shapeLookup, mappedShapeIds, mapsWritten, allMapBounds, surfaceStyles);
            using (BlockingCollection<Tuple<XbimShapeInstance, XbimShapeGeometry>> features = 
                                                                    new BlockingCollection<Tuple<XbimShapeInstance, XbimShapeGeometry>>())
            {
                //start a new task to process features
                HashSet<int> processed;
                using (Task writeToDB = Task.Factory.StartNew(() => WriteFeatureElementsToDatabase(features)))
                {
                    try
                    {
                        processed = WriteFeatureElements(openingsAndProjections, shapeLookup, mapsWritten, allMapBounds, curvedShapes, clusters, features, total, ref tally, ref percentageParsed, progDelegate);
                    }
                    finally
                    {
                        //wait for the geometry shapes to be written
                        features.CompleteAdding();
                        writeToDB.Wait();
                    }
                }
                List<IfcProduct> productsRemaining = _model.Instances.OfType<IfcProduct>()
                           .Where(p => p.Representation != null && !processed.Contains(p.EntityLabel)).ToList();

                WriteProductShapes(includeOpeningsAndProjections, progDelegate, pOpts, productsRemaining, total, ref tally, ref percentageParsed, shapeLookup, mapsWritten, allMapBounds, clusters);

                //Write out the actual representation item reference count
                WriteShapeGeometryReferenceCountToDB();

                //Write out the regions of the model
                foreach (var cluster in clusters)
                {
                    WriteRegionsToDB(_model, cluster.Key, cluster.Value);
                }

                _contextIsPersisted = true;

                return true;
            }
        }

        private void WriteShapeGeometriesToDatabase(ConcurrentDictionary<int, GeometryReference> shapeLookup, Dictionary<int, int> surfaceStyles, BlockingCollection<XbimShapeGeometry> shapeGeometries)
        {
            XbimShapeGeometry shapeGeom = new XbimShapeGeometry() ;
            XbimShapeGeometryCursor geomTable = _model.GetShapeGeometryTable();
            try
            {
                using (XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction())
                {

                    int geomCount = 0;
                    int _transactionBatchSize = 100;

                    while (!shapeGeometries.IsCompleted)
                    {
                        try
                        {
                            shapeGeom = shapeGeometries.Take();
                            GeometryReference refCounter = new GeometryReference();                        
                            refCounter.BoundingBox = shapeGeom.BoundingBox;
                            refCounter.GeometryId = geomTable.AddGeometry(shapeGeom);
                            int styleLabel;
                            surfaceStyles.TryGetValue(shapeGeom.IfcShapeLabel, out styleLabel);
                            refCounter.StyleLabel = styleLabel;
                            shapeLookup.TryAdd(shapeGeom.IfcShapeLabel, refCounter);
                            geomCount++;
                        }
                        catch (InvalidOperationException)
                        {
                            break;
                        }
                        catch (Exception e)
                        {        
                             Logger.ErrorFormat("Failed to write entity #{0} to database, error = {1}", shapeGeom.IfcShapeLabel, e.Message);
                           
                        }
                        long remainder = geomCount % _transactionBatchSize; //pulse transactions
                        if (remainder == _transactionBatchSize - 1)
                        {
                            transaction.Commit();
                            transaction.Begin();
                        }
                    }
                    transaction.Commit();
                }
            }
            finally
            {
                _model.FreeTable(geomTable);
            }
        }
        private void WriteFeatureElementsToDatabase(BlockingCollection<Tuple<XbimShapeInstance, XbimShapeGeometry>> shapeFeatures)
        {
            Tuple<XbimShapeInstance, XbimShapeGeometry> shapeFeature;
            XbimShapeGeometry shapeGeometry;
            XbimShapeInstance shapeInstance;
            XbimShapeInstanceCursor instanceTable = _model.GetShapeInstanceTable();
            XbimShapeGeometryCursor geomTable = _model.GetShapeGeometryTable();
            try
            {
                using (XbimLazyDBTransaction instanceTransaction = instanceTable.BeginLazyTransaction())
                {
                    using (XbimLazyDBTransaction geomTransaction = geomTable.BeginLazyTransaction())
                    {
                        int instanceCount = 0;
                        int _transactionBatchSize = 100;

                        while (!shapeFeatures.IsCompleted)
                        {
                            try
                            {
                                shapeFeature = shapeFeatures.Take();
                                shapeInstance = shapeFeature.Item1;
                                shapeGeometry = shapeFeature.Item2;
                                shapeInstance.ShapeGeometryLabel = geomTable.AddGeometry((IXbimShapeGeometryData)shapeGeometry);
                                instanceTable.AddInstance((IXbimShapeInstanceData)shapeInstance);
                            }
                            catch (InvalidOperationException)
                            {
                                break;
                            }
                            instanceCount++;
                            //localTally++;
                            //if (progDelegate != null)
                            //{
                            //    int newPercentage = Convert.ToInt32((double)localTally / total * 100.0);
                            //    if (newPercentage > localPercentageParsed)
                            //    {
                            //        localPercentageParsed = newPercentage;
                            //        progDelegate(localPercentageParsed, "Meshing");
                            //    }
                            //}
                            long remainder = instanceCount % _transactionBatchSize; //pulse transactions
                            if (remainder == _transactionBatchSize - 1)
                            {
                                instanceTransaction.Commit();
                                instanceTransaction.Begin();
                                geomTransaction.Commit();
                                geomTransaction.Begin();
                            }
                        }
                        geomTransaction.Commit();
                    }
                    instanceTransaction.Commit();
                }
            }
            finally
            {
                _model.FreeTable(instanceTable);
                _model.FreeTable(geomTable);
            }
        }

        /// <summary>
        /// Returns a set of element IDs that will need to be used later in boolean operations for cutting voids and adding projections
        /// </summary>
        /// <returns></returns>
        private HashSet<int> GetElementIDsForVoidandProjectionOperations()
        {
            HashSet<int> required = new HashSet<int>();
            IEnumerable<IGrouping<IfcElement, IfcRelVoidsElement>> productShapeAndOpeningsIds = _model.Instances.OfType<IfcRelVoidsElement>()
               .Where(r => r.RelatingBuildingElement.Representation != null && r.RelatedOpeningElement.Representation != null)
                            .GroupBy(x => x.RelatingBuildingElement);      

            foreach (IGrouping<IfcElement, IfcRelVoidsElement> pair in productShapeAndOpeningsIds)
            {
                required.Add(pair.Key.EntityLabel);
                foreach (var relVoid in pair)
                {
                    IfcFeatureElementSubtraction opening = relVoid.RelatedOpeningElement;
                    required.Add(opening.EntityLabel);
                }
            }

            IEnumerable<IGrouping<IfcElement, IfcRelProjectsElement>> productShapeAndProjectionsIds = _model.Instances.OfType<IfcRelProjectsElement>()
               .Where(r => r.RelatingElement.Representation != null && r.RelatedFeatureElement.Representation != null)
                            .ToLookup(x => x.RelatingElement);
            foreach (IGrouping<IfcElement, IfcRelProjectsElement> pair in productShapeAndProjectionsIds)
            {
                required.Add(pair.Key.EntityLabel);
                foreach (var relProject in pair)
                {
                    IfcFeatureElementAddition projection = relProject.RelatedFeatureElement;
                    required.Add(projection.EntityLabel);
                }
            }
            return required;
        }


        private HashSet<int> WriteFeatureElements(IEnumerable<IGrouping<IfcElement, IfcFeatureElement>> openingsAndProjections,
            ConcurrentDictionary<int, GeometryReference> shapeLookup,
            ConcurrentDictionary<int, List<GeometryReference>> mapsWritten,
            ConcurrentDictionary<int, XbimRect3D> allMapBounds,
            ConcurrentDictionary<int, IXbimGeometryModel> curvedShapes,
            Dictionary<IfcRepresentationContext, ConcurrentQueue<XbimBBoxClusterElement>> clusters,
            BlockingCollection<Tuple<XbimShapeInstance, XbimShapeGeometry>> features,
            int total,
            ref int tally,
            ref int percentageParsed,
            ReportProgressDelegate progDelegate
            )
        {
            HashSet<int> processed = new HashSet<int>();
            int localPercentageParsed = percentageParsed;
            int localTally = tally;

           Parallel.ForEach<IGrouping<IfcElement, IfcFeatureElement>>(openingsAndProjections, new ParallelOptions(), pair =>
         //   foreach (IGrouping<IfcElement, IfcFeatureElement> pair in openingsAndProjections)
           {
               IfcElement element = pair.Key;
               Interlocked.Increment(ref localTally);
              
               IEnumerable<XbimShapeInstance> elementShapes = WriteProductShape(shapeLookup, mapsWritten, allMapBounds, clusters, element, false);
               
               if (elementShapes.Any())
               {
                   int styleLabel = 0; //take the last style and rep label of the shapes that make up the object
                   int context = 0;
                   IXbimGeometryModel elementGeom = null;
                   if (elementShapes.Count() > 1) //merge multiple body parts together
                   {
                       List<IXbimGeometryModel> allBody = new List<IXbimGeometryModel>();
                       foreach (var elemShape in elementShapes)
                       {
                           IXbimGeometryModel geom = GetGeometryModel(elemShape, curvedShapes);
                           if (geom.HasCurvedEdges)
                           {
                               if (elementGeom == null) elementGeom = geom; else elementGeom = elementGeom.Union(geom, _model.ModelFactors);
                           }
                           else
                               allBody.Add(geom);
                           context = elemShape.RepresentationContext;
                       }
                       IXbimGeometryModel m = _engine.Merge(allBody, _model.ModelFactors);
                       if (elementGeom == null)
                           elementGeom = m;
                       else
                           elementGeom = elementGeom.Union(m, _model.ModelFactors);
                   }
                   else
                   {
                     
                       elementGeom = GetGeometryModel(elementShapes.First(),curvedShapes);      
                       context = elementShapes.First().RepresentationContext;
                   }



                   List<IXbimGeometryModel> allOpenings = new List<IXbimGeometryModel>();
                   List<IXbimGeometryModel> allProjections = new List<IXbimGeometryModel>();
                  
                   foreach (var feature in pair)
                   {
                       IfcFeatureElementSubtraction opening = feature as IfcFeatureElementSubtraction;
                       if (opening != null)
                       {
                           IEnumerable<XbimShapeInstance> openingShapes = WriteProductShape(shapeLookup, mapsWritten, allMapBounds, clusters, opening, false);
                          
                           if (openingShapes.Any())
                           {
                               foreach (var openingShape in openingShapes)
                               {
                                   IXbimGeometryModel openingGeom = GetGeometryModel(openingShape,curvedShapes);     
                                   allOpenings.Add(openingGeom);
                               }
                           }
                           else
                               Logger.WarnFormat("{0} - #{1} is an opening that has been no 3D geometric form definition", opening.GetType().Name, Math.Abs(opening.EntityLabel));
                           processed.Add(opening.EntityLabel);
                       }
                       else
                       {
                           IfcFeatureElementAddition addition = feature as IfcFeatureElementAddition;
                           if (addition != null)
                           {
                               IEnumerable<XbimShapeInstance> projectionShapes = WriteProductShape(shapeLookup, mapsWritten, allMapBounds, clusters, opening, false);
                               
                               if (projectionShapes.Any())
                               {
                                   foreach (var projectionShape in projectionShapes)
                                   {

                                       IXbimGeometryModel projGeom = GetGeometryModel(projectionShape, curvedShapes);
                                       allProjections.Add(projGeom);
                                   }
                               }
                               else
                                   Logger.WarnFormat("{0} - #{1} is a projection that has been no 3D geometric form definition", opening.GetType().Name,opening.EntityLabel);
                               processed.Add(opening.EntityLabel);
                           }
                       }
                   }


                   //make the finished shape
                   if (allProjections.Any())
                   {
                       List<IXbimGeometryModel> toMerge = new List<IXbimGeometryModel>(allProjections.Count);
                       //do anything that is curvedf first
                       foreach (var p in allProjections)
                       {
                           if (p.HasCurvedEdges) elementGeom = elementGeom.Union(p, _model.ModelFactors);
                           else toMerge.Add(p);

                       }
                       if (toMerge.Any())
                       {
                           IXbimGeometryModel m = _engine.Merge(toMerge, _model.ModelFactors);
                           elementGeom = elementGeom.Union(m, _model.ModelFactors);
                       }
                   }
                   if (allOpenings.Any())
                   {
                       List<IXbimGeometryModel> toMerge = new List<IXbimGeometryModel>(allOpenings.Count);
                       //do anything that is curvedf first
                       foreach (var o in allOpenings)
                       {
                           if (o.HasCurvedEdges) elementGeom = elementGeom.Cut(o, _model.ModelFactors);
                           else toMerge.Add(o);

                       }
                       if (toMerge.Any())
                       {
                           IXbimGeometryModel m = _engine.Merge(toMerge, _model.ModelFactors);
                           elementGeom = elementGeom.Cut(m, _model.ModelFactors);
                       }
                       
                   }

                   ////now add to the DB
             
                   string shapeData = elementGeom.WriteAsString(_model.ModelFactors);
                   XbimShapeGeometry shapeGeometry = new XbimShapeGeometry()
                   {
                       IfcShapeLabel = element.EntityLabel,
                       GeometryHash = 0,
                       LOD = XbimLOD.LOD_Unspecified,
                       Format = XbimGeometryType.Polyhedron,
                       ShapeData = shapeData,
                       BoundingBox = elementGeom.GetBoundingBox()
                   };
                   
                   XbimShapeInstance shapeInstance = new XbimShapeInstance()
                   {
                       IfcProductLabel = element.EntityLabel,
                       ShapeGeometryLabel = 0,
                       StyleLabel = styleLabel,
                       RepresentationType = XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded,
                       RepresentationContext = context,
                       IfcTypeId = IfcMetaData.IfcTypeId(element),
                       Transformation =  XbimMatrix3D.Identity,
                       BoundingBox = elementGeom.GetBoundingBox()
                   };
                   features.Add(new Tuple<XbimShapeInstance, XbimShapeGeometry>(shapeInstance, shapeGeometry));

               }
               else
                   Logger.WarnFormat("{0} - #{1} is an element that contains openings but it has no 3D geometric form definition", element.GetType().Name,element.EntityLabel);
               processed.Add(element.EntityLabel);
               if (progDelegate != null)
               {
                   int newPercentage = Convert.ToInt32((double)localTally / total * 100.0);
                   if (newPercentage > localPercentageParsed)
                   {
                       Interlocked.Exchange(ref  localPercentageParsed , newPercentage);
                       progDelegate(localPercentageParsed, "Building Elements");
                   }
               }
           }
         );
            percentageParsed = localPercentageParsed;
            tally = localTally;
            return processed;
        }

        private List<IGrouping<IfcElement, IfcFeatureElement>> GetOpeningsAndProjections()
        {
            var openings = _model.Instances.OfType<IfcRelVoidsElement>()
                .Where(r => r.RelatingBuildingElement.Representation != null && r.RelatedOpeningElement.Representation != null)
                .Select(f => new { element = f.RelatingBuildingElement, feature = (IfcFeatureElement)f.RelatedOpeningElement });

            var projections = _model.Instances.OfType<IfcRelProjectsElement>()
                .Where(r => r.RelatingElement.Representation != null && r.RelatedFeatureElement.Representation != null)
                .Select(f => new { element = f.RelatingElement, feature = (IfcFeatureElement)f.RelatedFeatureElement });

            var allOps = openings.Concat(projections);

            List<IGrouping<IfcElement, IfcFeatureElement>> openingsAndProjections = allOps
                            .GroupBy(x => x.element, y => y.feature).ToList();
            return openingsAndProjections;
        }

        private IXbimGeometryModel GetGeometryModel(XbimShapeInstance xbimShapeInstance, ConcurrentDictionary<int, IXbimGeometryModel> curvedShapes)
        {
            IXbimGeometryModel geomModel;
            XbimShapeGeometry shapeGeom = this.ShapeGeometry(xbimShapeInstance.ShapeGeometryLabel);
            if (curvedShapes.TryGetValue(shapeGeom.IfcShapeLabel, out geomModel))
            {
                geomModel = geomModel.TransformBy(xbimShapeInstance.Transformation);
                geomModel.RepresentationLabel = (int)shapeGeom.IfcShapeLabel;
                geomModel.SurfaceStyleLabel = (int)xbimShapeInstance.StyleLabel;
                return geomModel;
            }
            else
            {
                IXbimGeometryEngine engine = _model.GeometryEngine();
                if (engine != null)
                {
                    
                    if (shapeGeom.ShapeLabel < 0)
                        Logger.ErrorFormat("Shape Geometry #{0} was not found in the database ", xbimShapeInstance.ShapeGeometryLabel);
                    geomModel = engine.GetGeometry3D(shapeGeom.ShapeData, shapeGeom.Format);
                    geomModel.RepresentationLabel = (int)shapeGeom.IfcShapeLabel;
                    geomModel.SurfaceStyleLabel = (int)xbimShapeInstance.StyleLabel;
                    geomModel = geomModel.TransformBy(xbimShapeInstance.Transformation);
                    return geomModel.ToPolyhedron(_model.ModelFactors);
                }
                else return XbimEmptyGeometryGroup.Empty;
            }
        }

        public IXbimPolyhedron GetGeometryModel(XbimShapeInstance xbimShapeInstance)
        {
            IXbimGeometryEngine engine = _model.GeometryEngine();
            if (engine != null)
            {
                XbimShapeGeometry shapeGeom = this.ShapeGeometry(xbimShapeInstance.ShapeGeometryLabel);
                if (shapeGeom.ShapeLabel < 0)
                    Logger.ErrorFormat("Shape Geometry #{0} was not found in the database ", xbimShapeInstance.ShapeGeometryLabel);
                IXbimGeometryModel geomModel = engine.GetGeometry3D(shapeGeom.ShapeData, shapeGeom.Format);
                geomModel.RepresentationLabel = (int)shapeGeom.IfcShapeLabel;
                geomModel.SurfaceStyleLabel = (int)xbimShapeInstance.StyleLabel;
                geomModel = geomModel.TransformBy(xbimShapeInstance.Transformation);
                return geomModel.ToPolyhedron(_model.ModelFactors);
            }
            else return XbimEmptyGeometryGroup.Empty;
        }

        public IXbimPolyhedron GetGeometryModel(XbimShapeGeometry shapeGeom)
        {
            IXbimGeometryEngine engine = _model.GeometryEngine();
            if (engine != null)
            {
                IXbimGeometryModel geomModel = engine.GetGeometry3D(shapeGeom.ShapeData, shapeGeom.Format);
                geomModel.RepresentationLabel = (int)shapeGeom.IfcShapeLabel;
                return geomModel.ToPolyhedron(_model.ModelFactors);
            }
            else return XbimEmptyGeometryGroup.Empty;
        }

        /// <summary>
        /// populates the two hash sets with the identities of the representation items used in the model
        /// </summary>
        /// <param name="productShapeIds"></param>
        /// <param name="mappedShapeIds">Those representation items that are maps to others</param>
        private HashSet<int> GetProductShapeIds(out HashSet<int> mappedShapeIds)
        {

            mappedShapeIds = new HashSet<int>();
            HashSet<int> productShapeIds = new HashSet<int>();
            foreach (var product in _model.Instances.OfType<IfcProduct>().Where(p => p.Representation != null))
            {

                //select representations that are in the required context
                //only want solid representations for this context, but rep type is optional so just filter identified 2d elements
                //we can only handle one representation in a context and this is in an implementers agreement
                IfcRepresentation rep = product.Representation.Representations.Where(r =>
                    _contexts.Contains(r.ContextOfItems) &&
                    r.IsBodyRepresentation())
                    .FirstOrDefault();
                //write out the representation if it has one
                if (rep != null)
                {
                    foreach (var shape in rep.Items.Where(i=>!(i is IfcGeometricSet)))
                    {
                        if (shape is IfcMappedItem)
                        {
                            IfcMappedItem map = (IfcMappedItem)shape;
                            mappedShapeIds.Add(map.EntityLabel);
                            //make sure any shapes mapped are in the set to process as well
                            foreach (var item in map.MappingSource.MappedRepresentation.Items)
                            {
                                if (!(item is IfcGeometricSet))
                                {
                                    int mappedItemLabel = item.EntityLabel;
                                    //if not already processed add it
                                    productShapeIds.Add(mappedItemLabel);
                                    
                                }
                            }
                        }
                        else
                        { 
                            //if not already processed add it
                            productShapeIds.Add(shape.EntityLabel);
                        }
                    }

                }
            }
            
            return productShapeIds;
        }


        private void WriteProductShapes(bool includeOpeningsAndProjections, ReportProgressDelegate progDelegate, ParallelOptions pOpts, List<IfcProduct> products, int total, ref int tally, ref int percentageParsed, ConcurrentDictionary<int, GeometryReference> shapeLookup, ConcurrentDictionary<int, List<GeometryReference>> mapsWritten, ConcurrentDictionary<int, XbimRect3D> allMapBounds,
             Dictionary<IfcRepresentationContext, ConcurrentQueue<XbimBBoxClusterElement>> clusters)
        {


            int localTally = tally;
            int localPercentageParsed = percentageParsed;

            Parallel.ForEach<IfcProduct>(products, pOpts, product =>
            //    foreach (var product in products)
            {

                //select representations that are in the required context
                //only want solid representations for this context, but rep type is optional so just filter identified 2d elements
                //we can only handle one representation in a context and this is in an implementers agreement
                IfcRepresentation rep = product.Representation.Representations.Where(r =>
                    _contexts.Contains(r.ContextOfItems) &&
                    r.IsBodyRepresentation())
                    .FirstOrDefault();
                //write out the representation if it has one
                if (rep != null)
                {
                    WriteProductShape(shapeLookup, mapsWritten, allMapBounds, clusters, product, true);
                }

            }
          );
            tally = localTally;
            percentageParsed = localPercentageParsed;
        }
        /// <summary>
        /// Process the products shape and writes the instances of shape geometries to the Database
        /// </summary>
        /// <param name="shapeLookup"></param>
        /// <param name="mapsWritten"></param>
        /// <param name="allMapBounds"></param>
        /// <param name="clusters"></param>
        /// <param name="product"></param>
        /// <param name="rep"></param>
        /// <returns>IEnumerable of XbimShapeInstance that have been written</returns>
        private IEnumerable<XbimShapeInstance> WriteProductShape(ConcurrentDictionary<int, GeometryReference> shapeLookup, ConcurrentDictionary<int, List<GeometryReference>> mapsWritten, ConcurrentDictionary<int, XbimRect3D> allMapBounds, IDictionary<IfcRepresentationContext, ConcurrentQueue<XbimBBoxClusterElement>> clusters, IfcProduct element, bool includesOpenings)
        {
            
            IfcRepresentation rep = element.Representation.Representations.Where(r =>
                        _contexts.Contains(r.ContextOfItems) &&
                        r.IsBodyRepresentation())
                        .FirstOrDefault();
            if (rep != null)
            {
                XbimMatrix3D placementTransform = element.ObjectPlacement.ToMatrix3D();
                List<GeometryReference> geomLabels = new List<GeometryReference>(rep.Items.Count);    //prepare a list for actual keys  
                //write out any shapes it has                   
                foreach (var shape in rep.Items)
                {
                    if (shape is IfcMappedItem)
                    {
                        List<GeometryReference> mapGeomIds;
                        int mapId = shape.EntityLabel;
                        if (mapsWritten.TryGetValue(mapId, out mapGeomIds))//if we have something to write                           
                        {
                            geomLabels.AddRange(mapGeomIds);
                        }
                        
                    }
                    else  //it is a direct reference to geometry shape
                    {

                        GeometryReference counter;
                        if (shapeLookup.TryGetValue(shape.EntityLabel, out counter))
                        {
                            geomLabels.Add(counter);
                        }
                        else
                            Logger.ErrorFormat("Failed to find shape #{0}", shape.EntityLabel);
                    }

                }
                if (geomLabels.Any())
                {
                    List<XbimShapeInstance> shapesInstances = new List<XbimShapeInstance>(geomLabels.Count);
                    
                    int contextId = rep.ContextOfItems.EntityLabel;
                    foreach (var instance in geomLabels)
                    {
                        shapesInstances.Add(
                            WriteShapeInstanceToDB(_model, instance.GeometryId, instance.StyleLabel, contextId, element,
                                       placementTransform, instance.BoundingBox/*productBounds*/,
                                       includesOpenings ? XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded : XbimGeometryRepresentationType.OpeningsAndAdditionsExcluded)
                            );
                        XbimRect3D transproductBounds = instance.BoundingBox/*productBounds*/.Transform(placementTransform); //transform the bounds
                        clusters[rep.ContextOfItems].Enqueue(new XbimBBoxClusterElement(instance.GeometryId, transproductBounds));
                    }
                    return shapesInstances;
                }
            }
            return Enumerable.Empty<XbimShapeInstance>();
        }



        private void WriteMappedItems(ParallelOptions pOpts, ConcurrentDictionary<int, GeometryReference> shapeLookup, IEnumerable<int> allMaps, ConcurrentDictionary<int, List<GeometryReference>> mapsWritten, ConcurrentDictionary<int, XbimRect3D> allMapBounds, Dictionary<int, int> surfaceStyles)
        {
            Parallel.ForEach<int>(allMaps, pOpts, mapId =>
            //   foreach (var map in allMaps)
            {
                IPersistIfcEntity entity = _model.Instances[mapId];
                IfcMappedItem map = entity as IfcMappedItem;
                List<GeometryReference> mapShapes = new List<GeometryReference>();
                XbimRect3D mapBounds = XbimRect3D.Empty;
                if (map != null)
                {
                    foreach (var mapShape in map.MappingSource.MappedRepresentation.Items)
                    {
                        //Check if we have already written this shape geometry, we should have so throw an exception if not
                        GeometryReference counter;

                        int mapShapeLabel = mapShape.EntityLabel;

                        if (shapeLookup.TryGetValue(mapShapeLabel, out counter))
                        {
                            int style = 0;
                            if (surfaceStyles.TryGetValue(mapShapeLabel, out style))
                                counter.StyleLabel = style;
                            mapShapes.Add(counter);
                            if (mapBounds.IsEmpty) mapBounds = counter.BoundingBox;
                            else mapBounds.Union(counter.BoundingBox);
                        }
                        else
                            if (!(mapShape is IfcGeometricSet)) //ignore non solid geometry sets
                                Logger.ErrorFormat("Failed to find shape #{0}", mapShape.EntityLabel);

                    }
                    if (mapShapes.Any()) //if we have something to write
                    {
                        XbimMatrix3D cartesianTransform = map.MappingTarget.ToMatrix3D();
                        XbimMatrix3D localTransform = map.MappingSource.MappingOrigin.ToMatrix3D();
                        XbimMatrix3D mapTransform = XbimMatrix3D.Multiply(cartesianTransform, localTransform);
                        mapBounds = XbimRect3D.TransformBy(mapBounds, mapTransform);
                        mapsWritten.TryAdd(map.EntityLabel, mapShapes);
                        allMapBounds.TryAdd(map.EntityLabel, mapBounds);
                    }
                }
                else
                    Logger.ErrorFormat("Illegal entity found in maps collection #{0}, type {1}", entity.EntityLabel, entity.GetType().Name);
               
            }
            );
        }
       
        private int WriteShapeGeometries(
            IEnumerable<int> listShapes,
            ConcurrentDictionary<int, GeometryReference> shapeLookup,
            Dictionary<int, int> surfaceStyles,
            int total,
            ref int tally,
            ref int percentageParsed,
            ReportProgressDelegate progDelegate,
            ParallelOptions pOpts,
            BlockingCollection<XbimShapeGeometry> shapeGeometries,
            ConcurrentDictionary<int, IXbimGeometryModel> curvedShapes)
        {
            int localPercentageParsed = percentageParsed;
            int localTally = tally;
            int dedupCount = 0;


            ConcurrentDictionary<RepresentationItemGeometricHashKey, int> geomHash =
                new ConcurrentDictionary<RepresentationItemGeometricHashKey, int>();

            ConcurrentDictionary<int, int> mapLookup =
                new ConcurrentDictionary<int, int>();

            Parallel.ForEach<int>(listShapes, pOpts, shapeId =>
            //        foreach (var shapeId in listShapes)
            {   
                Interlocked.Increment(ref localTally);
                IfcRepresentationItem shape = (IfcRepresentationItem)Model.Instances[shapeId];
                RepresentationItemGeometricHashKey key = new RepresentationItemGeometricHashKey(shape);
                int mappedEntityLabel = geomHash.GetOrAdd(key, shapeId); 
            
                if (mappedEntityLabel != shapeId)//it already exists
                {
                    mapLookup.TryAdd(shapeId, mappedEntityLabel);
                    Interlocked.Increment(ref dedupCount);
                   
                }
                else //we have added a new shape geometry
                {
                    try
                    {
                       
                        IXbimGeometryModel geomModel = shape.Geometry3D();
                        if (geomModel != null)
                        {
                            if (geomModel.HasCurvedEdges) curvedShapes.TryAdd(shapeId, geomModel);
                            IXbimPolyhedron poly = geomModel.ToPolyhedron(_model.ModelFactors);
                            XbimRect3D bb = poly.GetBoundingBox();
                            string shapeData = poly.WriteAsString(_model.ModelFactors);                           
                            XbimShapeGeometry shapeGeometry = new XbimShapeGeometry()
                            {
                                IfcShapeLabel = shapeId,
                                GeometryHash = key.GetHashCode(),
                                LOD = XbimLOD.LOD_Unspecified,
                                Format = XbimGeometryType.Polyhedron,
                                ShapeData = shapeData,
                                BoundingBox = bb
                            };
                            shapeGeometries.Add(shapeGeometry);

                        }
                        else
                            throw new Exception("Geometry was not created");
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorFormat("Failed to add shape geometry for entity #{0}, reason {1}", shapeId, e.Message);
                    }

                }
                if (progDelegate != null)
                {
                    int newPercentage = Convert.ToInt32((double)localTally / total * 100.0);
                    if (newPercentage > localPercentageParsed)
                    {
                        Interlocked.Exchange(ref  localPercentageParsed, newPercentage);
                        progDelegate(localPercentageParsed, "Creating Geometry");
                    }
                }
            }

         );

            percentageParsed = localPercentageParsed;
            tally = localTally;

            //Now tidy up the maps
            Parallel.ForEach<KeyValuePair<int, int>>(mapLookup, pOpts, mapKV =>
           // foreach (var mapKV in mapLookup)
           {
               int surfaceStyle = 0;
               if (!surfaceStyles.TryGetValue(mapKV.Key, out surfaceStyle)) //it doesn't have a surface style assigned to itself, so use the base one of the main shape
                   surfaceStyles.TryGetValue(mapKV.Value, out surfaceStyle);
               GeometryReference geometryReference;
               shapeLookup.TryGetValue(mapKV.Value, out geometryReference);
               geometryReference.StyleLabel = surfaceStyle;
               shapeLookup.TryAdd(mapKV.Key, geometryReference);
           }
           );
            return dedupCount;
        }

        private Dictionary<int, int> GetSurfaceStyles()
        {
            //get all the surface styles

            var styledItemsGroup = _model.Instances
                        .OfType<IfcStyledItem>()
                        .Where(s => s.Item != null)
                        .GroupBy(s => s.Item.EntityLabel);
            Dictionary<int, int> surfaceStyles = new Dictionary<int, int>();
            foreach (var styledItemGrouping in styledItemsGroup)
            {

                var val = styledItemGrouping.SelectMany(s => s.Styles.Where(st=>st!=null).SelectMany(st =>  st.Styles.OfType<IfcSurfaceStyle>()));
                if (val.Any())
                {
                    surfaceStyles.Add(styledItemGrouping.Key, val.First().EntityLabel);
                }
            }
            return surfaceStyles;
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


        private int WriteRegionsToDB(XbimModel model, IfcRepresentationContext context, IEnumerable<XbimBBoxClusterElement> elementsToCluster)
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
                geomTable.AddGeometry(context.EntityLabel, XbimGeometryType.Region, IfcMetaData.IfcTypeId(context.GetType()), XbimMatrix3D.Identity.ToArray(), regions.ToArray());
                transaction.Commit();
            }
            finally
            {
                model.FreeTable(geomTable);
            }
            return cost;

        }
        private void WriteShapeGeometryReferenceCountToDB()
        {
            //Get the meta data about the instances
            var counts = ShapeInstances().GroupBy(
                       i => i.ShapeGeometryLabel,
                       (label, instances) => new
                       {
                           Label = label,
                           Count = instances.Count()
                       });
            XbimShapeGeometryCursor geomTable = _model.GetShapeGeometryTable();
            try
            {
                XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
                foreach (var item in counts)
                {
                    geomTable.UpdateReferenceCount(item.Label, item.Count);
                }
                transaction.Commit();
            }
            catch (Exception)
            {
                Logger.ErrorFormat("Failed to update reference count on geometry");
            }
            finally
            {
                _model.FreeTable(geomTable);
            }

        }



        private byte[] CompressData(string shapeData)
        {
            var bytes = Encoding.UTF8.GetBytes(shapeData);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }

                return mso.ToArray();
            }
        }

        /// <summary>
        /// Writes the geometry as a string into the database
        /// </summary>
        /// <param name="model"></param>
        /// <param name="geom"></param>
        /// <param name="refCount">Number of other references to this geometry</param>
        /// <returns></returns>
        XbimShapeInstance WriteShapeInstanceToDB(XbimModel model, int shapeLabel, int styleLabel, int ctxtId, IfcProduct product, XbimMatrix3D placementTransform, XbimRect3D bounds, XbimGeometryRepresentationType repType)
        {

            XbimShapeInstance shapeInstance = new XbimShapeInstance()
             {
                 IfcProductLabel = product.EntityLabel,
                 ShapeGeometryLabel = shapeLabel,
                 StyleLabel = styleLabel,
                 RepresentationType = repType,
                 RepresentationContext = ctxtId,
                 IfcTypeId = IfcMetaData.IfcTypeId(product),
                 Transformation = placementTransform,
                 BoundingBox=bounds
             };
            XbimShapeInstanceCursor geomTable = model.GetShapeInstanceTable();
            try
            {

                XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
                geomTable.AddInstance((IXbimShapeInstanceData)shapeInstance);
                transaction.Commit();
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Failed to add product geometry for entity #{0}, reason {1}", product.EntityLabel, e.Message);
            }
            finally
            {
                model.FreeTable(geomTable);
            }
            return shapeInstance;
        }



        /// <summary>
        /// Returns an enumerable of all XbimShape Instances in the model in this context, retrieveAllData will ensure that bounding box and transformation data are also retrieved
        /// </summary>
        /// <returns></returns>
        public IEnumerable<XbimShapeInstance> ShapeInstances()
        {

            XbimShapeInstanceCursor shapeInstanceTable = _model.GetShapeInstanceTable();
            try
            {
                using (var transaction = shapeInstanceTable.BeginReadOnlyTransaction())
                {
                    foreach (var context in _contexts)
                    {
                        IXbimShapeInstanceData shapeInstance = new XbimShapeInstance();
                        if (shapeInstanceTable.TrySeekShapeInstance(context.EntityLabel, ref shapeInstance))
                        {
                            do
                            {
                                yield return (XbimShapeInstance)shapeInstance;
                            }
                            while (shapeInstanceTable.TryMoveNextShapeInstance(ref shapeInstance));
                        }
                    }
                }
            }
            finally
            {
                _model.FreeTable(shapeInstanceTable);
            }
        }

        /// <summary>
        /// Retunrs shape instances grouped by their style there is very little overhead to this function when compared to calling ShapeInstances
        /// </summary>
        /// <param name="incorporateFeatures"> if true openings and projections are applied to shapes and not returned separately, if false openings and shapes that contain openings, projections and shapes that would incorporate these are returned as separate entities without incorporation </param>
        /// <returns></returns>
        public IEnumerable<IGrouping<int, XbimShapeInstance>> ShapeInstancesGroupByStyle(bool incorporateFeatures = true)
        {
            //note the storage of the instances in the DB is ordered by context and then style and type Id so the natural order will group correctly
            long currentStyle = 0;
            XbimShapeInstanceStyleGrouping grp = null;
            int openingLabel = IfcMetaData.IfcTypeId(typeof(IfcOpeningElement));
            int projectionLabel = IfcMetaData.IfcTypeId(typeof(IfcProjectionElement));

            List<XbimShapeInstance> groupedInstances = new List<XbimShapeInstance>(); ;
            foreach (var instance in ShapeInstances())
            {
                long nextStyle = instance.StyleLabel;
                //use a negative type Id if there is no style defined for this object and render on type
                if (nextStyle == 0) nextStyle = -(instance.IfcTypeId);
                if (currentStyle != nextStyle) //we have the beginnings of a group
                {
                    if (grp != null) //it is not the first time
                    {
                        yield return grp; //return a populated group
                    }
                    groupedInstances = new List<XbimShapeInstance>();
                    grp = new XbimShapeInstanceStyleGrouping((int)nextStyle, groupedInstances);
                    currentStyle = nextStyle;
                }

                if (incorporateFeatures && instance.IfcTypeId != openingLabel &&
                    instance.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded &&
                    instance.IfcTypeId != projectionLabel)
                    groupedInstances.Add(instance);
                else if (!incorporateFeatures && instance.RepresentationType != XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded)
                    groupedInstances.Add(instance);

            }
            //finally return the current group if not null
            if (grp != null) //it is not the first time
            {
                yield return grp; //return a populated group
            }

        }

        public IEnumerable<XbimShapeGeometry> ShapeGeometries()
        {
            XbimShapeGeometryCursor shapeGeometryTable = Model.GetShapeGeometryTable();
            try
            {
                using (var transaction = shapeGeometryTable.BeginReadOnlyTransaction())
                {
                    IXbimShapeGeometryData shapeGeometry = new XbimShapeGeometry();
                    if (shapeGeometryTable.TryMoveFirstShapeGeometry(ref shapeGeometry))
                    {
                        do
                        {
                            yield return (XbimShapeGeometry)shapeGeometry;
                        }
                        while (shapeGeometryTable.TryMoveNextShapeGeometry(ref shapeGeometry));
                    }
                }
            }
            finally
            {
                Model.FreeTable(shapeGeometryTable);
            }
        }


        public XbimShapeGeometry ShapeGeometry(int shapeGeometryLabel)
        {
            XbimShapeGeometryCursor shapeGeometryTable = Model.GetShapeGeometryTable();
            try
            {
                using (var transaction = shapeGeometryTable.BeginReadOnlyTransaction())
                {
                    IXbimShapeGeometryData shapeGeometry = new XbimShapeGeometry();
                    shapeGeometryTable.TryGetShapeGeometry(shapeGeometryLabel, ref shapeGeometry);
                    return (XbimShapeGeometry)shapeGeometry;
                }
            }
            finally
            {
                Model.FreeTable(shapeGeometryTable);
            }
        }

        public XbimShapeGeometry ShapeGeometry(XbimShapeInstance shapeInstance)
        {
            return ShapeGeometry(shapeInstance.ShapeGeometryLabel);
        }

        /// <summary>
        /// Returns an enumerable of all the unique surface styles used in this context, optionally pass in a colour map to obtain styles defaulted by type where they have no definition in the model
        /// </summary>
        /// <returns></returns>
        public IEnumerable<XbimTexture> SurfaceStyles(XbimColourMap colourMap = null)
        {
            if (colourMap == null) colourMap = new XbimColourMap(StandardColourMaps.IfcProductTypeMap);
            HashSet<short> productTypes = new HashSet<short>();
            XbimShapeInstanceCursor shapeInstanceTable = _model.GetShapeInstanceTable();
            try
            {
                using (var transaction = shapeInstanceTable.BeginReadOnlyTransaction())
                {
                    foreach (var context in _contexts)
                    {
                        int surfaceStyle;
                        short productType;
                        if (shapeInstanceTable.TryMoveFirstSurfaceStyle(context.EntityLabel, out surfaceStyle, out productType))
                        {
                            do
                            {
                                if (surfaceStyle > 0) //we have a surface style
                                {
                                    IfcSurfaceStyle ss = (IfcSurfaceStyle)_model.Instances[surfaceStyle];
                                    yield return new XbimTexture().CreateTexture(ss);
                                    surfaceStyle = shapeInstanceTable.SkipSurfaceStyes(surfaceStyle);
                                }
                                else  //then we use the product type for the surface style
                                {
                                    //read all shape instance of style 0 and get their product texture
                                    do
                                    {
                                        if (productTypes.Add(productType)) //if we have not seen this yet then add it
                                        {
                                            Type theType = IfcMetaData.GetType(productType);
                                            XbimTexture texture = new XbimTexture().CreateTexture(colourMap[theType.Name]); //get the colour to use
                                            texture.DefinedObjectId = productType*-1;
                                            yield return texture;
                                        }

                                    } while (shapeInstanceTable.TryMoveNextSurfaceStyle(out surfaceStyle, out productType) && surfaceStyle == 0); //skip over all the zero entriesand get theeir style

                                }
                            }
                            while (surfaceStyle != -1);
                            //now get all the undefined styles and use their product type to create the texture

                        }
                    }
                }
            }
            finally
            {
                _model.FreeTable(shapeInstanceTable);
            }
        }


        public XbimModel Model
        {
            get
            {
                return _model;
            }
        }
        public IEnumerable<XbimRegion> GetRegions()
        {
            if (_contexts == null) return null; //nothing to do
            List<XbimGeometryData> regionDataColl = new List<XbimGeometryData>();
            foreach (var context in _contexts)
            {
                regionDataColl.AddRange(_model.GetGeometryData(context.EntityLabel, XbimGeometryType.Region));
            }

            if (regionDataColl.Any())
            {
                XbimRegionCollection regions = new XbimRegionCollection();
                foreach (var regionData in regionDataColl)
                {
                    regions.AddRange(XbimRegionCollection.FromArray(regionData.ShapeData));
                }
                return regions;
            }
            return Enumerable.Empty<XbimRegion>();
        }

        /// <summary>
        /// Get the region with the greates population
        /// </summary>
        /// <returns></returns>
        public XbimRegion GetLargestRegion()
        {
            if (_contexts == null) return null; //nothing to do
            List<XbimGeometryData> regionDataColl = new List<XbimGeometryData>();
            foreach (var context in _contexts)
            {
                regionDataColl.AddRange(_model.GetGeometryData(context.EntityLabel, XbimGeometryType.Region));
            }

            if (regionDataColl.Any())
            {
                XbimRegionCollection regions = new XbimRegionCollection();
                foreach (var regionData in regionDataColl)
                {
                    regions.AddRange(XbimRegionCollection.FromArray(regionData.ShapeData));
                }
                return regions.MostPopulated();
            }
            return null;

        }


        /// <summary>
        /// Returns all instances of the specified shape geometry
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public IEnumerable<XbimShapeInstance> ShapeInstancesOf(XbimShapeGeometry geometry)
        {
            XbimShapeInstanceCursor shapeInstanceTable = _model.GetShapeInstanceTable();
            try
            {
                using (var transaction = shapeInstanceTable.BeginReadOnlyTransaction())
                {
                    foreach (var context in _contexts)
                    {
                        IXbimShapeInstanceData shapeInstance = new XbimShapeInstance();
                        if (shapeInstanceTable.TrySeekShapeInstanceOfGeometry(geometry.ShapeLabel, ref shapeInstance))
                        {
                            do
                            {
                                if (context.EntityLabel == shapeInstance.RepresentationContext) yield return (XbimShapeInstance)shapeInstance;
                            }
                            while (shapeInstanceTable.TryMoveNextShapeInstance(ref shapeInstance));
                        }
                    }
                }
            }
            finally
            {
                _model.FreeTable(shapeInstanceTable);
            }
        }

        /// <summary>
        /// Returns the shape instances of the specified product in this context
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public IEnumerable<XbimShapeInstance> ShapeInstancesOf(IfcProduct product)
        {
            XbimShapeInstanceCursor shapeInstanceTable = _model.GetShapeInstanceTable();
            try
            {
                using (var transaction = shapeInstanceTable.BeginReadOnlyTransaction())
                {
                    foreach (var context in _contexts)
                    {
                        IXbimShapeInstanceData shapeInstance = new XbimShapeInstance();
                        if (shapeInstanceTable.TrySeekShapeInstanceOfProduct(product.EntityLabel, ref shapeInstance))
                        {
                            do
                            {
                                if (context.EntityLabel == shapeInstance.RepresentationContext) yield return (XbimShapeInstance)shapeInstance;
                            }
                            while (shapeInstanceTable.TryMoveNextShapeInstance(ref shapeInstance));
                        }
                    }
                }
            }
            finally
            {
                _model.FreeTable(shapeInstanceTable);
            }
        }

        /// <summary>
        /// Returns the shape instances that have a surface style of the  specified texture in this context
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public IEnumerable<XbimShapeInstance> ShapeInstancesOf(XbimTexture texture)
        {
            XbimShapeInstanceCursor shapeInstanceTable = _model.GetShapeInstanceTable();
            try
            {
                using (var transaction = shapeInstanceTable.BeginReadOnlyTransaction())
                {
                    foreach (var context in _contexts)
                    {
                        IXbimShapeInstanceData shapeInstance = new XbimShapeInstance();

                        if (texture.DefinedObjectId > 0)
                        {
                            if (shapeInstanceTable.TrySeekSurfaceStyle(context.EntityLabel, texture.DefinedObjectId, ref shapeInstance))
                            {
                                do
                                {
                                    yield return (XbimShapeInstance)shapeInstance;
                                }
                                while (shapeInstanceTable.TryMoveNextShapeInstance(ref shapeInstance) && shapeInstance.StyleLabel == texture.DefinedObjectId);
                            }
                        }
                        else if (texture.DefinedObjectId < 0) //if the texture is for a type then get all instances of the type
                        {
                            short typeId = (short)Math.Abs(texture.DefinedObjectId);
                            if (shapeInstanceTable.TrySeekProductType(typeId, ref shapeInstance))
                            {
                                do
                                {
                                    if (context.EntityLabel == shapeInstance.RepresentationContext) yield return (XbimShapeInstance)shapeInstance;
                                }
                                while (shapeInstanceTable.TryMoveNextShapeInstance(ref shapeInstance) && shapeInstance.IfcTypeId == typeId);
                            }
                        }
                    }
                }
            }
            finally
            {
                _model.FreeTable(shapeInstanceTable);
            }
        }

        /// <summary>
        /// Returns whether the product has geometry
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public bool ProductHasGeometry(Int32 productId)
        {
            XbimShapeInstanceCursor shapeInstanceTable = _model.GetShapeInstanceTable();
            try
            {
                using (var transaction = shapeInstanceTable.BeginReadOnlyTransaction())
                {
                    foreach (var context in _contexts)
                    {
                        if (shapeInstanceTable.TrySeekShapeInstanceOfProduct(productId))
                        {
                            return true;
                        }
                    }
                }
            }
            finally
            {
                _model.FreeTable(shapeInstanceTable);
            }
            return false;
        }

        /// <summary>
        /// Returns whether the product has geometry
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public bool ProductHasGeometry(IfcProduct product)
        {
            return ProductHasGeometry(product.EntityLabel);
        }
       
        /// <summary>
        /// Returns a triangulated mesh geometry fopr the specified shape
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public IXbimMeshGeometry3D ShapeGeometryMeshOf(int shapeGeometryLabel)
        {
            XbimShapeGeometry sg = ShapeGeometry(shapeGeometryLabel);
            XbimMeshGeometry3D mg = new XbimMeshGeometry3D();
            mg.Read(sg.ShapeData);
            return mg;
        }

        /// <summary>
        /// Returns a triangulated mesh geometry fopr the specified shape geometry
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public IXbimMeshGeometry3D ShapeGeometryMeshOf(XbimShapeGeometry shapeGeometry)
        {
            XbimMeshGeometry3D mg = new XbimMeshGeometry3D();
            mg.Read(shapeGeometry.ShapeData);
            return mg;
        }
        /// <summary>
        /// Returns a triangulated mesh geometry fopr the specified shape instance, all transformations are applied
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public IXbimMeshGeometry3D ShapeGeometryMeshOf(XbimShapeInstance shapeInstance)
        {
            XbimShapeGeometry sg = ShapeGeometry(shapeInstance.ShapeGeometryLabel);
            XbimMeshGeometry3D mg = new XbimMeshGeometry3D();
            mg.Add(sg.ShapeData, shapeInstance.IfcTypeId, shapeInstance.IfcProductLabel,
                shapeInstance.InstanceLabel, shapeInstance.Transformation, _model.UserDefinedId);
            return mg;
        }

        /// <summary>
        /// Creates a SceneJS scene on the textwriter for the model context
        /// </summary>
        /// <param name="textWriter"></param>
        public void CreateSceneJS(TextWriter textWriter)
        {
            HashSet<int> maps = new HashSet<int>();
            using (JsonWriter writer = new JsonTextWriter(textWriter))
            {            
                XbimSceneJS sceneJS = new XbimSceneJS(this);
                sceneJS.WriteScene(writer);
            }
        }

        public string CreateSceneJS()
        {
            StringWriter sw = new StringWriter();
            CreateSceneJS(sw);
            return sw.ToString();
        }


    }
}

