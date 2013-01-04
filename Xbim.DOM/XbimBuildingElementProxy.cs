using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.Extensions;
using Xbim.DOM.PropertiesQuantities;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.DOM
{
    public class XbimBuildingElementProxy : XbimBuildingElement
    {
        #region constructors
        internal XbimBuildingElementProxy(XbimDocument document, XbimBuildingElementProxyType xbimBuildingElementProxyType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
            : base(document)
        {
            BaseInit(xbimBuildingElementProxyType);
            EnumConvertor<XbimLayerSetDirectionEnum, IfcLayerSetDirectionEnum> conv1 = new EnumConvertor<XbimLayerSetDirectionEnum, IfcLayerSetDirectionEnum>();
            IfcLayerSetDirectionEnum direction = conv1.Conversion(MaterialLayersDirection);

            EnumConvertor<XbimDirectionSenseEnum, IfcDirectionSenseEnum> conv2 = new EnumConvertor<XbimDirectionSenseEnum, IfcDirectionSenseEnum>();
            IfcDirectionSenseEnum sense = conv2.Conversion(MaterialLayersDirectionSense);
            _ifcBuildingElement.SetMaterialLayerSetUsage(xbimBuildingElementProxyType.IfcMaterialLayerSet, direction, sense, MaterialLayersOffsett);

        }

        internal XbimBuildingElementProxy(XbimDocument document, XbimBuildingElementProxyType xbimBuildingElementProxyType)
            : base(document)
        {
            if (xbimBuildingElementProxyType != null)
            {
                BaseInit(xbimBuildingElementProxyType);
                _ifcBuildingElement.SetMaterialLayerSetUsage(xbimBuildingElementProxyType.IfcMaterialLayerSet, IfcLayerSetDirectionEnum.AXIS1, IfcDirectionSenseEnum.POSITIVE, 0);
            }
            else
            {
                BaseInit();
            }
        }

        internal XbimBuildingElementProxy(XbimDocument document)
            : base(document)
        {
            BaseInit();
        }

        internal XbimBuildingElementProxy(XbimDocument document, IfcBuildingElementProxy proxyElement)
            : base(document)
        {
            _ifcBuildingElement = proxyElement;
        }

        private void BaseInit(XbimBuildingElementProxyType xbimBuildingElementProxyType)
        {
            BaseInit();
            _ifcBuildingElement.SetDefiningType(xbimBuildingElementProxyType.IfcTypeProduct, _document.Model);
        }
        private void BaseInit()
        {
            _document.BuildingElementProxys.Add(this);
            _ifcBuildingElement = _document.Model.Instances.New<IfcBuildingElementProxy>();
        }

        #endregion

        internal IfcBuildingElementProxy IfcBuildingElementProxy { get { return IfcBuildingElement as IfcBuildingElementProxy; } }

        public override XbimBuildingElementType ElementType
        {
            get { return IfcTypeObject == null ? null : new XbimBuildingElementProxyType(_document, IfcTypeObject as IfcBuildingElementProxyType); }
        }

        
    }
}
