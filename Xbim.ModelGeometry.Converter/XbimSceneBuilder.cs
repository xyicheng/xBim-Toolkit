using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using Xbim.Common.Exceptions;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;

namespace Xbim.ModelGeometry.Converter
{
    public class XbimSceneBuilder
    {
        /// <summary>
        /// This function builds a scene of all IfcProducts in the model, excluding the geometry of Openings
        /// It will create a scene database, overwriting any of the same name
        /// </summary>
        /// <param name="model">Model containing the model entities</param>
        /// <param name="sceneDbName">Name of scene DB file</param>
         public void BuildGlobalScene(XbimModel model, string sceneDbName)
        {
             if(File.Exists(sceneDbName)) File.Delete(sceneDbName);
            XbimSqliteDB db = new XbimSqliteDB(sceneDbName);
            //Create the scene table
            db.CreateTable("CREATE TABLE IF NOT EXISTS 'Scenes' (" +
                    "'SceneId' INTEGER PRIMARY KEY," +
                    "'SceneName' VARCHAR NOT NULL," +
                    "'BoundingBox' BLOB" +
                    ");");
            //create the layer table
            db.CreateTable("CREATE TABLE IF NOT EXISTS 'Layers' (" +
                    "'SceneName' VARCHAR NOT NULL," +
                    "'LayerName' VARCHAR NOT NULL," +
                    "'LayerId' INTEGER PRIMARY KEY," +
                    "'ParentLayerId' INTEGER, " +
                    "'Meshes' BLOB, " +
                    "'XbimTexture' BLOB," +
                    "'BoundingBox' BLOB" +
                    ");");
            //create the meta data table
            db.CreateTable("CREATE TABLE IF NOT EXISTS 'Meta' (" +
                    "'Meta_ID' INTEGER PRIMARY KEY," +
                    "'Meta_type' text not null," +
                    "'Meta_key' text," +
                    "'Meta_Value' text not null" +
                    ");");
            //get a connection
            SQLiteConnection connection = db.GetConnection();
            try
            {
                XbimGeometryHandleCollection handles = new XbimGeometryHandleCollection(model.GetGeometryHandles()
                                                           .Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT));
                XbimRect3D modelBounds = XbimRect3D.Empty;
                XbimColourMap cmap = new XbimColourMap();
                int layerid = 1;
                foreach (var layerContent in handles.FilterByBuildingElementTypes())
                {
                    string elementTypeName = layerContent.Key;
                    XbimGeometryHandleCollection layerHandles = layerContent.Value;
                    IEnumerable<XbimGeometryData> geomColl = model.GetGeometryData(layerHandles);
                    XbimColour colour = cmap[elementTypeName];
                    XbimMeshLayer<XbimMeshGeometry3D, XbimRenderMaterial> layer = new XbimMeshLayer<XbimMeshGeometry3D, XbimRenderMaterial>(colour) { Name = elementTypeName };
                    //add all content initially into the hidden field
                    foreach (var geomData in geomColl)
                    {
                        layer.AddToHidden(geomData, model);
                    }
                    
                    if (modelBounds.IsEmpty)
                        modelBounds = layer.BoundingBoxHidden();
                    else
                        modelBounds.Union(layer.BoundingBoxHidden());
                   
                    // add  top level layers
                    layerid = AddLayer(connection, layer, layerid, -1);
                    layerid++;
                }
                // setup bounding box SizeX, SizeY, SizeZ, X, Y, Z
                byte[] boundingBoxFull = new byte[6 * sizeof(float)];
                MemoryStream ms = new MemoryStream(boundingBoxFull);
                BinaryWriter bw = new BinaryWriter(ms);

                bw.Write(modelBounds.SizeX);
                bw.Write(modelBounds.SizeY);
                bw.Write(modelBounds.SizeZ);
                bw.Write(modelBounds.X);
                bw.Write(modelBounds.Y);
                bw.Write(modelBounds.Z);
                
                // create scene row in Scenes tabls
                string str = "INSERT INTO Scenes (SceneId, SceneName, BoundingBox) ";
                str += "VALUES (@SceneId, @SceneName, @BoundingBox) ";
                using (SQLiteTransaction SQLiteTrans = connection.BeginTransaction())
                {
                    using (SQLiteCommand cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = str;
                        cmd.Parameters.Add("@SceneId", DbType.Int32).Value = 1;
                        cmd.Parameters.Add("@SceneName", DbType.String).Value = "MainScene";
                        cmd.Parameters.Add("@BoundingBox", DbType.Binary).Value = boundingBoxFull;
                        cmd.ExecuteNonQuery();
                    }
                    SQLiteTrans.Commit();
                }

                //now add some meta data
                foreach (var space in model.Instances.OfType<IfcSpace>())
                {
                    AddMetaData(connection,space.GetType().Name,space.Name,space.EntityLabel.ToString());
                }
            }
            finally
            {
                connection.Close();
            }
        }

