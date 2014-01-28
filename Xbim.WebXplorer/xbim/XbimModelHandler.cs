using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.SelectTypes;

namespace Xbim.WebXplorer.xbim
{
    public class XbimModelHandler :IDisposable
    {
        struct Header {
            public Int32 ProductLabel;
            public Int32 GeometryLabel;
        };
        const Int32 MAX_LAYER_SIZE = 10000;

        public ConcurrentDictionary<String, List<long>> xLayers = new ConcurrentDictionary<String, List<long>>();
        public ConcurrentDictionary<String, XBimMaterial> xMaterials = new ConcurrentDictionary<String, XBimMaterial>();
        public ConcurrentDictionary<long, XbimModel> xModelMap = new ConcurrentDictionary<long, XbimModel>();
        public ConcurrentDictionary<long, Int32> xProductMap = new ConcurrentDictionary<long, int>();
        public ColourMap TypeMap = new ColourMap();

        public ConcurrentDictionary<string, string> IfcGlobalUnits { get; set; }

        public XbimMatrix3D _modelTranslation;
        public XbimMatrix3D[] _ModelTransform;
        public XbimRect3D[] _ModelBoxes;
        public XbimRect3D _ModelBounds = XbimRect3D.Empty;

        List<Header> Headers = new List<Header>();
        XbimModel model = null;
        public XbimModelHandler(String ModelPath)
        {
            model = new XbimModel();
            model.Open(ModelPath, XbimExtensions.XbimDBAccess.Read);
            //setup a type map
            Init();
        }
        private Int32[] UnLabelifier(long y)
        {
            var retval = new Int32[2];
            retval[0] = (Int32)(y >> 32); //modelid
            retval[1] = (Int32)(y & uint.MaxValue);
            return retval;
        }
        private long Labelifier(Int32 ModelID, Int32 GeometryID)
        {
            //pack modelid and geometrylabel into a long (upper bits are model, lower bits are label)
            long b = ModelID;
            b = b << 32;
            b = b | (uint)GeometryID;
            return b;
        }
        public void Init()
        {
            XbimGeometryHandleCollection ghc;
            _ModelTransform = new XbimMatrix3D[1];
            _ModelBoxes = new XbimRect3D[1];
            ghc = model.GetGeometryHandles(XbimGeometryType.TriangulatedMesh, XbimGeometrySort.OrderByIfcSurfaceStyleThenIfcType);
            SetupModel(ghc, model, 0);
            SetupCamera(model, 0);
            //check if we have a valid bounding box for the model, and throw if we don't
            if (IsRectEmpty(_ModelBounds))
            {
                throw new Exception("Region Undefined");// RegionUndefinedException();
            }

            //move the bounding box to 0,0
            _ModelBounds = _ModelBounds.Transform(_modelTranslation);

            //Set Global Units
            IfcGlobalUnits = new ConcurrentDictionary<string, string>();
            SetGlobalUnits(model);
        }
        private void SetGlobalUnits(XbimModel Model)
        {
            IfcUnitAssignment ifcUnitAssignment = Model.Instances.OfType<IfcUnitAssignment>().FirstOrDefault(); //usually one in Ifc file

            foreach (IfcUnit ifcUnit in ifcUnitAssignment.Units) //loop the UnitSet
            {
                IfcNamedUnit ifcNamedUnit = ifcUnit as IfcNamedUnit;
                if (ifcNamedUnit != null)
                    IfcGlobalUnits.TryAdd(ifcNamedUnit.UnitType.ToString(), ModelPropertyValues.GetUnitAbbreviation(ifcNamedUnit));

                //get the money unit
                if (ifcUnit is IfcMonetaryUnit)
                    IfcGlobalUnits.TryAdd("MonetaryUnit", (ifcUnit as IfcMonetaryUnit).GetSymbol());

                if (ifcUnit is IfcDerivedUnit)
                {
                    IfcDerivedUnit ifcDerivedUnit = (ifcUnit as IfcDerivedUnit);
                    string name = ifcDerivedUnit.UnitType.ToString();
                    string unit = ModelPropertyValues.GetDerivedUnitName(ifcDerivedUnit);
                    IfcGlobalUnits.TryAdd(name, unit);
                }
            }
        }
        private void SetupModel(XbimGeometryHandleCollection ghc, XbimModel model, Int32 ModelID)
        {
            long geo;
            foreach (var xgh in ghc)
            {
                geo = Labelifier(ModelID, xgh.GeometryLabel);
                SetupMaterial(TypeMap, xgh, model, ModelID);
                var layer = xLayers[xgh.SurfaceStyle.IfcSurfaceStyleLabel.ToString()];
                layer.Add(geo);
                xModelMap[geo] = model;
                xProductMap[geo] = xgh.ProductLabel;

            }
        }
        private void SetupNewLayer(XbimSurfaceStyle topstyle, XbimModel model)
        {
            XBimMaterial m = new XBimMaterial();
            XbimTexture style;
            if (topstyle.IsIfcSurfaceStyle)
            {
                style = new XbimTexture().CreateTexture(topstyle.IfcSurfaceStyle(model));
            }
            else
            {
                style = new XbimTexture().CreateTexture(TypeMap[topstyle.IfcType.Name]);
            }
            m.CreateMaterial(style);
            m.Material.MaterialID = topstyle.IfcSurfaceStyleLabel;
            xMaterials[topstyle.IfcSurfaceStyleLabel.ToString()] = m;
            xLayers[topstyle.IfcSurfaceStyleLabel.ToString()] = new List<long>();
        }
        private void SetupMaterial(ColourMap TypeMap, XbimGeometryHandle xgh, XbimModel model, Int32 ModelID)
        {
            if (xMaterials.ContainsKey(xgh.SurfaceStyle.IfcSurfaceStyleLabel.ToString()) && xLayers[xgh.SurfaceStyle.IfcSurfaceStyleLabel.ToString()].Count > MAX_LAYER_SIZE)
            {
                String LayerName = xgh.SurfaceStyle.IfcSurfaceStyleLabel.ToString();
                while (xLayers.ContainsKey(LayerName))
                {
                    LayerName += "_";
                }
                xLayers[LayerName] = xLayers[xgh.SurfaceStyle.IfcSurfaceStyleLabel.ToString()];
                xMaterials[LayerName] = xMaterials[xgh.SurfaceStyle.IfcSurfaceStyleLabel.ToString()];
                SetupNewLayer(xgh.SurfaceStyle, model);
            }
            else
            {
                if (xgh.SurfaceStyleLabel != 0 && !xMaterials.ContainsKey(xgh.SurfaceStyle.IfcSurfaceStyleLabel.ToString()))
                {
                    SetupNewLayer(xgh.SurfaceStyle, model);
                }
            }
        }
        private Boolean IsRectEmpty(XbimRect3D rect)
        {
            var sum = rect.SizeX + rect.SizeY + rect.SizeZ;
            return sum == 0;
        }
        private void SetupCamera(XbimModel model, Int32 ModelID)
        {
            XbimRegion region = GetLargestRegion(model);
            XbimRect3D ModelBox = XbimRect3D.Empty;
            XbimPoint3D p;
            if (region == null) return;

            ModelBox = region.ToXbimRect3D();

            //scale sub-model to 1 metre (so all models are in the same coord space)
            double metreFactor = 1.0 / model.ModelFactors.OneMetre;
            XbimMatrix3D scale = new XbimMatrix3D((Single)metreFactor);
            _ModelTransform[ModelID] = scale;
            ModelBox = ModelBox.Transform(scale);
            _ModelBoxes[ModelID] = ModelBox;

            //update model bounding box to include this sub-model
            if (IsRectEmpty(_ModelBounds))
            {
                _ModelBounds = ModelBox;
            }
            else
            {
                _ModelBounds.Union(ModelBox);
            }

            //Setup the translation of all models to the combined bounding box
            p = _ModelBounds.Centroid();
            _modelTranslation = new XbimMatrix3D(new XbimVector3D(-p.X, -p.Y, -p.Z));
        }
        private XbimRegion GetLargestRegion(XbimModel model)
        {
            IfcProject project = model.IfcProject;
            int projectId = 0;
            if (project != null) projectId = Math.Abs(project.EntityLabel);
            XbimGeometryData regionData = model.GetGeometryData(projectId, XbimGeometryType.Region).FirstOrDefault(); //get the region data should only be one
            if (regionData != null)
            {
                XbimRegionCollection regions = XbimRegionCollection.FromArray(regionData.ShapeData);
                return regions.MostPopulated();
            }
            else
                return null;
        }

        public String RequestModelData(String data)
        {
            return String.Empty;
        }

        public void Dispose()
        {
            model.Close();
            model.Dispose();
        }

        internal List<XbimGeometryData> GetGeometry(string Ids)
        {
            List<XbimGeometryData> data = new List<XbimGeometryData>();
            var labels = Ids.Split(new char[]{','});
            foreach (String i in labels)
            {
                data.Add(model.GetGeometryData(Convert.ToInt32(i)));
            }
            return data;
        }
    }
}