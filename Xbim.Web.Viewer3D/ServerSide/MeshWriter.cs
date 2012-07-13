using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Xml;
using Xbim.IO;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.ModelGeometry.Scene;
using System.Windows.Media.Media3D;

namespace Xbim.Web.Viewer3D.ServerSide
{
    public class MeshWriter
    {

        public MeshWriter() : this(false)
        { }

        public MeshWriter(bool displayAll)
        {
            this.displayAll = displayAll;
        }

        /// <summary>
        /// Default types to render
        /// </summary>
        Type[] DefaultTypes = new Type[] {
                    typeof(Xbim.Ifc2x3.SharedBldgElements.IfcWallStandardCase),
                    typeof(Xbim.Ifc2x3.SharedBldgElements.IfcWall),
                    typeof(Xbim.Ifc2x3.SharedBldgElements.IfcRoof),
                    typeof(Xbim.Ifc2x3.SharedBldgElements.IfcColumn),
                    typeof(Xbim.Ifc2x3.SharedBldgElements.IfcBeam),
                    typeof(Xbim.Ifc2x3.SharedBldgElements.IfcWindow), 
                    typeof(Xbim.Ifc2x3.SharedBldgElements.IfcDoor),
                    typeof(Xbim.Ifc2x3.SharedBldgElements.IfcStair),
                    typeof(Xbim.Ifc2x3.ProductExtension.IfcSpace),
                    typeof(Xbim.Ifc2x3.SharedBldgElements.IfcSlab),
                    typeof(IfcElectricalElement),
                    typeof(IfcDistributionElement)
                };

        public void WriteMesh(XbimRepo repo, Stream deststream)
        {
         
            // prepares stream
            
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                CloseOutput = false,
                Indent = false
            };
            XmlWriter xmlWriter = XmlTextWriter.Create(deststream, settings);
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteComment("XMLSerialised XBIM geometry");
            xmlWriter.WriteStartElement("XBIMGeometry");

            // prepare xbim

            var scene = repo.Scene;

            // loop elements
            try
            {
                
                foreach (var item in scene.Graph.ProductNodes)
                {
                    XbimMeshGeometry3D m3D = new XbimMeshGeometry3D();

                    
                    XbimTriangulatedModelStream tm = scene.Triangulate(item.Value); // GetTriangulatedModelStream
                    if (!tm.IsEmpty)
                    {
                        if (!item.Value.Product.GetType().IsSubclassOf(typeof(IfcFeatureElementSubtraction)))
                        {
                            tm.Build(m3D);
                            //SpaceTriangulatedModel.XMLWriterInit(writer);
                            //todo: make sure IfcSite is not rendered if (product is IfcSite)
                            OutputProduct(xmlWriter, tm, item.Value.Product, item.Value.WorldMatrix());
                            //SpaceTriangulatedModel.XMLWriterFinalise(writer);
                        }
                    }
                }
            }
            finally
            {
                scene.Close();
            }

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }


        private static void ValidateFiles(string semanticfilename, string geometricfilename)
        {
            if(!File.Exists(semanticfilename))
            {
                throw new InvalidOperationException(
                    String.Format("Can't locate semantic data file {0}", semanticfilename));
            }

            if(!File.Exists(geometricfilename))
            {
                throw new InvalidOperationException(
                    String.Format("Can't locate geometric data file {0}", geometricfilename));
            }
        }