        private int AddLayer(SQLiteConnection mDBcon, XbimMeshLayer<XbimMeshGeometry3D, XbimRenderMaterial> layer, int layerid, int parentLayerId)
        {
             try
             {

                 // bytes contain 6 floats and 1 int
                 // pointX, pointY, pointZ, normalX, normalY, normalZ, entityLabel
                 // this is the vbo.

                 XbimTexture texture = layer.Style;
                 XbimColour c = layer.Style.ColourMap[0]; //take the first one, not perhaps correct or best practice
                 byte[] colour = new byte[4 * sizeof(float)];
                 MemoryStream ms = new MemoryStream(colour);
                 BinaryWriter bw = new BinaryWriter(ms);
                 bw.Write(c.Red);
                 bw.Write(c.Green);
                 bw.Write(c.Blue);
                 bw.Write(c.Alpha);

                 byte[] vbo = ((XbimMeshGeometry3D)layer.Hidden).ToByteArray();

                 XbimRect3D bb = layer.BoundingBoxHidden();
                 // setup bounding box SizeX, SizeY, SizeZ, X, Y, Z
                 byte[] bbArray = new byte[6 * sizeof(float)];
                 ms = new MemoryStream(bbArray);
                 bw = new BinaryWriter(ms);

                 bw.Write(bb.SizeX);
                 bw.Write(bb.SizeY);
                 bw.Write(bb.SizeZ);
                 bw.Write(bb.X);
                 bw.Write(bb.Y);
                 bw.Write(bb.Z);

                 string str = "INSERT INTO Layers (SceneName, LayerName, LayerId, ParentLayerId, ";
                 str += "Meshes, XbimTexture, BoundingBox) ";

                 //str += "VALUES (" + "SceneName" + layer.Name + layer.Name + "-1" + positions + normals + triangleIndices + "" + "" + "" + "" + "" + colour + "" + "" + ")";
                 str += "VALUES (@SceneName, @LayerName, @LayerId, @ParentLayerId, ";
                 str += "@Meshes, @XbimTexture, @BoundingBox) ";


                 using (SQLiteTransaction SQLiteTrans = mDBcon.BeginTransaction())
                 {
                     using (SQLiteCommand cmd = mDBcon.CreateCommand())
                     {
                         cmd.CommandText = str;
                         cmd.Parameters.Add("@SceneName", DbType.String).Value = "MainScene";
                         cmd.Parameters.Add("@LayerName", DbType.String).Value = layer.Name;
                         cmd.Parameters.Add("@LayerId", DbType.Int32).Value = layerid;
                         cmd.Parameters.Add("@ParentLayerId", DbType.Int32).Value = parentLayerId;
                         cmd.Parameters.Add("@Meshes", DbType.Binary).Value = vbo;
                         cmd.Parameters.Add("@XbimTexture", DbType.Binary).Value = colour;
                         cmd.Parameters.Add("@BoundingBox", DbType.Binary).Value = bbArray;
                         cmd.ExecuteNonQuery();
                     }

                     SQLiteTrans.Commit();
                 }

                 int myParent = layerid;
                 foreach (var subLayer in layer.SubLayers)
                 {
                     layerid++;
                     layerid = AddLayer(mDBcon, subLayer, layerid, myParent);
                 }
                 return layerid;

             }
             catch (Exception ex)
             {
                 throw new XbimException("Error building scene layer" , ex);
             }
        }

        /// <summary>
        /// Adds a receord to the Meta table
        /// </summary>
        /// <param name="Type">Type of record (required)</param>
        /// <param name="Identifier">Optional</param>
        /// <param name="Value">Any string persistence mechanism of choice (required).</param>
        public void AddMetaData(SQLiteConnection mDBcon, string Type, string Value, string Identifier = null)
        {
            string str = "INSERT INTO Meta (" +
                "'Meta_type', 'Meta_Value', 'Meta_key' " +
                ") values (" +
                "@Meta_type, @Meta_Value, @Meta_key " +
                ")";

            using (SQLiteTransaction SQLiteTrans = mDBcon.BeginTransaction())
            {
                using (SQLiteCommand cmd = mDBcon.CreateCommand())
                {
                    cmd.CommandText = str;
                    cmd.Parameters.Add("@Meta_type", DbType.String).Value = Type;
                    cmd.Parameters.Add("@Meta_Value", DbType.String).Value = Value;
                    if (Identifier == null)
                        cmd.Parameters.Add("@Meta_key", DbType.String).Value = DBNull.Value;
                    else
                        cmd.Parameters.Add("@Meta_key", DbType.String).Value = Identifier;
                    cmd.ExecuteNonQuery();
                }
                SQLiteTrans.Commit();
            }
        }

    }
}
