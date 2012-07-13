using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.SelectTypes;
using Xbim.Ifc2x3.Extensions;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM
{
    public class XbimBuildingStorey : XbimSpatialStructureElement
    {
        private IfcBuildingStorey Storey { get { return _spatialElement as IfcBuildingStorey; } }

        //public properties specific for the storey
        public double? Elevation { get { return Storey.Elevation; } set { Storey.Elevation = value; } }
        public XbimBuildingStoreyCommonProperties CommonProperties { get { return new XbimBuildingStoreyCommonProperties(Storey); } }
        public XbimBuildingStoreyQuantities Quantities { get { return new XbimBuildingStoreyQuantities(Storey); } }

        //internal constructor for creation from XbimDocument (parsing data into the document)
        internal XbimBuildingStorey(XbimDocument document, IfcBuildingStorey buildingStorey) : base(document, buildingStorey) { }

        //internal constructor for creation from XbimObjectCreator
        internal XbimBuildingStorey(XbimDocument document, string name, XbimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum)
            : base(document, document.Model.New<IfcBuildingStorey>())
        {
            Storey.Name = name;
            Storey.CompositionType = GeIfcElementCompositionEnum(compositionEnum);
            if (parentElement != null) parentElement.AddToSpatialDecomposition(this);
            document.Storeys.Add(this);
        }


    }

    

    
}