        private void OutputProduct(XmlWriter writer, XbimTriangulatedModelStream CachedStream, IfcProduct product, Matrix3D matrix3D)
        {
            try
            {
                if (ShouldOutput(product))
                {
                    // MeshGeometry3D m3D = CachedStream.AsMeshGeometry3D();
                    //SpaceTriangulatedModel oneitem = xbimGeomCache.GetTriangulatedModel(product.EntityLabel);
                    // todo : transform the stream to XML then write it to the writer.
                    WriteMeshXml(writer, CachedStream, product, matrix3D);
                    // oneitem.DumpInXMLWriter(writer);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Skipping {0} - {1}", product.GetType(), product.Name);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning(String.Format("Failed to Output : {0} - {1}", product.ToString(), ex.Message));
            }
        }

        public class BoundingBox
        {
            public bool IsValid = false;
            public Point3D PointMin = new Point3D();
            public Point3D PointMax = new Point3D();

            /// <summary>
            /// Extends to include the specified point; returns true if boundaries were changed.
            /// </summary>
            /// <param name="Point"></param>
            /// <returns></returns>
            internal bool IncludePoint(Point3D Point)
            {
                if (!IsValid)
                {
                    PointMin = Point;
                    PointMax = Point;
                    IsValid = true;
                    return true;
                }
                bool ret = false;

                if (PointMin.X > Point.X)
                { PointMin.X = Point.X; ret = true; }
                if (PointMin.Y > Point.Y)
                { PointMin.Y = Point.Y; ret = true; }
                if (PointMin.Z > Point.Z)
                { PointMin.Z = Point.Z; ret = true; }

                if (PointMax.X < Point.X)
                { PointMax.X = Point.X; ret = true; }
                if (PointMax.Y < Point.Y)
                { PointMax.Y = Point.Y; ret = true; }
                if (PointMax.Z < Point.Z)
                { PointMax.Z = Point.Z; ret = true; }

                IsValid = true;

                return ret;
            }

            internal void IncludeBoundingBox(BoundingBox childBB)
            {
                if (!childBB.IsValid)
                    return;
                this.IncludePoint(childBB.PointMin);
                this.IncludePoint(childBB.PointMax);
            }

            internal BoundingBox TransformBy(Matrix3D Matrix)
            {
                BoundingBox newBB = new BoundingBox();
                // include all 8 vertices of the box.
                newBB.IncludePoint(new Point3D(PointMin.X, PointMin.Y, PointMin.Z), Matrix);
                newBB.IncludePoint(new Point3D(PointMin.X, PointMin.Y, PointMax.Z), Matrix);
                newBB.IncludePoint(new Point3D(PointMin.X, PointMax.Y, PointMin.Z), Matrix);
                newBB.IncludePoint(new Point3D(PointMin.X, PointMax.Y, PointMax.Z), Matrix);

                newBB.IncludePoint(new Point3D(PointMax.X, PointMin.Y, PointMin.Z), Matrix);
                newBB.IncludePoint(new Point3D(PointMax.X, PointMin.Y, PointMax.Z), Matrix);
                newBB.IncludePoint(new Point3D(PointMax.X, PointMax.Y, PointMin.Z), Matrix);
                newBB.IncludePoint(new Point3D(PointMax.X, PointMax.Y, PointMax.Z), Matrix);
                return newBB;
            }

            private bool IncludePoint(Point3D Point, Matrix3D Matrix)
            {
                Point3D t = Point3D.Multiply(Point, Matrix);
                return IncludePoint(t);
            }
        }


        private void WriteMeshXml(XmlWriter xmlWriter, XbimTriangulatedModelStream m3D, IfcProduct product, Matrix3D matrix3D)
        {
            xmlWriter.WriteStartElement("Mesh");
            if (product.EntityLabel != 0)
            {
                xmlWriter.WriteAttributeString("EntityLabel", Math.Abs(product.EntityLabel).ToString());

                var Material = Materials.LookupMaterial(product);
                if (!String.IsNullOrEmpty(Material))
                {
                    xmlWriter.WriteAttributeString("Material", Material);
                }
            }
            WriteTransformMatrix(matrix3D, xmlWriter);
            MeshGeometry mg = new MeshGeometry();
            m3D.Build(mg);
            BoundingBox bb = WriteGeometry(mg, xmlWriter);
            if (!matrix3D.IsIdentity && bb.IsValid)
            {
                BoundingBox Wbb = bb.TransformBy(matrix3D);
                xmlWriter.WriteStartElement("WBB");
                xmlWriter.WriteAttributeString("MnX", Wbb.PointMin.X.ToString(format));
                xmlWriter.WriteAttributeString("MnY", Wbb.PointMin.Y.ToString(format));
                xmlWriter.WriteAttributeString("MnZ", Wbb.PointMin.Z.ToString(format));

                xmlWriter.WriteAttributeString("MxX", Wbb.PointMax.X.ToString(format));
                xmlWriter.WriteAttributeString("MxY", Wbb.PointMax.Y.ToString(format));
                xmlWriter.WriteAttributeString("MxZ", Wbb.PointMax.Z.ToString(format));
                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
        }

        private static BoundingBox WriteGeometry(MeshGeometry mesh, XmlWriter xmlWriter)
        {
            BoundingBox bb = new BoundingBox();
            //foreach (var item in mesh)
            //{
            //    MeshGeometry m = item as MeshGeometry;
            //    if (m != null)
            //    {
            //        xmlWriter.WriteStartElement("Mesh");
            //        BoundingBox childBB = WriteGeometry(m, xmlWriter);
            //        bb.IncludeBoundingBox(childBB);
            //        xmlWriter.WriteEndElement();
            //    }
            //}
            if (mesh.UniquePoints.Count > 0)
            {
                for (int i = 0; i < mesh.UniquePoints.Count; i++)
                {
                    bb.IncludePoint(mesh.Positions[mesh.UniquePoints[i].PositionIndex]);

                    xmlWriter.WriteStartElement("PN");
                    xmlWriter.WriteAttributeString("PX", mesh.Positions[mesh.UniquePoints[i].PositionIndex].X.ToString(format));
                    xmlWriter.WriteAttributeString("PY", mesh.Positions[mesh.UniquePoints[i].PositionIndex].Y.ToString(format));
                    xmlWriter.WriteAttributeString("PZ", mesh.Positions[mesh.UniquePoints[i].PositionIndex].Z.ToString(format));

                    xmlWriter.WriteAttributeString("NX", mesh.Normals[mesh.UniquePoints[i].NormalIndex].X.ToString(format));
                    xmlWriter.WriteAttributeString("NY", mesh.Normals[mesh.UniquePoints[i].NormalIndex].Y.ToString(format));
                    xmlWriter.WriteAttributeString("NZ", mesh.Normals[mesh.UniquePoints[i].NormalIndex].Z.ToString(format));
                    xmlWriter.WriteEndElement();
                }
                for (int i = 0; i < mesh.TriangleIndices.Count; )
                {
                    xmlWriter.WriteStartElement("F");
                    xmlWriter.WriteAttributeString("I1", mesh.TriangleIndices[i++].ToString());
                    xmlWriter.WriteAttributeString("I2", mesh.TriangleIndices[i++].ToString());
                    xmlWriter.WriteAttributeString("I3", mesh.TriangleIndices[i++].ToString());
                    xmlWriter.WriteEndElement();
                }
            }
            if (bb.IsValid)
            {
                xmlWriter.WriteStartElement("BB");
                xmlWriter.WriteAttributeString("MnX", bb.PointMin.X.ToString(format));
                xmlWriter.WriteAttributeString("MnY", bb.PointMin.Y.ToString(format));
                xmlWriter.WriteAttributeString("MnZ", bb.PointMin.Z.ToString(format));

                xmlWriter.WriteAttributeString("MxX", bb.PointMax.X.ToString(format));
                xmlWriter.WriteAttributeString("MxY", bb.PointMax.Y.ToString(format));
                xmlWriter.WriteAttributeString("MxZ", bb.PointMax.Z.ToString(format));
                xmlWriter.WriteEndElement();
            }

            return bb;
        }


        private const string format = "G6";

        private static void WriteTransformMatrix(Matrix3D matrix, XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("T");

            if (matrix.IsIdentity)
                xmlWriter.WriteAttributeString("value", "Identity");
            else
            {
                xmlWriter.WriteAttributeString("M11", matrix.M11.ToString(format));
                xmlWriter.WriteAttributeString("M12", matrix.M12.ToString(format));
                xmlWriter.WriteAttributeString("M13", matrix.M13.ToString(format));
                xmlWriter.WriteAttributeString("M14", matrix.M14.ToString(format));

                xmlWriter.WriteAttributeString("M21", matrix.M21.ToString(format));
                xmlWriter.WriteAttributeString("M22", matrix.M22.ToString(format));
                xmlWriter.WriteAttributeString("M23", matrix.M23.ToString(format));
                xmlWriter.WriteAttributeString("M24", matrix.M24.ToString(format));

                xmlWriter.WriteAttributeString("M31", matrix.M31.ToString(format));
                xmlWriter.WriteAttributeString("M32", matrix.M32.ToString(format));
                xmlWriter.WriteAttributeString("M33", matrix.M33.ToString(format));
                xmlWriter.WriteAttributeString("M34", matrix.M34.ToString(format));

                xmlWriter.WriteAttributeString("M41", matrix.OffsetX.ToString(format));
                xmlWriter.WriteAttributeString("M42", matrix.OffsetY.ToString(format));
                xmlWriter.WriteAttributeString("M43", matrix.OffsetZ.ToString(format));
                xmlWriter.WriteAttributeString("M44", matrix.M44.ToString(format));

            }
            xmlWriter.WriteEndElement();
        }


        private bool ShouldOutput(IfcProduct product)
        {
            return (DefaultTypes.Contains(product.GetType()) || displayAll);
        }

        private bool displayAll = false;
    }
}