using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.Extensions;
using Xbim.DOM.PropertiesQuantities;
using Xbim.Ifc2x3.GeometricConstraintResource;

namespace Xbim.DOM
{
    public class XbimBuilding : XbimSpatialStructureElement
    {
        private IfcBuilding Building {get {return _spatialElement as IfcBuilding;}}

        //public properties specific for the building
        public double? ElevationOfRefHeight { get { return Building.ElevationOfRefHeight; } set { Building.ElevationOfRefHeight = value; } }
        public double? ElevationOfTerrain { get { return Building.ElevationOfTerrain; } set { Building.ElevationOfTerrain = value; } }
        public XbimBuildingCommonProperties CommonProperties { get { return new XbimBuildingCommonProperties(Building); } }
        public XbimBuildingQuantities Quantities { get { return new XbimBuildingQuantities(Building); } }

        //internal constructor for creation from XbimDocument (parsing data into the document)
        internal XbimBuilding(XbimDocument document, IfcBuilding building): base(document, building) { }

        //internal constructor for creation from XbimObjectCreator
        internal XbimBuilding(XbimDocument document, string name, XbimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum)
            : base(document, document.Model.Instances.New<IfcBuilding>())
        {
            Building.Name = name;
            Building.CompositionType = GeIfcElementCompositionEnum(compositionEnum);
            if (parentElement != null) parentElement.AddToSpatialDecomposition(this);
            if (Document.ModelView == XbimModelView.CoordinationView)
            {
                IfcLocalPlacement lp = Document.Model.Instances.New<IfcLocalPlacement>();
                lp.RelativePlacement = Document.WCS;
                if (parentElement != null)  lp.PlacementRelTo = parentElement.GetObjectPlacement();
                Building.ObjectPlacement = lp;
            }
            Document.Buildings.Add(this);
            
        }
    }

    

    
}
