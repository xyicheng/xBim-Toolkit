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

    [Flags]
    public enum GenerateSceneOption
    {
        None = 0,
        IncludeRegions = 1,
        IncludeStoreys = 2,
        IncludeSpaces = 4,
        IncludeSpacesBBox = 8,
        IncludeSpacesStoreyId = 16,
        IncludeTransform = 32,
        All = IncludeTransform * 2 - 1
    }

    public class XbimSceneBuilder
    {
        static XbimSceneBuilder()
        {
            AssemblyResolver.HandleUnresolvedAssemblies();
        }

        public XbimSceneBuilder()
        {
            Options = GenerateSceneOption.IncludeRegions | 
                      GenerateSceneOption.IncludeStoreys | 
                      GenerateSceneOption.IncludeSpaces;
        }

        public GenerateSceneOption Options { get; set; }

        /// <summary>
        /// This function builds a scene of all IfcProducts in the model, excluding the geometry of Openings
        /// It will create a scene database, overwriting any of the same name
        /// </summary>
        /// <param name="model">Model containing the model entities</param>
        /// <param name="sceneDbName">Name of scene DB file</param>
        /// <param name="Logger">Logging engine for detailed feedback</param>
        public void BuildGlobalScene(XbimModel model, string sceneDbName, Common.Logging.ILogger Logger = null)
        {
            if (File.Exists(sceneDbName)) 
                File.Delete(sceneDbName);


            //get a connection
            using (var db = new XbimSqliteDB(sceneDbName))
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
                    uint projectId = 0;
                    if (project != null) projectId = project.EntityLabel;
                    
                    float mScalingReference = (float)model.ModelFactors.OneMetre;

                    if (Logger != null)
                        Logger.DebugFormat("XbimScene: Scaling reference {0}\r\n", mScalingReference);

                    XbimMatrix3D translate = XbimMatrix3D.Identity;
                    XbimMatrix3D scale = XbimMatrix3D.CreateScale(1 / mScalingReference);
                    XbimMatrix3D composed = translate * scale;
                    XbimGeometryData regionData = model.GetGeometryData(projectId, XbimGeometryType.Region).FirstOrDefault(); //get the region data should only be one
                    if (regionData != null)
                    {
                        // this results in centering the most populated area of the model
                        //
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
                        composed = translate * scale;

                        // store region information in Scene
                        if ((Options & GenerateSceneOption.IncludeRegions) == GenerateSceneOption.IncludeRegions)
                        {
                            if (Logger != null)
                                Logger.DebugFormat("XbimScene: Exporting regions.\r\n", mScalingReference);
                            foreach (var item in regions)
                            {
                                // the bounding box needs to be moved/scaled by the transform.
                                //
                                XbimRect3D transformed = item.ToXbimRect3D().Transform(composed);
                                db.AddMetaData(
                                        "Region",
                                        0,
                                        string.Format("Name:{0};Box:{1};", item.Name, transformed.ToString()), // verbose, but only a few items are expected in the model
                                        item.Name
                                        );
                    }
                        }
                    }

                    if ((Options & GenerateSceneOption.IncludeTransform) == GenerateSceneOption.IncludeTransform)
                    {
                        if (Logger != null)
                            Logger.DebugFormat("XbimScene: Exporting transform.\r\n", mScalingReference);
                        db.AddMetaData(
                                "Transform",
                                0,
                                composed.ToArray(false),
                                "World"
                                );
                        db.Flush();
                    }

                    if (Logger != null)
                        Logger.DebugFormat("XbimScene: Exporting layers.\r\n", mScalingReference);
                    foreach (var layerContent in handles.GroupByBuildingElementTypes())
                    {
                        string elementTypeName = layerContent.Key;
                        XbimGeometryHandleCollection layerHandles = layerContent.Value;
                        IEnumerable<XbimGeometryData> geomColl = model.GetGeometryData(layerHandles);
                        XbimColour colour = cmap[elementTypeName];
                        XbimMeshLayer<XbimMeshGeometry3D, XbimRenderMaterial> layer = new XbimMeshLayer<XbimMeshGeometry3D, XbimRenderMaterial>(model, colour) { Name = elementTypeName };
                        //add all content initially into the hidden field
                        foreach (var geomData in geomColl)
                        {           
                            if (geomData.IfcTypeId == spaceId)
                                layer.AddToHidden(geomData.TransformBy(composed));
                            else
                                layer.AddToHidden(geomData.TransformBy(composed), model);
                        }

                        if (modelBounds.IsEmpty)
                            modelBounds = layer.BoundingBoxHidden();
                        else
                            modelBounds.Union(layer.BoundingBoxHidden());

                        // add  top level layers
                        layerid = RecursivelyPersistLayer(db, layer, layerid, -1);
                        layerid++;
                    }

                    // create scene row in Scenes tables
                    //
                    if (Logger != null)
                        Logger.DebugFormat("XbimScene: Exporting scene.\r\n", mScalingReference);
                    byte[] boundingBoxFull = modelBounds.ToFloatArray();
                    db.AddScene(1, "MainScene", boundingBoxFull);

                    //now add some meta data about spaces
                    if (
                        (Options & GenerateSceneOption.IncludeSpaces) == GenerateSceneOption.IncludeSpaces
                        ||
                        (Options & GenerateSceneOption.IncludeSpacesBBox) == GenerateSceneOption.IncludeSpacesBBox
                        ||
                        (Options & GenerateSceneOption.IncludeSpacesStoreyId) == GenerateSceneOption.IncludeSpacesStoreyId
                        )
                    {
                        if (Logger != null)
                            Logger.DebugFormat("XbimScene: Exporting spaces.\r\n", mScalingReference);

                        // string data loop
                        foreach (var space in model.Instances.OfType<IfcSpace>())
                        {
                            uint iEntLabel = space.EntityLabel;
                            if ((Options & GenerateSceneOption.IncludeSpaces) == GenerateSceneOption.IncludeSpaces)
                            {
                                db.AddMetaData(
                                    space.GetType().Name,
                                    iEntLabel,
                                    space.Name ?? "Unnamed Space",
                                    iEntLabel.ToString()
                                    );
                            }
                            if ((Options & GenerateSceneOption.IncludeSpacesStoreyId) == GenerateSceneOption.IncludeSpacesStoreyId)
                            {
                                var parent = space.GetContainingStructuralElement();
                                if (parent == null)
                                {
                                    // try with different application behaviours
                                    foreach (var item in space.Decomposes)
                                    {
                                        parent = item.RelatingObject as IfcSpatialStructureElement;
                                        if (parent != null)
                    {
                                            break;
                                        }
                                    }
                                    // parent = space.Decomposes.FirstOrDefault().RelatingObject;
                                    // Decomposes RelatingObject
                                }
                                if (parent != null)
                        {
                                    db.AddMetaData(
                                        "SpaceToStorey",
                                        iEntLabel,
                                        string.Format("StoreyName={0};StoreyLabel={1};", parent.Name, parent.EntityLabel),
                                        iEntLabel.ToString());
                                }
                        }
                    }

                        // binary data loop
                    foreach (var space in model.Instances.OfType<IfcSpace>())
                    {
                           
                            if ((Options & GenerateSceneOption.IncludeSpacesBBox) == GenerateSceneOption.IncludeSpacesBBox)
                            {
                                XbimGeometryData geomdata = model.GetGeometryData(space.EntityLabel, XbimGeometryType.BoundingBox).FirstOrDefault();
                                if (geomdata != null)
                                {
                                    XbimRect3D r3d = XbimRect3D.FromArray(geomdata.ShapeData);
                                    XbimRect3D transformed = r3d.Transform(composed);
                                    db.AddMetaData(
                                            "SpaceBBox",
                                            space.EntityLabel,
                                        // string.Format("Box:{1};", transformed.ToString()), // verbose, but only a few items are expected in the model
                                            transformed.ToFloatArray(),
                                            space.EntityLabel.ToString()
                                            );
                                }
                                // db.AddMetaData(space.GetType().Name, space.Name ?? "Undefined Space", space.EntityLabel.ToString());
                            }
                        }

                    }

                    
                    // Add storey information with elevation.
                    // 
                    IfcBuilding bld = model.IfcProject.GetBuildings().FirstOrDefault();
                    if (bld != null && (Options & GenerateSceneOption.IncludeStoreys) == GenerateSceneOption.IncludeStoreys)
                    {
                        if (Logger != null)
                            Logger.DebugFormat("XbimScene: Exporting storeys.\r\n", mScalingReference);
                        double storeyHeight = 0;//all scenes are in metres
                        int defaultStoreyName = 0;
                        foreach (var storey in bld.GetBuildingStoreys(true))
                        {
                            string cleanName;
                            if(storey.Name.HasValue)
                                cleanName = storey.Name.Value.ToString().Replace(';', ' ');
                            else
                                cleanName = "Floor " + defaultStoreyName++;

                            var spacesCount = storey.SpatialStructuralElementChildren.OfType<IfcSpatialStructureElement>().ToList().Count(); // add for checking spaces


                            // Storey Elevation is optional (and has been found unreliable), therefore
                            // Storey elevation values (in building reference system) are taken from the 
                            // objectplacement through the XbimGeometryType.TransformOnly geometry type
                            //
                            XbimGeometryData geomdata = model.GetGeometryData(storey.EntityLabel, XbimGeometryType.TransformOnly).FirstOrDefault();
                            if (geomdata != null)
                            {
                                storeyHeight = XbimMatrix3D.FromArray(geomdata.DataArray2).OffsetZ;
                                // apply the transformation previously applied to the building 
                                XbimPoint3D InTranslatedReference = composed.Transform(
                                    new XbimPoint3D(0, 0, storeyHeight)
                                    );

                                double InTranslatedReferenceZ = InTranslatedReference.Z; // then express it in meters.
                                
                                // Logger.DebugFormat("StoreyName: {0}; Model Elevation: {1}; Scene Elevation: {2}", cleanName, storeyHeight, InTranslatedReferenceZ);

                                db.AddMetaData(
                                "Storey",
                                    storey.EntityLabel,
                                    string.Format("Name:{0};Elevation:{1};SpaceCount:{2};", cleanName, InTranslatedReferenceZ, spacesCount), // storeyHeight),
                                cleanName);
                        }
                    }
                }
                }
                finally
                {
                    db.Flush();
                    GC.Collect();
                }
            }
        }

        private int RecursivelyPersistLayer(XbimSqliteDB db, XbimMeshLayer<XbimMeshGeometry3D, XbimRenderMaterial> layer, int layerid, int parentLayerId)
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
                vbo = memoryStream.ToArray();


                // bounding box
                XbimRect3D bb = layer.BoundingBoxHidden();
                byte[] bbArray = bb.ToFloatArray();

                db.AddLayer(layer.Name, layerid, parentLayerId, colour, vbo, bbArray);

                int myParent = layerid;
                foreach (var subLayer in layer.SubLayers)
                {
                    layerid++;
                    layerid = RecursivelyPersistLayer(db, subLayer, layerid, myParent);
                }
                return layerid;

            }
            catch (Exception ex)
            {
                throw new XbimException("Error building scene layer", ex);
            }
        }
    }
}
