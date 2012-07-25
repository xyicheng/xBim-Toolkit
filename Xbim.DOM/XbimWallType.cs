using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.ExternalReferenceResource;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.Extensions;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.DOM.PropertiesQuantities;
namespace Xbim.DOM
{
    public class XbimWallType : XbimBuildingElementType
    {
        #region constructors
        //overloaded internal constructors:
        internal XbimWallType(XbimDocument document, string name) 
            : base(document)
        {
            BaseInit(name);
            _document.WallTypes.Add(this);
            IfcWallType.PredefinedType = IfcWallTypeEnum.NOTDEFINED;
        }

        internal XbimWallType(XbimDocument document, string name, string description, XbimWallTypeEnum predefinedType)
            : base(document)
        {
            BaseInit(name);

            _ifcTypeProduct.Description = description;
            (_ifcTypeProduct as IfcWallType).PredefinedType = GetIfcWallTypeEnum(predefinedType);
            
            _document.WallTypes.Add(this);
        }

        internal XbimWallType(XbimDocument document, IfcWallType wallType)
            : base(document)
        {
            _ifcTypeProduct = wallType;
        }

        private void BaseInit(string name)
        {
            _ifcTypeProduct = _document.Model.New<IfcWallType>();
            _ifcTypeProduct.Name = name;
        }
        #endregion

        #region helpers
        private IfcWallType IfcWallType { get { return this._ifcTypeProduct as IfcWallType; } }
        private IfcWallTypeEnum GetIfcWallTypeEnum(XbimWallTypeEnum enu)
        {
            switch (enu)
            {
                case XbimWallTypeEnum.STANDARD: return IfcWallTypeEnum.STANDARD;
                case XbimWallTypeEnum.POLYGONAL: return IfcWallTypeEnum.POLYGONAL;
                case XbimWallTypeEnum.SHEAR: return IfcWallTypeEnum.SHEAR;
                case XbimWallTypeEnum.ELEMENTEDWALL: return IfcWallTypeEnum.ELEMENTEDWALL;
                case XbimWallTypeEnum.PLUMBINGWALL: return IfcWallTypeEnum.PLUMBINGWALL;
                case XbimWallTypeEnum.USERDEFINED: return IfcWallTypeEnum.USERDEFINED;
                case XbimWallTypeEnum.NOTDEFINED: return IfcWallTypeEnum.NOTDEFINED;
                default: return IfcWallTypeEnum.NOTDEFINED;
            }
        }
        private XbimWallTypeEnum GetIfcWallTypeEnum(IfcWallTypeEnum enu)
        {
            switch (enu)
            {
                case IfcWallTypeEnum.STANDARD: return XbimWallTypeEnum.STANDARD;
                case IfcWallTypeEnum.POLYGONAL: return XbimWallTypeEnum.POLYGONAL;
                case IfcWallTypeEnum.SHEAR: return XbimWallTypeEnum.SHEAR;
                case IfcWallTypeEnum.ELEMENTEDWALL: return XbimWallTypeEnum.ELEMENTEDWALL;
                case IfcWallTypeEnum.PLUMBINGWALL: return XbimWallTypeEnum.PLUMBINGWALL;
                case IfcWallTypeEnum.USERDEFINED: return XbimWallTypeEnum.USERDEFINED;
                case IfcWallTypeEnum.NOTDEFINED: return XbimWallTypeEnum.NOTDEFINED;
                default: return XbimWallTypeEnum.NOTDEFINED;
            }
        }
        #endregion

        //public NRMWallTypeQuantities NRMQuantities { get { return new NRMWallTypeQuantities(this); } }

        public XbimWallTypeEnum PredefinedType { get { return IfcWallType.PredefinedType.XbimWallTypeEnum(); } }
    }
}
