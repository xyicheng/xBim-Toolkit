using glMatrix;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using Xbim.Ifc.PresentationAppearanceResource;
using Xbim.Ifc.ProductExtension;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Xplorer
{
    class ModelProcessor
    {
        //ModelScene.Graph.ProductNodes
        public static void ProcessModelAsync(XbimSceneStream Scene, OpenGLModel model)
        {
            Parallel.ForEach(Scene.Graph.ProductNodes.Values, tn => { AddProduct(Scene, tn, model); });
            //foreach (TransformNode tn in Scene.Graph.ProductNodes.Values)
            //{
            //    AddProduct(Scene, tn, model);
            //}
            return;
        }

        private static void AddProduct(IXbimScene Scene, TransformNode node, OpenGLModel model)
        {
            if (node.ChildCount > 0 || (node.TriangulatedModel.HasData == 1 || node.TriangulatedModel.NumChildren > 0))
            {
                //try to get any material defined in the model
                Material definedMaterial = GetDefinedMaterial(node); 
                Material defaultMaterial = DefaultMaterials.LookupMaterial(node.Product); 

                String materialName = String.Empty;
                Material material = null;

                if (definedMaterial != null)
                {
                    materialName = definedMaterial.Name;
                    material = definedMaterial;
                    material.Priority = definedMaterial.Priority;
                }
                else
                {
                    material = defaultMaterial;

                    if (material == null)
                    {
                        // set null material as SHOCKING PINK
                        material = new Material(DefaultMaterials.StripString(node.ProductId.GetType().ToString()), 0.98823529411764705882352941176471d, 0.05882352941176470588235294117647d, 0.75294117647058823529411764705882d, 1.0d, 0.0d);
                    }

                    materialName = material.Name;
                }

                //check if the material already exists, if not - add it
                if (!model.HasMaterial(material.Name))
                {
                    model.AddMaterial(material.Color, material.Name, material.Priority);
                }

                XbimTriangulatedModelStream tm = Scene.Triangulate(node);
                if (!tm.IsEmpty)
                {
                    if (!node.Product.GetType().IsSubclassOf(typeof(IfcFeatureElementSubtraction)) && !(node.Product.GetType() == typeof(IfcSpace)))
                    {
                        try
                        {
                            if (node.Product.EntityLabel != 0)
                            {
                                GLGeometry geo = new GLGeometry();

                                //get transform matrix
                                mat4 dWorldMatrix = mat4.create();

                                //TODO: this bit isn't Mono compatible
                                Matrix3D worldMatrix = node.WorldMatrix();
                                if (!worldMatrix.IsIdentity)
                                {
                                    dWorldMatrix[0] = (Single)worldMatrix.M11;
                                    dWorldMatrix[1] = (Single)worldMatrix.M12;
                                    dWorldMatrix[2] = (Single)worldMatrix.M13;
                                    dWorldMatrix[3] = (Single)worldMatrix.M14;

                                    dWorldMatrix[4] = (Single)worldMatrix.M21;
                                    dWorldMatrix[5] = (Single)worldMatrix.M22;
                                    dWorldMatrix[6] = (Single)worldMatrix.M23;
                                    dWorldMatrix[7] = (Single)worldMatrix.M24;

                                    dWorldMatrix[8] = (Single)worldMatrix.M31;
                                    dWorldMatrix[9] = (Single)worldMatrix.M32;
                                    dWorldMatrix[10] = (Single)worldMatrix.M33;
                                    dWorldMatrix[11] = (Single)worldMatrix.M34;

                                    dWorldMatrix[12] = (Single)worldMatrix.OffsetX;
                                    dWorldMatrix[13] = (Single)worldMatrix.OffsetY;
                                    dWorldMatrix[14] = (Single)worldMatrix.OffsetZ;
                                    dWorldMatrix[15] = (Single)worldMatrix.M44;
                                }
                                geo.EntityLabel = node.ProductId;

                                //TODO: this bit isn't Mono compatible
                                box3 bounds = box3.create();
                                //Rect3D bb = tm.BoundingBox;
                                //bounds.includePoint(vec3.create(bb.X, bb.Y, bb.Z));
                                //bounds.includePoint(vec3.create(bb.X+bb.SizeX, bb.Y+bb.SizeY, bb.Z+bb.SizeZ));
                                //bounds.TransformByMatrix(dWorldMatrix);

                                //lock (model.modelbounds)
                                //{
                                //    model.modelbounds.includeBox(bounds);
                                //}

                                //return new GeometryData(Convert.ToInt32(id), tm.DataStream.ToArray(), tm.HasData, tm.NumChildren, dWorldMatrix);
                                if (!tm.IsEmpty)
                                {
                                    viewer_mesh_converter mesh = new viewer_mesh_converter();
                                    View data = new View(tm.DataStream.ToArray());
                                    try
                                    {
                                        if (tm.HasData != 0x00)
                                        {
                                            mesh.AddOneMesh(data);
                                        }
                                        for (var i = 0; i < tm.NumChildren; i++)
                                        {
                                            mesh.AddOneMesh(data);
                                        }
                                    }
                                    catch (Exception) { }

                                    try
                                    {
                                        List<vec3> positions = new List<vec3>();
                                        List<vec3> normals = new List<vec3>();

                                        foreach (Int32[] lint in mesh.uniquePoints)
                                        {
                                            Int32 pI = lint[0];
                                            Int32 nI = lint[1];
                                            vec3 p = mesh.points[pI];
                                            vec3 n = mesh.normals[nI];

                                            positions.Add(p);
                                            normals.Add(n);
                                        }

                                        geo.Positions = new List<float>(mesh.indices.Count * 3 * 3);
                                        geo.Normals = new List<float>(mesh.indices.Count * 3 * 3);
                                        for (var k = 0; k < mesh.indices.Count; k++)
                                        {

                                            var ind = mesh.indices[k];
                                            vec3 pos = vec3.create();
                                            mat4.multiplyVec3(dWorldMatrix, positions[ind], pos);
                                            bounds.includePoint(pos);
                                            geo.Positions.AddRange((Single[])pos);
                                            geo.Normals.AddRange((Single[])normals[ind]);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show(ex.ToString());
                                    }
                                }
                                if (geo != null)
                                {
                                    lock (model.modelbounds)
                                    {
                                        model.modelbounds.includeBox(bounds);
                                    }
                                    model.AddGeometry(geo, material.Name);
                                }
                            }
                            else
                            {
                            }
                        }
                        catch (Exception ex) { MessageBox.Show(ex.ToString()); }
                    }
                }
            }
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
                if (styleByItem == null || styleByItem.Styles.Count == 0) return null;
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
            }
            return null;
        }
    }
}
