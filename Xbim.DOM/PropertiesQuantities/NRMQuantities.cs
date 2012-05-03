using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.Extensions;

namespace Xbim.DOM.PropertiesQuantities
{
    //public class NRMWindowStyleQuantities : XbimQuantities
    //{
    //    internal NRMWindowStyleQuantities(XbimWindowStyle windowStyle) : base(windowStyle.IfcTypeProduct, "NRM") { }

    //    /// <summary>
    //    ///Count of the windows of this type in the model
    //    /// </summary>
    //    public double? Count { get { return GetElementQuantityAsDouble("Count"); } set { SetOrRemoveQuantity("Count", XbimQuantityTypeEnum.COUNT, value); } }
        
    //}

    //public class NRMDoorStyleQuantities : XbimQuantities
    //{
    //    internal NRMDoorStyleQuantities(XbimDoorStyle doorStyle) : base (doorStyle.IfcTypeProduct, "NRM") {}

    //    /// <summary>
    //    ///Count of the doors of this type in the model
    //    /// </summary>
    //    public double? Count { get { return GetElementQuantityAsDouble("Count"); } set { SetOrRemoveQuantity("Count", XbimQuantityTypeEnum.COUNT, value); } }
        
        
    //}

    //public class NRMWallTypeQuantities : XbimQuantities
    //{
    //    internal NRMWallTypeQuantities(XbimWallType wallTYype) : base (wallTYype.IfcTypeProduct, "NRM") { }


    //    /// <summary>
    //    ///Area of the wall
    //    /// </summary>
    //    public double? Area { get { return GetElementQuantityAsDouble("Area"); } set { SetOrRemoveQuantity("Area", XbimQuantityTypeEnum.AREA, value); } }
        
    //}

    //public class NRMSlabTypeQuantities : XbimQuantities
    //{
    //    internal NRMSlabTypeQuantities(XbimSlabType slabType) : base(slabType.IfcTypeProduct, "NRM") { }


    //    /// <summary>
    //    ///Area of the floor
    //    /// </summary>
    //    public double? Area { get { return GetElementQuantityAsDouble("Area"); } set { SetOrRemoveQuantity("Area", XbimQuantityTypeEnum.AREA, value); } }
        
    //}

    //public class NRMPlateTypeQuantities : XbimQuantities
    //{
    //    internal NRMPlateTypeQuantities(XbimPlateType plateType) : base(plateType.IfcTypeProduct, "NRM") { }


    //    /// <summary>
    //    ///
    //    /// </summary>
    //    public double? Area { get { return GetElementQuantityAsDouble("Area"); } set { SetOrRemoveQuantity("Area", XbimQuantityTypeEnum.AREA, value); } }
        
    //}

    //public class NRMRoofQuantities : XbimQuantities
    //{
    //    internal NRMRoofQuantities(XbimRoof roof) : base(roof.IfcBuildingElement, "NRM") { }


    //    /// <summary>
    //    ///Area of the roof
    //    /// </summary>
    //    public double? Area { get { return GetElementQuantityAsDouble("Area"); } set { SetOrRemoveQuantity("Area", XbimQuantityTypeEnum.AREA, value); } }
        
    //}

    //public class NRMCurtainWallTypeQuantities : XbimQuantities
    //{
    //    internal NRMCurtainWallTypeQuantities(XbimCurtainWallType walType) : base(walType.IfcTypeProduct, "NRM") { }

    //    /// <summary>
    //    ///Area of the wall
    //    /// </summary>
    //    public double? Area { get { return GetElementQuantityAsDouble("Area"); } set { SetOrRemoveQuantity("Area", XbimQuantityTypeEnum.AREA, value); } }
    //}

    //public class NRMBeamTypeQuantities : XbimQuantities
    //{
    //    internal NRMBeamTypeQuantities(XbimBeamType beamType) : base(beamType.IfcTypeProduct, "NRM") { }


    //    /// <summary>
    //    ///
    //    /// </summary>
    //    public double? Length { get { return GetElementQuantityAsDouble("Length"); } set { SetOrRemoveQuantity("Length", XbimQuantityTypeEnum.LENGTH, value); } }
        
    //}

    //public class NRMStairFlightTypeQuantities : XbimQuantities
    //{
    //    internal NRMStairFlightTypeQuantities(XbimStairFlightType stairType) : base(stairType.IfcTypeProduct, "NRM") { }


    //    /// <summary>
    //    ///
    //    /// </summary>
    //    public double? Count { get { return GetElementQuantityAsDouble("Count"); } set { SetOrRemoveQuantity("Count", XbimQuantityTypeEnum.COUNT, value); } }
        
    //}

    //public class NRMColumnTypeQuantities :XbimQuantities
    //{
    //    internal NRMColumnTypeQuantities(XbimColumnType columnType) : base(columnType.IfcTypeProduct, "NRM") { }


    //    /// <summary>
    //    ///
    //    /// </summary>
    //    public double? Count { get { return GetElementQuantityAsDouble("Count"); } set { SetOrRemoveQuantity("Count", XbimQuantityTypeEnum.COUNT, value); } }
        
    //}

    //public class NRMRailingTypeQuantities : XbimQuantities
    //{
    //    internal NRMRailingTypeQuantities(XbimRailingType railingType) : base(railingType.IfcTypeProduct, "NRM") { }


    //    /// <summary>
    //    ///
    //    /// </summary>
    //    public double? Length { get { return GetElementQuantityAsDouble("Length"); } set { SetOrRemoveQuantity("Length", XbimQuantityTypeEnum.LENGTH, value); } }
        
    //}

    //public class NRMRampFlightTypeQuantities : XbimQuantities
    //{
    //    internal NRMRampFlightTypeQuantities(XbimRampFlightType rampFlightType) : base(rampFlightType.IfcTypeProduct, "NRM") { }
    //}

    public class NRMQuantities :XbimQuantities, Xbim.DOM.INRMQuantities
    {
        internal NRMQuantities(XbimBuildingElementType elemType) : base(elemType.IfcTypeProduct, "NRM") { }
        internal NRMQuantities(XbimBuildingElement elem) : base(elem.IfcBuildingElement, "NRM") { }


        /// <summary>
        ///
        /// </summary>
        public double? Volume { get { return GetElementQuantityAsDouble("Volume"); } set { SetOrRemoveQuantity("Volume", XbimQuantityTypeEnum.VOLUME, value); } }


        /// <summary>
        ///
        /// </summary>
        public double? Area { get { return GetElementQuantityAsDouble("Area"); } set { SetOrRemoveQuantity("Area", XbimQuantityTypeEnum.AREA, value); } }


        /// <summary>
        ///
        /// </summary>
        public double? Length { get { return GetElementQuantityAsDouble("Length"); } set { SetOrRemoveQuantity("Length", XbimQuantityTypeEnum.LENGTH, value); } }


        /// <summary>
        ///
        /// </summary>
        public double? Count { get { return GetElementQuantityAsDouble("Count"); } set { SetOrRemoveQuantity("Count", XbimQuantityTypeEnum.COUNT, value); } }


        /// <summary>
        ///
        /// </summary>
        public double? Number { get { return GetElementQuantityAsDouble("Number"); } set { SetOrRemoveQuantity("Number", XbimQuantityTypeEnum.COUNT, value); } }
        
    }
}
