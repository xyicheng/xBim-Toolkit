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
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Extensions;

namespace Xbim.ModelGeometry.Converter
{
    public class XbimSceneBuilder
    {
        static XbimSceneBuilder()
        {
            AssemblyResolver.HandleUnresolvedAssemblies();
        }

        /// <summary>
        /// This function builds a scene of all IfcProducts in the model, excluding the geometry of Openings
        /// It will create a scene database, overwriting any of the same name
        /// </summary>
        /// <param name="model">Model containing the model entities</param>
        /// <param name="sceneDbName">Name of scene DB file</param>
        /// <param name="Logger">Logging engine for detailed feedback</param>
        public void BuildGlobalScene(XbimModel model, string sceneDbName, Common.Logging.ILogger Logger = null)
        {
            if (File.Exists(sceneDbName)) File.Delete(sceneDbName);
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
            using (SQLiteConnection connection = db.GetConnection())
            {

                try
                {
                    short spaceId = IfcMetaData.IfcTypeId(typeof(IfcSpace));
                    XbimGeometryHandleCollection handles = new XbimGeometryHandleCollection(model.GetGeometryHandles()
                                                               .Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT));
                    XbimRect3D modelBounds = XbimRect3D.Empty;
                    XbimColourMap cmap = new XbimColourMap();
                    int layerid = 1;
                    IfcProject project = model.IfcProject;
                    int projectId = 0;
                    if (project != null) projectId = Math.Abs(project.EntityLabel);
                    XbimGeometryData regionData = model.GetGeometryData(projectId, XbimGeometryType.Region).FirstOrDefault(); //get the region data should only be one
                    float mScalingReference = (float)model.GetModelFactors.OneMetre;

                    if (Logger != null)
                        Logger.DebugFormat("XbimScene: Scaling reference {0}", mScalingReference);

                    XbimMatrix3D translate = XbimMatrix3D.Identity;
                    XbimMatrix3D scale = XbimMatrix3D.CreateScale(1 / mScalingReference);

                    if (regionData != null)
                    {
                        XbimRegionCollection regions = XbimRegionCollection.FromArray(regionData.ShapeData);
                        XbimRegion largest = regions.MostPopulated();
                        if (largest != null)
                        {
                            translate = XbimMatrix3D.CreateTranslation(
                                -largest.Centre.X,
                                -largest.Centre.Y,
                                -largest.Centre.Z
                                );
                        }
                    }
                    XbimMatrix3D composed = translate * scale;

                    foreach (var layerContent in handles.FilterByBuildingElementTypes())
                    {
                        string elementTypeName = layerContent.Key;
                        XbimGeometryHandleCollection layerHandles = layerContent.Value;
                        IEnumerable<XbimGeometryData> geomColl = model.GetGeometryData(layerHandles);
                        XbimColour colour = cmap[elementTypeName];
                        XbimMeshLayer<XbimMeshGeometry3D, XbimRenderMaterial> layer = new XbimMeshLayer<XbimMeshGeometry3D, XbimRenderMaterial>(model, colour) { Name = elementTypeName };
                        //add all content initially into the hidden field
                        foreach (var geomData in geomColl)
                        {
                            geomData.TransformBy(composed);
                            if (geomData.IfcTypeId == spaceId)
                                layer.AddToHidden(geomData);
                            else
                                layer.AddToHidden(geomData, model);
                        }

                        if (modelBounds.IsEmpty)
                            modelBounds = layer.BoundingBoxHidden();
                        else
                            modelBounds.Union(layer.BoundingBoxHidden());

                        // add  top level layers
                        layerid = AddLayerToSceneDatabase(connection, layer, layerid, -1);
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

                    double ZBoundaryLow = modelBounds.Z;
                    double ZBoundaryHigh = modelBounds.Z + modelBounds.SizeZ;

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
                        AddMetaData(connection, space.GetType().Name, space.Name??"Undefined Space", space.EntityLabel.ToString());
                    }

                    // this causes a crash if no elevation has been defined used the extension method which does not
                    // var storeys = model.Instances.OfType<IfcBuildingStorey>().OrderBy(t => Convert.ToDouble(t.Elevation.Value)).ToArray();
                    IfcBuilding bld = model.IfcProject.GetBuildings().FirstOrDefault();
                    if (bld != null)
                    {
                        List<StoreyInfo> Storeys = new List<StoreyInfo>();

                        double metre = model.GetModelFactors.OneMetre;
                        double storeyHeight = 0;//all scenes are in metres
                        int defaultStoreyName = 0;

                        double MinElev = double.PositiveInfinity;
                        double MaxElev = double.NegativeInfinity;

                        foreach (var storey in bld.GetBuildingStoreys(true))
                        {
                            string cleanName;
                            if(storey.Name.HasValue)
                                cleanName = storey.Name.Value.ToString().Replace(';', ' ');
                            else
                                cleanName = "Floor " + defaultStoreyName++;
                            if (storey.Elevation.HasValue)
                                storeyHeight = storey.Elevation.Value; // values are to be tranaformed to meters later with the wcsTransform
                            else
                                storeyHeight += 3 * metre; //default to 3 metres

                            // apply the transformation previously applied to the building 
                            XbimPoint3D InTranslatedReference = composed.Transform(
                                new XbimPoint3D(0, 0, storeyHeight)
                                );

                            double InTranslatedReferenceZ = InTranslatedReference.Z; // then express it in meters.
                            if (Logger != null)
                                Logger.DebugFormat("StoreyName: {0}; Model Elevation: {1}; Scene Elevation: {2}", cleanName, storeyHeight, InTranslatedReferenceZ);

                            MinElev = Math.Min(MinElev, InTranslatedReferenceZ);
                            MaxElev = Math.Max(MaxElev, InTranslatedReferenceZ);
                            Storeys.Add(new StoreyInfo() { Name = cleanName, Elevation = InTranslatedReferenceZ });
                        }

                        double deltaElev = 0;
                        if (MinElev < ZBoundaryLow || MaxElev > ZBoundaryHigh)
                        {
                            double midFrom = (MinElev + MaxElev) / 2;
                            double midTo = (ZBoundaryLow + ZBoundaryHigh) / 2;
                            deltaElev = midTo - midFrom;
                            Logger.DebugFormat("Elevation corrected by: {0}", deltaElev);
                        }

                        foreach (var Storey in Storeys)
                        {
                            AddMetaData(
                                connection,
                                "Storey",
                                string.Format("Name:{0};Elevation:{1};", Storey.Name, Storey.Elevation + deltaElev), // storeyHeight),
                                Storey.Name);    
                        }

                        
                    }
                }
                finally
                {
                    connection.Close();
                    SQLiteConnection.ClearPool(connection);
                    GC.Collect();
                }
            }

        }

        private class StoreyInfo
        {
            public string Name;
            public double Elevation;
        }

        private int AddLayerToSceneDatabase(SQLiteConnection mDBcon, XbimMeshLayer<XbimMeshGeometry3D, XbimRenderMaterial> layer, int layerid, int parentLayerId)
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

                MemoryStream memoryStream = new MemoryStream();
                using (ICSharpCode.SharpZipLib.GZip.GZipOutputStream zis = new ICSharpCode.SharpZipLib.GZip.GZipOutputStream(memoryStream))
                {
                    zis.Write(vbo, 0, vbo.Length);
                    memoryStream.Flush();
                }

                //using (Ionic.Zlib.GZipStream gZipStream = new Ionic.Zlib.GZipStream(memoryStream, Ionic.Zlib.CompressionMode.Compress))
                //{
                //    gZipStream.Write(vbo, 0, vbo.Length);
                //    memoryStream.Flush();
                //}
                vbo = memoryStream.ToArray();


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
                    layerid = AddLayerToSceneDatabase(mDBcon, subLayer, layerid, myParent);
                }
                return layerid;

            }
            catch (Exception ex)
            {
                throw new XbimException("Error building scene layer", ex);
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
