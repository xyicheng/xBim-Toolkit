using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.Extensions;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM
{
    public class XbimSlab : XbimBuildingElement
    {
        #region constructors
        internal XbimSlab(XbimDocument document, XbimSlabType xbimSlabType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
            : base(document)
        {
            BaseInit(xbimSlabType);
            SetMaterialLayerSetUsage(MaterialLayersDirection, MaterialLayersDirectionSense, MaterialLayersOffsett);

        }

        internal XbimSlab(XbimDocument document, XbimSlabType xbimSlabType)
            : base(document)
        {
            BaseInit(xbimSlabType);
            _ifcBuildingElement.SetMaterialLayerSetUsage(xbimSlabType.IfcMaterialLayerSet, IfcLayerSetDirectionEnum.AXIS1, IfcDirectionSenseEnum.POSITIVE, 0);

        }


        internal XbimSlab(XbimDocument document, IfcSlab slab)
            : base(document)
        {
            _ifcBuildingElement = slab;
        }

        private void BaseInit(XbimSlabType xbimSlabType)
        {
            _document.Slabs.Add(this);
            _ifcBuildingElement = _document.Model.Instances.New<IfcSlab>();
            _ifcBuildingElement.SetDefiningType(xbimSlabType.IfcTypeProduct, _document.Model);
        }
        #endregion

        internal IfcSlab IfcSlab { get { return IfcBuildingElement as IfcSlab; } }

        public void SetSlabType(XbimSlabTypeEnum slabType)
        {
            EnumConvertor<IfcSlabTypeEnum, XbimSlabTypeEnum> conv = new EnumConvertor<IfcSlabTypeEnum, XbimSlabTypeEnum>();
            IfcSlabTypeEnum type = conv.Conversion(slabType);
            IfcSlab.PredefinedType = type;
        }

        public XbimSlabQuantities Quantities { get { return new XbimSlabQuantities(IfcSlab); } }
        public XbimSlabCommonProperties CommonProperties { get { return new XbimSlabCommonProperties(IfcSlab); } }
        
        public override XbimBuildingElementType ElementType
        {
            get { return IfcTypeObject == null ? null : new XbimSlabType(_document, IfcTypeObject as IfcSlabType); }
        }
    }
}
