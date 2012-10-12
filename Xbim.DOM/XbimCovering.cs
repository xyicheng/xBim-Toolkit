using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.DOM.PropertiesQuantities;
using Xbim.Ifc2x3.ProductExtension;
namespace Xbim.DOM
{
    public class XbimCovering : XbimBuildingElement
    {
         #region constructors
        internal XbimCovering(XbimDocument document, XbimCoveringType xbimType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
            : base(document)
        {
            BaseInit(xbimType);
            EnumConvertor<XbimLayerSetDirectionEnum, IfcLayerSetDirectionEnum> conv1 = new EnumConvertor<XbimLayerSetDirectionEnum, IfcLayerSetDirectionEnum>();
            IfcLayerSetDirectionEnum direction = conv1.Conversion(MaterialLayersDirection);

            EnumConvertor<XbimDirectionSenseEnum, IfcDirectionSenseEnum> conv2 = new EnumConvertor<XbimDirectionSenseEnum, IfcDirectionSenseEnum>();
            IfcDirectionSenseEnum sense = conv2.Conversion(MaterialLayersDirectionSense);
            _ifcBuildingElement.SetMaterialLayerSetUsage(xbimType.IfcMaterialLayerSet, direction, sense, MaterialLayersOffsett);

        }

        internal XbimCovering(XbimDocument document, XbimCoveringType xbimType)
            : base(document)
        {
            BaseInit(xbimType);
            _ifcBuildingElement.SetMaterialLayerSetUsage(xbimType.IfcMaterialLayerSet, IfcLayerSetDirectionEnum.AXIS1, IfcDirectionSenseEnum.POSITIVE, 0);

        }


        internal XbimCovering(XbimDocument document, IfcCovering covering)
            : base(document)
        {
            _ifcBuildingElement = covering;
        }

        private void BaseInit(XbimCoveringType xbimCoveringType)
        {
            _document.Coverings.Add(this);
           
            IfcCovering cov = _document.Model.Instances.New<IfcCovering>();
            _ifcBuildingElement = cov;
            cov.PredefinedType = xbimCoveringType.IfcCoveringType.PredefinedType;
            _ifcBuildingElement.SetDefiningType(xbimCoveringType.IfcTypeProduct, _document.Model);
        }
        #endregion

        internal IfcCovering IfcCovering { get { return IfcBuildingElement as IfcCovering; } }

        //public XbimCoveringQuantities Quantities { get { return new XbimCoveringQuantities(IfcCovering); } }
        //public XbimCoveringCommonProperties CommonProperties { get { return new XbimCoveringCommonProperties(IfcCovering); } }


        public override XbimBuildingElementType ElementType
        {
            get { return IfcTypeObject == null ? null : new XbimCoveringType(_document, IfcTypeObject as IfcCoveringType); }
        }
    }
}
