using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Xbim.SceneJSWebViewer
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Windows.Media.Media3D;
    using Xbim.Ifc2x3.PresentationAppearanceResource;
    using Xbim.Ifc2x3.ProductExtension;
    using Xbim.Ifc2x3.SharedBldgElements;
    using Xbim.IO;
    using Xbim.ModelGeometry.Scene;
    using Xbim.Common.Logging;
    using Xbim.XbimExtensions;
    using Xbim.XbimExtensions.Interfaces;
    using Xbim.Ifc2x3.Kernel;
    using System.Diagnostics;
    using Xbim.Ifc2x3;
    using Xbim.Common.Geometry;

    /// <summary>
    /// An XBim implementation of an <see cref="IModelStream"/>. 
    /// </summary>
    /// <remarks>Provides access to semantic and geometric data within an IFC model
    /// using the XBIM.IFC library</remarks>
    public class XBimModelStream : IModelStream
    {
        #region private members
        private List<XbimSurfaceStyle> SurfaceStyleList;
        private List<String> TypeList = new List<string>();
        private List<GeometryHeader> ProductsList = new List<GeometryHeader>();
        private Camera DefaultCamera = new Camera();
        private XbimModel _model;

        private string _modelId;
        #endregion


        #region Static / Factory Members
        /// <summary>
        /// A Factory method to acquire a <see cref="IModelStream"/> for the requested model file.
        /// </summary>
        /// <param name="model">A path to the required XBIM file</param>
        /// <returns></returns>
        public static IModelStream GetModelStream(String model)
        {
            IModelStream stream;
            bool success = models.TryGetValue(model, out stream);
            if (success && stream != null)
            {
                return stream;
            }
            else
            {
                stream = new XBimModelStream(model);
                models.AddOrUpdate(model, (k) => stream, (k, v) => stream);
                return stream;
            }
        }

        // TODO: should this be a Dispose pattern, rather than static?
        /// <summary>
        /// Explicitly close the model, and release any resources
        /// </summary>
        /// <param name="model"></param>
        public static void CloseModel(String model)
        {
            IModelStream stream;
            bool success = models.TryRemove(model, out stream);
            if (success)
            {
                Logger.DebugFormat("Closing Model {0}", model);
                stream.Close();
                // TODO: Implement Displose pattern
            }
            stream = null;
        }

        private static readonly ILogger Logger = LoggerFactory.GetLogger();
        private static ConcurrentDictionary<String, IModelStream> models = new ConcurrentDictionary<String, IModelStream>();

        #endregion

        #region Constructors
        /// <summary>
        /// Prevents a default instance of the <see cref="XBimModelStream"/> class from being created.
        /// </summary>
        /// <param name="model">The model.</param>
        private XBimModelStream(String model)
        {
            string xbimFile = model + ".xbim";
           

            if (!File.Exists(xbimFile))
            {
                Logger.WarnFormat("Timed out waiting for XBIM files to become available for model : {0}", model);
                throw new System.TimeoutException("Model Stream Timed Out Waiting for Model Caching");
            }

            _modelId = model;
            _model = new XbimModel();
            _model.Open(xbimFile);
             Init(model);
        }

        #endregion

       


        #region SceneJSTest.IModelStream
        public void Close()
        {
            
            _model.Dispose();
            
            _model = null;
        }

        private BoundingBox GetModelBounds()
        {
            BoundingBox box = new BoundingBox();

            foreach (XbimGeometryData shape in _model.GetGeometryData(XbimGeometryType.BoundingBox))
            {

                XbimMatrix3D matrix3d = XbimMatrix3D.FromArray(shape.TransformData);
                BoundingBox bb = BoundingBox.FromArray(shape.ShapeData);
                bb.TransformBy(matrix3d);
                box.IncludeBoundingBox(bb);
            }
            return box;
        }
        public Camera GetBoundingBox()
        {
            //get the model boundaries
            BoundingBox box = GetModelBounds();
            return new Camera(box.PointMin.X, box.PointMin.Y, box.PointMin.Z, box.PointMax.X, box.PointMax.Y, box.PointMax.Z);
        }

        public MemoryStream GetPNIGeometryData(int geometryId)
        {
           
            MemoryStream ms = new MemoryStream();
            // BinaryWriter bw = new BinaryWriter(ms);
            // ushort tally = 0;
            // byte[] wm = null;
            // ms.Write()

            // bool TransformMatrixInitialised = false;
            if (geometryId == 610)
            {

            }

            XbimGeometryHandle handle = _model.GetGeometryHandle(geometryId);
            IEnumerable<XbimGeometryData> geometries = _model.GetGeometryData(handle.ProductLabel, handle.GeometryType).Where(gd => gd.StyleLabel == handle.SurfaceStyleLabel);
            
            if (geometries.Count() == 1)
            {
                XbimGeometryData geom = geometries.First();
                PositionsNormalsIndicesBinaryStreamWriter PNI_SW = new PositionsNormalsIndicesBinaryStreamWriter(geom.ShapeData);
                if (PNI_SW.Stream.Length > 0)
                {
                    ms.Write(geom.TransformData, 0, geom.TransformData.Length); // send transform 
                    ms.Flush();
                }
                // write the geometry
                PNI_SW.Stream.WriteTo(ms);

                // used for debugging
                //PositionsNormalsIndicesBinaryStreamMerger mrger = new PositionsNormalsIndicesBinaryStreamMerger();
                //mrger.Merge(PNI_SW.Stream.ToArray(), geom.TransformData);
                //MemoryStream merged = new MemoryStream();
                //mrger.WriteTo(merged);

                //byte[] rght = PNI_SW.Stream.ToArray();
                //byte[] wrong = merged.ToArray();

                //for (int i = 0; i < rght.Length; i++)
                //{
                //    if (rght[i] != wrong[i])
                //    {

                //    }
                //}
            }
            else
            {
                PositionsNormalsIndicesBinaryStreamMerger mrger = new PositionsNormalsIndicesBinaryStreamMerger();
                foreach (XbimGeometryData geom in geometries)
                {
                    PositionsNormalsIndicesBinaryStreamWriter PNI_SW = new PositionsNormalsIndicesBinaryStreamWriter(geom.ShapeData);
                    // PositionsNormalsIndicesBinaryStreamWriter.DebugStream(PNI_SW.Stream.ToArray(), false, "merge geom source " + geom.GeometryLabel);
                    mrger.Merge(PNI_SW.Stream.ToArray(), geom.TransformData);
                }

                if (mrger.iTotPosNormals > 0)
                {
                    ms.Write(mrger.TransformData, 0, mrger.TransformData.Length); // send transform matrix only once
                    ms.Flush();
                }
                mrger.WriteTo(ms);
                ms.Flush();
            }
            // PositionsNormalsIndicesBinaryStreamWriter.DebugStream(ms.ToArray(), true, "Completed " + entityId.ToString());
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        public List<GeometryHeader> GetGeometryHeaders()
        {
            return ProductsList;
        }

        public List<XbimSurfaceStyle> GetMaterials()
        {
            return SurfaceStyleList;
        }

        public List<String> GetTypes()
        {
            return TypeList;
        }

  
        private static Material GetMaterialFromSurfaceStyle(IfcSurfaceStyle surfaceStyle)
        {
            
            if (surfaceStyle == null || surfaceStyle.Styles.Count == 0) 
                return null;
            IfcSurfaceStyleRendering rgb = surfaceStyle.Styles.First as IfcSurfaceStyleRendering;
            if (rgb == null) 
                return null;

            // defines the material name from a property or from the enetity label of the surfacestyle

            string materialName = surfaceStyle.Name.HasValue ? surfaceStyle.Name.Value.ToString() : surfaceStyle.EntityLabel.ToString();
            
            return new Material(
                materialName + "Material",  //name
                rgb.SurfaceColour.Red,
                rgb.SurfaceColour.Green,
                rgb.SurfaceColour.Blue,
                1.0 - (double)rgb.Transparency.Value.Value, // alpha
                0.0  // emit
                );
        }

       

        // ok, cleaned up
        public void Init(string model)
        {
          
            // surface styles are taken starting from the geometryhandles
            //
            XbimGeometryHandleCollection handles = new XbimGeometryHandleCollection(_model.GetGeometryHandles().Exclude(IfcEntityNameEnum.IFCSPACE, IfcEntityNameEnum.IFCFEATUREELEMENT));
            SurfaceStyleList = handles.GetSurfaceStyles().ToList();
            TypeList = new List<string>();
            ProductsList = new List<GeometryHeader>();

            HashSet<int> alreadysent = new HashSet<int>();
                        
            foreach (XbimSurfaceStyle surfaceStyle in SurfaceStyleList)
            {
                 //try to get any material defined in the model
                Material SurfaceStyleMaterial = GetMaterialFromSurfaceStyle(surfaceStyle.IfcSurfaceStyle(_model));
                
                if (SurfaceStyleMaterial == null)
                {
                    SurfaceStyleMaterial = DefaultMaterials.LookupMaterial(surfaceStyle.IfcTypeId);
                    if (SurfaceStyleMaterial == null)
                    {
                        Logger.WarnFormat("Could not locate default material for entity type #{0} in model {1}", surfaceStyle.IfcType.Name, _modelId);
                        // set null material as SHOCKING PINK
                        SurfaceStyleMaterial = new Material(
                            surfaceStyle.IfcType.Name, 
                            0.98823529411764705882352941176471d, 
                            0.05882352941176470588235294117647d, 
                            0.75294117647058823529411764705882d, 
                            1.0d, 
                            0.0d);
                    }
                }
                SurfaceStyleMaterial.Name = SurfaceStyleMaterial.Name.Replace(' ', '_');
                SurfaceStyleMaterial.Name = SurfaceStyleMaterial.Name.Replace(',', '_');
                //prepare geometry header to send to client
                //
                GeometryHeader geomHeader = new GeometryHeader();
                geomHeader.Type = SurfaceStyleMaterial.Name;
                geomHeader.Material = SurfaceStyleMaterial.Name;
                if (SurfaceStyleMaterial.Alpha < 1)
                    geomHeader.LayerPriority = 1;
                foreach (var geomHandle in handles.GetGeometryHandles(surfaceStyle).Distinct(new CompareDistinctGeometryHandles()))
                {
                    if (!alreadysent.Contains(geomHandle.GeometryLabel))
                    {
                        string label = geomHandle.GeometryLabel.ToString();
                        geomHeader.Geometries.Add(label);
                        alreadysent.Add(geomHandle.GeometryLabel);
                    }
                }

                // populate lists
                ProductsList.Add(geomHeader);
                TypeList.Add(SurfaceStyleMaterial.Name);

                //store the material
                //the code below makes no sense, it is constantly overriding the surface style
                //surfaceStyle.TagRenderMaterial = SurfaceStyleMaterial; 
            }
            // DumpProducts();
        }

        private void DumpProducts()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("-- Types: {0}", TypeList.Count);
            sb.AppendLine();
            foreach (string type in TypeList)
            {
                sb.Append("\t");
                sb.AppendLine(type);
            }

            sb.AppendLine();
            sb.AppendFormat("-- Materials: {0}", SurfaceStyleList.Count);
            sb.AppendLine();
            foreach (var material in SurfaceStyleList)
            {
                sb.Append("\t");
                sb.AppendLine(material.TagRenderMaterial.ToString());
            }

            sb.AppendLine();
            sb.AppendFormat("-- Products: {0}", ProductsList.Count);
            sb.AppendLine("\t [Type] - [Material]");
            sb.AppendLine();
            long totalEntities = 0;
            foreach (var product in ProductsList)
            {
                sb.Append("\t");
                sb.Append(product.Type);
                sb.Append(" - ");
                sb.Append(product.Material);
                sb.AppendFormat(": {0} entities", product.Geometries.Count);
                sb.AppendFormat(" [{0}]", string.Join(",", product.Geometries.ToArray()));

                sb.AppendLine();
                totalEntities += product.Geometries.Count;
            }
            sb.AppendFormat("-- Total Entities: {0}", totalEntities);
            Debug.WriteLine(sb.ToString());
            Logger.DebugFormat("- Model Manifest for: {0}\n\n{1}", _modelId, sb);
        }

        public string QueryData(string id, string query)
        {
            try
            {       
                IfcProduct product = _model.Instances.GetFromGeometryLabel(Convert.ToInt32(id)) as IfcProduct;
                if (product != null)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("ID " + product.EntityLabel + "/" + id);
                    sb.Append(" Name: ");
                    sb.Append(product.ToString());
                    sb.Append(", IFC Type: " + product.GetType().Name);
                    Logger.DebugFormat("Query result: ", sb);
                    return sb.ToString();
                }
                else
                    return "You sent a query of: '" + query + "' for id: '" + id + "'";
            }
            catch (Exception)
            {
                return "You sent a query of: '" + query + "' for id: '" + id + "'";
            }
        }

        #endregion SceneJSTest.IModelStream

        /// <summary>
        /// Comapres two geometry handles to be distinct per surace style render
        /// </summary>
        private class CompareDistinctGeometryHandles : IEqualityComparer<XbimGeometryHandle>
        {

            public bool Equals(XbimGeometryHandle x, XbimGeometryHandle y)
            {
                return x.ProductLabel == y.ProductLabel && x.SurfaceStyleLabel == y.SurfaceStyleLabel;
            }

            public int GetHashCode(XbimGeometryHandle obj)
            {
                return obj.ProductLabel ^ obj.SurfaceStyleLabel;
            }
        }
    }
}
