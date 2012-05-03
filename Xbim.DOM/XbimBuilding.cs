using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.Extensions;
using Xbim.DOM.PropertiesQuantities;
using Xbim.Ifc.GeometricConstraintResource;

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
            : base(document, document.Model.New<IfcBuilding>())
        {
            Building.Name = name;
            Building.CompositionType = GeIfcElementCompositionEnum(compositionEnum);
            if (parentElement != null) parentElement.AddToSpatialDecomposition(this);
            if (Document.ModelView == XbimModelView.CoordinationView)
            {
                IfcLocalPlacement lp = Document.Model.New<IfcLocalPlacement>();
                lp.RelativePlacement = Document.WCS;
                if (parentElement != null)  lp.PlacementRelTo = parentElement.GetObjectPlacement();
                Building.ObjectPlacement = lp;
            }
            Document.Buildings.Add(this);
            
        }
    }

    

    
}
