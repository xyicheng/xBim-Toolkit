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

    /// <summary>
    /// An XBim implementation of an <see cref="IModelStream"/>. 
    /// </summary>
    /// <remarks>Provides access to semantic and geometric data within an IFC model
    /// using the XBIM.IFC library</remarks>
    public class XBimModelStream : IModelStream
    {
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
            string gcFile = model + ".xbimGC";

            if ((!File.Exists(xbimFile) || !File.Exists(gcFile)))
            {
                Logger.WarnFormat("Timed out waiting for XBIM files to become available for model : {0}", model);
                throw new System.TimeoutException("Model Stream Timed Out Waiting for Model Caching");
            }

            _modelId = model;
            _model = new XbimModel();
            _model.Open(xbimFile);
            _scene = new XbimSceneStream(_model, gcFile); // opens the pre-calculated Geometry file
            Init(model);
        }

        #endregion

        /// <summary>
        /// Gets the XBim Geometry scene, containing Graphs and Triangulated Meshes.
        /// </summary>
        public IXbimScene Scene { get { return _scene; } }


        #region SceneJSTest.IModelStream
        public void Close()
        {
            _scene.Close();
            _model.Dispose();
            _scene = null;
            _model = null;
        }

        private BoundingBox GetModelBounds()
        {
            BoundingBox box = new BoundingBox();

            foreach (var item in Scene.Graph.ProductNodes)
            {
                XbimTriangulatedModelStream tm = null;
                try
                {
                    tm = Scene.Triangulate(item.Value);
                }
                catch (Exception) { continue; }

                if (tm != null)
                {
                    if (item.Value != null)
                    {
                        if (item.Value.BoundingBox != null)
                        {
                            if (item.Value.BoundingBox.SizeX + item.Value.BoundingBox.SizeY + item.Value.BoundingBox.SizeZ != 0)
                            {
                                BoundingBox bb = new BoundingBox();
                                Point3D min = new Point3D(item.Value.BoundingBox.X, item.Value.BoundingBox.Y, item.Value.BoundingBox.Z);
                                Point3D max = new Point3D(item.Value.BoundingBox.X + item.Value.BoundingBox.SizeX,
                                    item.Value.BoundingBox.Y + item.Value.BoundingBox.SizeY,
                                    item.Value.BoundingBox.Z + item.Value.BoundingBox.SizeZ);
                                bb.IncludePoint(min);
                                bb.IncludePoint(max);
                                box.IncludeBoundingBox(bb.TransformBy(item.Value.WorldMatrix()));
                            }
                        }
                    }
                }
            }
            return box;
        }
        public Camera GetCamera()
        {
            //get the model boundaries
            BoundingBox box = GetModelBounds();
            return new Camera(box.PointMin.X, box.PointMin.Y, box.PointMin.Z, box.PointMax.X, box.PointMax.Y, box.PointMax.Z);
        }

        public GeometryData GetGeometryData(String entityId)
        {
            Int64 id = Convert.ToInt64(entityId);

            //check item exists
            if (!Scene.Graph.ProductNodes.ContainsKey(id))
                return new GeometryData();

            TransformNode tn = Scene.Graph.ProductNodes[id];
            XbimTriangulatedModelStream tm = Scene.Triangulate(tn);


            if (!tm.IsEmpty)
            {
                if (!tn.Product.GetType().IsSubclassOf(typeof(IfcFeatureElementSubtraction)) && !(tn.Product.GetType() == typeof(IfcSpace)))
                {
                    try
                    {
                        if (tn.Product.EntityLabel != 0)
                        {
                            //get transform matrix
                            Double[] dWorldMatrix = new Double[16];
                            Matrix3D worldMatrix = tn.WorldMatrix();
                            if (!worldMatrix.IsIdentity)
                            {
                                dWorldMatrix[0] = worldMatrix.M11;
                                dWorldMatrix[1] = worldMatrix.M12;
                                dWorldMatrix[2] = worldMatrix.M13;
                                dWorldMatrix[3] = worldMatrix.M14;

                                dWorldMatrix[4] = worldMatrix.M21;
                                dWorldMatrix[5] = worldMatrix.M22;
                                dWorldMatrix[6] = worldMatrix.M23;
                                dWorldMatrix[7] = worldMatrix.M24;

                                dWorldMatrix[8] = worldMatrix.M31;
                                dWorldMatrix[9] = worldMatrix.M32;
                                dWorldMatrix[10] = worldMatrix.M33;
                                dWorldMatrix[11] = worldMatrix.M34;

                                dWorldMatrix[12] = worldMatrix.OffsetX;
                                dWorldMatrix[13] = worldMatrix.OffsetY;
                                dWorldMatrix[14] = worldMatrix.OffsetZ;
                                dWorldMatrix[15] = worldMatrix.M44;
                            }
                            return new GeometryData(Convert.ToInt32(id), tm.DataStream.ToArray(), tm.HasData, tm.NumChildren, dWorldMatrix);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(
                            String.Format("Failed to get geometry data for entity #{0} {1) in model {2}.", entityId, tn.Product.ToString(), _modelId),
                            ex);
                        return null;
                    }
                }
            }
            Logger.InfoFormat("No geometry available for entity {0} - {1} in model {2}.", entityId, tn.Product.ToString(), _modelId);
            return null;
        }

        public List<GeometryLabel> GetGeometryHeaders()
        {
            return ProductsList;
        }

        public List<Material> GetMaterials()
        {
            return MaterialList;
        }

        public List<String> GetTypes()
        {
            return TypeList;
        }

        private void AddProduct(TransformNode node)
        {
            if (node.ChildCount > 0 || (node.TriangulatedModel.HasData == 1 || node.TriangulatedModel.NumChildren > 0))
            {
                //try to get any material defined in the model
                Material definedMaterial = GetDefinedMaterial(node); ;

                String materialName = String.Empty;
                Material material = null;

                if (definedMaterial != null)
                {
                    materialName = definedMaterial.Name;
                    material = definedMaterial;
                }
                else
                {
                    material = DefaultMaterials.LookupMaterial(node.Product);

                    if (material == null)
                    {
                        Logger.WarnFormat("Could not locate default material for entity #{0} in model {1}", node.ProductId, _modelId);
                        // set null material as SHOCKING PINK
                        material = new Material(DefaultMaterials.StripString(node.ProductId.GetType().ToString()), 0.98823529411764705882352941176471d, 0.05882352941176470588235294117647d, 0.75294117647058823529411764705882d, 1.0d, 0.0d);
                    }

                    materialName = material.Name;
                }

                //check if the material already exists, if not - add it
                if (!TypeList.Contains(material.Name))
                {
                    materialName = AddMaterialType(materialName, material);
                }

                //add the product to the correct header now we are sure to have the material in the list
                GeometryLabel label = ProductsList.First(s => s.Type == materialName);
                label.Geometries.Add(node.ProductId.ToString());
            }
            else
            {
                Logger.DebugFormat("No mesh available for entity #{0} - {1}", node.ProductId, node.Product.ToString());
            }
        }

        private String AddMaterialType(String materialName, Material material)
        {
            materialName = material.Name;
            TypeList.Add(materialName);
            GeometryLabel g = new GeometryLabel();
            g.Type = materialName;
            g.Material = materialName;
            if (material.Alpha < 1)
            {
                g.LayerPriority = 1;
            }
            MaterialList.Add(material);
            ProductsList.Add(g);
            return materialName;
        }

        private static Material GetDefinedMaterial(TransformNode node)
        {
            try
            {
                if (node.Product.Representation == null)
                    return null;

                var representation = node.Product.Representation.Representations.First();

                //if we dont have any representation, return
                if (representation == null || representation.Items.Count == 0) return null;
                var styledBy = representation.Items.First();
                if (styledBy == null || styledBy.StyledByItem.Count() == 0) return null;
                var styleByItem = styledBy.StyledByItem.First();
                if(styleByItem == null || styleByItem.Styles.Count == 0) return null;
                var firstStyle = styleByItem.Styles.First;
                if (firstStyle == null || firstStyle.Styles.Count == 0) return null;
                IfcSurfaceStyle surfaceStyle = firstStyle.Styles.First as IfcSurfaceStyle;
                if (surfaceStyle == null || surfaceStyle.Styles.Count == 0) return null;
                IfcSurfaceStyleRendering rgb = surfaceStyle.Styles.First as IfcSurfaceStyleRendering;
                if (rgb == null) return null;
                    
                return new Material(surfaceStyle.Name.Value + "Material",
                    rgb.SurfaceColour.Red,
                    rgb.SurfaceColour.Green,
                    rgb.SurfaceColour.Blue,
                    (1.0 - (double)rgb.Transparency.Value.Value),
                    0.0);
                
            }
            catch (Exception ex)
            {
                Logger.Warn(String.Format("Failed to get Representation and Material for entity #{0}", node.ProductId),
                    ex);
            }
            return null;
        }

        private Func<TransformNode, bool> FilterByType(Type t)
        {
            return p => (p.Product.GetType() == t || p.Product.GetType().IsSubclassOf(t));
        }

        public void Init(string model)
        {
            //prepare temp collections of the different types we want to prioritise
            IEnumerable<TransformNode> Slabs = Scene.Graph.ProductNodes.Values.Where(FilterByType(typeof(IfcSlab)));
            IEnumerable<TransformNode> Walls = Scene.Graph.ProductNodes.Values.Where(FilterByType(typeof(IfcWall)));
            IEnumerable<TransformNode> Roofs = Scene.Graph.ProductNodes.Values.Where(FilterByType(typeof(IfcRoof)));
            IEnumerable<TransformNode> Windows = Scene.Graph.ProductNodes.Values.Where(FilterByType(typeof(IfcWindow)));
            IEnumerable<TransformNode> Doors = Scene.Graph.ProductNodes.Values.Where(FilterByType(typeof(IfcDoor)));
            IEnumerable<TransformNode> Plates = Scene.Graph.ProductNodes.Values.Where(FilterByType(typeof(IfcPlate)));
            IEnumerable<TransformNode> Beams = Scene.Graph.ProductNodes.Values.Where(FilterByType(typeof(IfcBeam)));
            IEnumerable<TransformNode> Columns = Scene.Graph.ProductNodes.Values.Where(FilterByType(typeof(IfcColumn)));
            IEnumerable<TransformNode> Others = Scene.Graph.ProductNodes.Values.Except(Slabs).Except(Walls).Except(Roofs).Except(Windows).Except(Doors).Except(Plates).Except(Beams).Except(Columns);

            //Add the types we want to load first (in reverse order as client pops from top of list)
            foreach (TransformNode tn in Others)
            {
                if (!tn.Product.GetType().IsSubclassOf(typeof(IfcFeatureElementSubtraction)) && !(tn.Product.GetType() == typeof(IfcSpace)))
                {
                    AddProduct(tn);
                }
                else
                {
                    Logger.DebugFormat("Skipping non-visible entity #{0} - {1}", tn.ProductId, tn.Product.ToString());
                }
            }

            foreach (TransformNode tn in Beams)
            {
                AddProduct(tn);
            }
            foreach (TransformNode tn in Columns)
            {
                AddProduct(tn);
            }
            foreach (TransformNode tn in Plates)
            {
                AddProduct(tn);
            }
            foreach (TransformNode tn in Doors)
            {
                AddProduct(tn);
            }
            foreach (TransformNode tn in Windows)
            {
                AddProduct(tn);
            }
            foreach (TransformNode tn in Roofs)
            {
                AddProduct(tn);
            }

            foreach (TransformNode tn in Walls)
            {
                AddProduct(tn);
            }
            foreach (TransformNode tn in Slabs)
            {
                AddProduct(tn);
            }

            if (Logger.IsDebugEnabled)
            {
                DumpProducts();
            }
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
            sb.AppendFormat("-- Materials: {0}", MaterialList.Count);
            sb.AppendLine();
            foreach (var material in MaterialList)
            {
                sb.Append("\t");
                sb.AppendLine(material.Name);
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
                sb.AppendLine();
                totalEntities += product.Geometries.Count;
            }
            sb.AppendFormat("-- Total Entities: {0}", totalEntities);
            Logger.DebugFormat("- Model Manifest for: {0}\n\n{1}", _modelId, sb);
        }

        public string QueryData(string id, string query)
        {
            //foreach (var item in Scene.Graph.ProductNodes)
            //{
            TransformNode tn = null;
            if (Scene.Graph.ProductNodes.TryGetValue(Convert.ToInt64(id), out tn))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("ID " + id);
                sb.Append(" Name: ");
                try
                {
                    sb.Append(tn.Product.ToString());
                    //sb.Append(" " + tn.Product.OwnerHistory.LastModifyingUser.ThePerson.FamilyName.Value.ToPart21);
                }
                catch { }
                //sb.Append(" min: ");
                //sb.Append(tn.BoundingBox.X); sb.Append(", ");
                //sb.Append(tn.BoundingBox.Y); sb.Append(", ");
                //sb.Append(tn.BoundingBox.Z);
                //sb.Append(" max: ");
                //sb.Append(tn.BoundingBox.X + tn.BoundingBox.SizeX); sb.Append(", ");
                //sb.Append(tn.BoundingBox.Y + tn.BoundingBox.SizeY); sb.Append(", ");
                //sb.Append(tn.BoundingBox.Z + tn.BoundingBox.SizeZ);
                sb.Append(", IFC Type: " + DefaultMaterials.StripString(tn.Product.GetType().ToString()));

                Logger.DebugFormat("Query result: ", sb);
                return sb.ToString();
            }
            //}
            return "You sent a query of: '" + query + "' for id: '" + id + "'";
        }

        #endregion SceneJSTest.IModelStream

        #region private members
        private List<Material> MaterialList = new List<Material>();
        private List<String> TypeList = new List<string>();
        private List<GeometryLabel> ProductsList = new List<GeometryLabel>();
        private Camera DefaultCamera = new Camera();
        private XbimModel _model;
        private IXbimScene _scene;
        private string _modelId;
        #endregion
    }
}