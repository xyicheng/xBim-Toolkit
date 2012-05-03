using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.ProductExtension;

namespace Xbim.DOM.ExportHelpers
{
    public class ExportElementHelper
    {
        private XbimDocumentSource _source;
        private XbimDocumentSource Source
        {
            get { return _source; }
        }
        private ExportPropertiesHelper PropertiesHelper
        {
            get { return Source.PropertiesHelper; }
        }
        private IBimTarget Target { get { return Source.Target; } }
       
        private ExportElementTypeHelper TypeHelper { get { return Source.ElementTypeHelper; } }

        public ExportElementHelper(XbimDocumentSource source)
        {
            _source = source;
        }

        private IBimBuildingElement BaseConvertion(XbimBuildingElement xElement, Func<IBimBuildingElementType, IBimBuildingElement> elemConvFunc, Func<XbimBuildingElementType, IBimBuildingElementType> elemTypeConvFunc)
        {
            XbimBuildingElementType xElementType = xElement.ElementType;

            IBimBuildingElementType tElementType = null;
            if (xElementType != null)
            {
                tElementType = Target.GetBuildingElementType(xElementType.Guid);
                if (tElementType == null) tElementType = elemTypeConvFunc(xElementType);
            }

            IBimBuildingElement tElement = null;
            try
            {
                 tElement = elemConvFunc(tElementType);
            }
            catch (Exception e)
            {
                
                throw new Exception("Error while converting element '" + tElement.Name + "': " + e.Message);
            }
            
            PropertiesHelper.Convert(tElement.Properties, xElement.SingleProperties);

            return tElement;
        }

        public IBimBuildingElement ConvertWall(XbimWall element)
        {
            IBimBuildingElement tElement = BaseConvertion(element, Target.NewWall, TypeHelper.ConvertWallType);
            return tElement;
        }

        public IBimBuildingElement ConvertSlab(XbimSlab element)
        {
            IBimBuildingElement tElement = null;
            switch ((element.ElementType.IfcElementType as IfcSlabType).PredefinedType)
            {
                case IfcSlabTypeEnum.FLOOR: tElement = BaseConvertion(element, Target.NewFloor, TypeHelper.ConvertSlabType);
                    break;
                case IfcSlabTypeEnum.ROOF: tElement = BaseConvertion(element, Target.NewRoof, TypeHelper.ConvertSlabType);
                    break;
                case IfcSlabTypeEnum.LANDING:
                    break;
                case IfcSlabTypeEnum.BASESLAB:
                    break;
                case IfcSlabTypeEnum.USERDEFINED:
                    break;
                case IfcSlabTypeEnum.NOTDEFINED:
                    break;
                default: tElement = BaseConvertion(element, Target.NewSlab, TypeHelper.ConvertSlabType);
                    break;
            }
            return tElement;
        }

        public IBimBuildingElement ConvertCovering(XbimCovering element)
        {
            if ((element.ElementType.IfcElementType as IfcCoveringType).PredefinedType == IfcCoveringTypeEnum.CEILING)
            {
                IBimBuildingElement tElement = BaseConvertion(element, Target.NewCeiling, TypeHelper.ConvertCoveringType);
                return tElement;
            }
            return null; //todo: implement other predefined element types
        }

    }
}
