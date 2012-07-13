using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM
{
    public class XbimPlate : XbimBuildingElement
    {
         #region constructors
        internal XbimPlate(XbimDocument document, XbimPlateType xbimPlateType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
            : base(document)
        {
            BaseInit(xbimPlateType);
            EnumConvertor<XbimLayerSetDirectionEnum, IfcLayerSetDirectionEnum> conv1 = new EnumConvertor<XbimLayerSetDirectionEnum, IfcLayerSetDirectionEnum>();
            IfcLayerSetDirectionEnum direction = conv1.Conversion(MaterialLayersDirection);

            EnumConvertor<XbimDirectionSenseEnum, IfcDirectionSenseEnum> conv2 = new EnumConvertor<XbimDirectionSenseEnum, IfcDirectionSenseEnum>();
            IfcDirectionSenseEnum sense = conv2.Conversion(MaterialLayersDirectionSense);
            _ifcBuildingElement.SetMaterialLayerSetUsage(xbimPlateType.IfcMaterialLayerSet, direction, sense, MaterialLayersOffsett);

        }

        internal XbimPlate(XbimDocument document, XbimPlateType xbimPlateType)
            : base(document)
        {
            BaseInit(xbimPlateType);
            _ifcBuildingElement.SetMaterialLayerSetUsage(xbimPlateType.IfcMaterialLayerSet, IfcLayerSetDirectionEnum.AXIS1, IfcDirectionSenseEnum.POSITIVE, 0);

        }


        internal XbimPlate(XbimDocument document, IfcPlate plate)
            : base(document)
        {
            _ifcBuildingElement = plate;
        }

        private void BaseInit(XbimPlateType xbimPlateType)
        {
            _document.Plates.Add(this);
            _ifcBuildingElement = _document.Model.New<IfcPlate>();
            _ifcBuildingElement.SetDefiningType(xbimPlateType.IfcTypeProduct, _document.Model);
        }
        #endregion

        internal IfcPlate IfcPlate { get { return IfcBuildingElement as IfcPlate; } }

        public XbimPlateQuantities Quantities { get { return new XbimPlateQuantities(IfcPlate); } }
        public XbimPlateCommonProperties CommonProperties { get { return new XbimPlateCommonProperties(IfcPlate); } }


        public override XbimBuildingElementType ElementType
        {
            get { return IfcTypeObject == null ? null : new XbimPlateType(_document, IfcTypeObject as IfcPlateType); }
        }
    }
}
