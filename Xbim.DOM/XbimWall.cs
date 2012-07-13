using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.GeometryResource;
using System.Diagnostics;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SelectTypes;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.QuantityResource;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM
{
    public class XbimWall : XbimBuildingElement
    {
        #region constructors
        /// <summary>
        /// Creates wall with specified parameters of material set usage.
        /// </summary>
        /// <param name="document">DOM document in witch the wall should be created in</param>
        /// <param name="xbimWallType">Wall type of the wall. It must be created befor the wall is going to be created.</param>
        /// <param name="MaterialLayersDirection">Direction of material set (axis)</param>
        /// <param name="MaterialLayersDirectionSense">Sense of direction of material set</param>
        /// <param name="MaterialLayersOffsett">Offset of direction of material set</param>
        internal XbimWall(XbimDocument document, XbimWallType xbimWallType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
            : base(document)
        {
            BaseInit(xbimWallType);
            EnumConvertor<XbimLayerSetDirectionEnum, IfcLayerSetDirectionEnum> conv1 = new EnumConvertor<XbimLayerSetDirectionEnum, IfcLayerSetDirectionEnum>();
            IfcLayerSetDirectionEnum direction = conv1.Conversion(MaterialLayersDirection);

            EnumConvertor<XbimDirectionSenseEnum, IfcDirectionSenseEnum> conv2 = new EnumConvertor<XbimDirectionSenseEnum, IfcDirectionSenseEnum>();
            IfcDirectionSenseEnum sense = conv2.Conversion(MaterialLayersDirectionSense);
            _ifcBuildingElement.SetMaterialLayerSetUsage(xbimWallType.IfcMaterialLayerSet, direction, sense, MaterialLayersOffsett);

        }

        /// <summary>
        /// Creates wall with default parameters of material layer set usage: 
        /// LayerSetDirection = IfcLayerSetDirectionEnum.AXIS1
        /// DirectionSense = IfcDirectionSenseEnum.POSITIVE
        /// OffsetFromReferenceLine = 0
        /// </summary>
        /// <param name="document">DOM document in witch the wall should be created in</param>
        /// <param name="xbimWallType">Wall type of the wall. It must be created befor the wall is going to be created.</param>
        internal XbimWall(XbimDocument document, XbimWallType xbimWallType)
            : base(document)
        {
            BaseInit(xbimWallType);
            _ifcBuildingElement.SetMaterialLayerSetUsage(xbimWallType.IfcMaterialLayerSet, IfcLayerSetDirectionEnum.AXIS2, IfcDirectionSenseEnum.POSITIVE, 0);

        }

        internal XbimWall(XbimDocument document, IfcWall wall)
            : base(document)
        {
            _ifcBuildingElement = wall;
        }

        private void BaseInit(XbimWallType xbimWallType)
        {
            _document.Walls.Add(this);
            _ifcBuildingElement = _document.Model.New<IfcWallStandardCase>();
            _ifcBuildingElement.SetDefiningType(xbimWallType.IfcTypeProduct, _document.Model);
        }
        #endregion

        public XbimWallCommonProperties CommonProperties { get { return new XbimWallCommonProperties(_ifcBuildingElement as IfcWall); } }
        /// <summary>
        /// Quantities of the wall. Default units are mm for length, m2 for areas and m3 for volumes
        /// </summary>
        public XbimWallQuantities Quantities { get { return new XbimWallQuantities(_ifcBuildingElement as IfcWall); } }


        public override XbimBuildingElementType ElementType
        {
            get { return IfcTypeObject == null ? null : new XbimWallType(_document, IfcTypeObject as IfcWallType); }
        }
    }

   

    
}
