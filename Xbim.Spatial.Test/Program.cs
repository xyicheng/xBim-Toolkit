using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using System.Windows.Media.Media3D;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;
using System.Windows.Media;

namespace Xbim.Spatial.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() == 0)
            {
                Console.WriteLine("No file specified");
            }

            //create model
            XbimModel model = new XbimModel();
            model.CreateFrom(args[0]);

            //Cache
            Dictionary<int, ModelVisual3D> mvCache = new Dictionary<int, ModelVisual3D>();
            Dictionary<int, MeshGeometry3D> meshCache = new Dictionary<int, MeshGeometry3D>();

            XbimGeometryHandleCollection handles = new XbimGeometryHandleCollection(model.GetGeometryHandles());
            foreach (var ss in handles.GetSurfaceStyles())
            {
                ss.GeometryData = model.GetGeometryData(handles.GetGeometryHandles(ss)).ToList();
                foreach (var prodGeom in ss.ProductGeometries)
                {
                    //Try and get the visual for the product, if not found create one
                    ModelVisual3D mv;
                    if (!mvCache.TryGetValue(prodGeom.ProductLabel, out mv))
                        mv = new ModelVisual3D();

                    //Set up the Model Geometry to hold the product geometry, this has unique material and tranform
                    //and may reuse GeometryModel3D meshes from previous renders
                    GeometryModel3D m3d = new GeometryModel3D();

                    bool first = true;
                    foreach (var geom in prodGeom.Geometry) //create a new one don't add it to the scene yet as we may have no valid content
                    {
                        if (first)
                        {
                            Matrix3D matrix3d = new Matrix3D().FromArray(geom.TransformData);
                            m3d.Transform = new MatrixTransform3D(matrix3d);
                            first = false;
                        }
                        int geomHash = geom.GeometryHash; //check if it is already loaded
                        MeshGeometry3D mesh;
                        if (!meshCache.TryGetValue(geomHash, out mesh)) //if not loaded, load it and merge with any other meshes in play
                        {
                            if (geom.GeometryType == XbimGeometryType.TriangulatedMesh)
                            {
                                XbimTriangulatedModelStream tMod = new XbimTriangulatedModelStream(geom.ShapeData);
                                mesh = tMod.AsMeshGeometry3D();
                                meshCache.Add(geomHash, mesh);
                            }
                            else if (geom.GeometryType == XbimGeometryType.BoundingBox)
                            {
                                Rect3D r3d = new Rect3D().FromArray(geom.ShapeData);
                                mesh = MakeBoundingBox(r3d);
                                meshCache.Add(geomHash, mesh);
                            }
                            else
                                throw new Exception("Illegal geometry type found");
                            if (m3d.Geometry == null)
                                m3d.Geometry = mesh;
                            else
                                m3d.Geometry = Append(m3d.Geometry, mesh);
                        }
                        else //add a new GeometryModel3d to the visual as we want to reference an existing mesh
                        {
                            GeometryModel3D m3dRef = new GeometryModel3D();
                            m3dRef.Geometry = mesh;
                            m3dRef.Transform = m3d.Transform; //reuse the same transform
                            AddGeometry(mv, m3dRef);
                        }
                    }
                }
            }
        }

        public static void AddGeometry(ModelVisual3D visual, GeometryModel3D geometry)
        {
            if (visual.Content == null)
                visual.Content = geometry;
            else
            {
                if (visual.Content is Model3DGroup)
                    ((Model3DGroup)visual.Content).Children.Add(geometry);
                //it is not a group but now needs to be
                else
                {
                    Model3DGroup m3dGroup = new Model3DGroup();
                    m3dGroup.Children.Add(visual.Content);
                    m3dGroup.Children.Add(geometry);
                    visual.Content = m3dGroup;
                }
            }
        }

        public static Geometry3D Append(Geometry3D sourceMesh, MeshGeometry3D toAdd)
        {

            MeshGeometry3D addTo = sourceMesh as MeshGeometry3D;

            MeshGeometry3D m3d = new MeshGeometry3D();


            m3d.Positions = new Point3DCollection(addTo.Positions.Count + toAdd.Positions.Count);
            foreach (var pt in addTo.Positions) m3d.Positions.Add(pt);
            foreach (var pt in toAdd.Positions) m3d.Positions.Add(pt);


            m3d.Normals = new Vector3DCollection(addTo.Normals.Count + toAdd.Normals.Count);
            foreach (var v in addTo.Normals) m3d.Normals.Add(v);
            foreach (var v in toAdd.Normals) m3d.Normals.Add(v);

            int maxIndices = addTo.Positions.Count; //we need to increment all indices by this amount
            m3d.TriangleIndices = new Int32Collection(addTo.TriangleIndices.Count + toAdd.TriangleIndices.Count);
            foreach (var i in addTo.TriangleIndices) m3d.TriangleIndices.Add(i);
            foreach (var i in toAdd.TriangleIndices) m3d.TriangleIndices.Add(i + maxIndices);
            return m3d;
        }

        private static MeshGeometry3D MakeBoundingBox(Rect3D r3D)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            Point3D p0 = r3D.Location;
            Point3D p1 = p0;
            p1.X += r3D.SizeX;
            Point3D p2 = p1;
            p2.Z += r3D.SizeZ;
            Point3D p3 = p2;
            p3.X -= r3D.SizeX;
            Point3D p4 = p3;
            p4.Y += r3D.SizeY;
            Point3D p5 = p4;
            p5.Z -= r3D.SizeZ;
            Point3D p6 = p5;
            p6.X += r3D.SizeX;
            Point3D p7 = p6;
            p7.Z += r3D.SizeZ;

            List<Point3D> points = new List<Point3D>();
            points.Add(p0);
            points.Add(p1);
            points.Add(p2);
            points.Add(p3);
            points.Add(p4);
            points.Add(p5);
            points.Add(p6);
            points.Add(p7);

            AddVertex(3, mesh, points);
            AddVertex(0, mesh, points);
            AddVertex(2, mesh, points);

            AddVertex(0, mesh, points);
            AddVertex(1, mesh, points);
            AddVertex(2, mesh, points);

            AddVertex(4, mesh, points);
            AddVertex(5, mesh, points);
            AddVertex(3, mesh, points);

            AddVertex(5, mesh, points);
            AddVertex(0, mesh, points);
            AddVertex(3, mesh, points);

            AddVertex(7, mesh, points);
            AddVertex(6, mesh, points);
            AddVertex(4, mesh, points);

            AddVertex(6, mesh, points);
            AddVertex(5, mesh, points);
            AddVertex(4, mesh, points);

            AddVertex(2, mesh, points);
            AddVertex(1, mesh, points);
            AddVertex(7, mesh, points);

            AddVertex(1, mesh, points);
            AddVertex(6, mesh, points);
            AddVertex(7, mesh, points);

            AddVertex(4, mesh, points);
            AddVertex(3, mesh, points);
            AddVertex(7, mesh, points);

            AddVertex(3, mesh, points);
            AddVertex(2, mesh, points);
            AddVertex(7, mesh, points);

            AddVertex(6, mesh, points);
            AddVertex(1, mesh, points);
            AddVertex(5, mesh, points);

            AddVertex(1, mesh, points);
            AddVertex(0, mesh, points);
            AddVertex(5, mesh, points);

            return mesh;
        }

        private static void AddVertex(int index, MeshGeometry3D mesh, List<Point3D> points)
        {
            mesh.TriangleIndices.Add(mesh.Positions.Count);
            mesh.Positions.Add(points[index]);
        }
    }
}