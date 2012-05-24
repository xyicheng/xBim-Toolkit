using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using Xbim.IO;
using Xbim.DOM;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.ProductExtension;
using System.Diagnostics;
using Xbim.ModelGeometry.Scene;
using System.Xml;
using System.Text;
using System.Windows.Media.Media3D;
using Xbim.XbimExtensions;

namespace Xbim.Web.Viewer3D.ServerSide
{
    public class XbimRepo
    {
        private XbimFileModelServer _model;
        private IXbimScene _scene;

        public IXbimScene Scene { get { return _scene; } }

        public XbimRepo(string fileName)
        {
            string xbimFile = fileName;
            string gcFile = Path.ChangeExtension(xbimFile, ".xbimGC");
            if (!File.Exists(xbimFile)) throw new Exception("Semantic file does not exist");
            if (!File.Exists(gcFile)) throw new Exception("Geometry file does not exist");

            _model = new XbimFileModelServer();
            _model.Open(xbimFile);
            _scene = new XbimSceneStream(_model, gcFile); // opens the pre-calculated Geometry file
             
        }
        public XbimRepo(XbimFileModelServer model, IXbimScene scene)
        {
            _model = model;
            _scene = scene;
        }

        public long SelectedItem = -1;

        public class NoteList{
            public List<Note> Notes = new List<Note>();
        }

        public class Note
        {
            public string title;
            public string text;

            public Note(string NewTitle, string NewText)
            {
                title = NewTitle;
                text = NewText;
            }

        }

        public Dictionary<long, NoteList> AllNotes = new Dictionary<long, NoteList>();

        public List<Note> SelecteditemNotes
        {
            get
            {
                if (SelectedItem != -1 && AllNotes.ContainsKey(SelectedItem))
                    return AllNotes[SelectedItem].Notes;
                return new List<Note>();
            }
        }

        public string TypesToMesh
        {
            get;
            set;
        }


        public bool RenderAll()
        {
            return true;
            // return TypesToMesh == "All";
        }

      

        public void WriteTg(HttpResponse Response)
        {
            XmlInit(Response);


            // prepares stream
            // MemoryStream resultStream = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                CloseOutput = false,
                Indent = false
            };
            XmlWriter xmlWriter = XmlWriter.Create(Response.OutputStream, settings);
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteComment("XMLSerialised XBIM Transformgraph");
            xmlWriter.WriteStartElement("XBIMTransformGraph");

            // prepare xbim

            xmlWriter.WriteStartElement("Scene");
            WriteNodeRecursive(_scene.Graph.Root, xmlWriter);

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();

            BinaryClose(Response);
        }

        private void WriteNodeRecursive(TransformNode transformNode, XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("Node");
            xmlWriter.WriteAttributeString("label", transformNode.ProductId.ToString());
            xmlWriter.WriteAttributeString("geom", transformNode.HasGeometryModel.ToString());
            if (transformNode.Product != null)
            {
                string s = transformNode.Product.GetType().Name;
                xmlWriter.WriteAttributeString("t", s);
            }
            WriteBoundingBox(transformNode.BoundingBox, xmlWriter);
            // WriteTransformMatrix("LT", transformNode.LocalMatrix, xmlWriter);
            WriteTransformMatrix("WT", transformNode.WorldMatrix(), xmlWriter);
            foreach (var item in transformNode.Children)
            {
                WriteNodeRecursive(item, xmlWriter);
            }
            // xmlWriter.WriteAttributes(
            xmlWriter.WriteEndElement();
        }

        private const string format = "G6";

        private static void WriteTransformMatrix(string ElementName,  Matrix3D matrix, XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement(ElementName);

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

        private static void WriteBoundingBox(Rect3D Wbb, XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("WBB");
            xmlWriter.WriteAttributeString("MnX", Wbb.X.ToString(format));
            xmlWriter.WriteAttributeString("MnY", Wbb.Y.ToString(format));
            xmlWriter.WriteAttributeString("MnZ", Wbb.Z.ToString(format));

            xmlWriter.WriteAttributeString("MxX", (Wbb.X + Wbb.SizeX).ToString(format));
            xmlWriter.WriteAttributeString("MxY", (Wbb.Y + Wbb.SizeY).ToString(format));
            xmlWriter.WriteAttributeString("MxZ", (Wbb.Z + Wbb.SizeZ).ToString(format));
            xmlWriter.WriteEndElement();
        }

        private void XmlInit(HttpResponse Response)
        {

            Response.ContentType = "text/xml";
            // Response.Charset = "x-user-defined";
        }


        public void WriteBinaryMesh(HttpResponse Response, string CommaSepIds)
        {
            if (CommaSepIds == null)
                return;

            BinaryInit(Response);

            // BinaryWriter bw = new BinaryWriter(Response.OutputStream);


            string[] sIds = CommaSepIds.Split(',');
            List<long> ids = new List<long>();
            foreach (var item in sIds)
            {
                long val = -1;
                if (long.TryParse(item, out val))
                {
                    ids.Add(val);
                }
            }

            foreach (var item in ids)
            {
                if (!Scene.Graph.ProductNodes.ContainsKey(item))
                    continue;
                TransformNode tn = Scene.Graph.ProductNodes[item];
                XbimTriangulatedModelStream tm = Scene.Triangulate(tn);
                if (!tm.IsEmpty)
                {
                    // byte[] header = new byte[8];
                    // MemoryStream m = new MemoryStream(header, true);
                    
                    BinaryWriter bw = new BinaryWriter(Response.OutputStream);
                    bw.Write((int)item);
                    int size = (int)(tm.DataStream.Length + sizeof(UInt16) + sizeof(byte));
                    bw.Write(size);
                    // bw.Write(tm.NumChildren);
                    bw.Write(tm.HasData);
                    bw.Close();

                    tm.DataStream.CopyTo(Response.OutputStream);
                }
            }
            Scene.Close();
            BinaryClose(Response);
        }

        private static void BinaryClose(HttpResponse Response)
        {
            Response.OutputStream.Flush();
            Response.OutputStream.Close();
            Response.End();
        }

        private static void BinaryInit(HttpResponse Response)
        {
            Response.ContentType = "application/octet-stream";
            Response.Charset = "x-user-defined";
        }

        public void WriteXMLMesh(HttpResponse Response)
        {
            XmlInit(Response);
            MeshWriter writer = new MeshWriter(false);
            writer.WriteMesh(this, Response.OutputStream);
            BinaryClose(Response);
        }
    }
}