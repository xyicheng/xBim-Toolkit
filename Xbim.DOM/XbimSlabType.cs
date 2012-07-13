using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.Extensions;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM
{
    public class XbimSlabType : XbimBuildingElementType
    {
        #region constructors
        //overloaded internal constructors:
        internal XbimSlabType(XbimDocument document, string name) 
            : base(document)
        {
            BaseInit(name);
            _document.SlabTypes.Add(this);
            IfcSlabType.PredefinedType = IfcSlabTypeEnum.NOTDEFINED;
        }

        internal XbimSlabType(XbimDocument document, string name, string description, XbimSlabTypeEnum predefinedType)
            : base(document)
        {
            BaseInit(name);

            _ifcTypeProduct.Description = description;

            EnumConvertor<XbimSlabTypeEnum, IfcSlabTypeEnum> convertor = new EnumConvertor<XbimSlabTypeEnum, IfcSlabTypeEnum>();
            IfcSlabTypeEnum type = convertor.Conversion(predefinedType);
            IfcSlabType.PredefinedType = type;
            
            _document.SlabTypes.Add(this);
        }

        internal XbimSlabType(XbimDocument document, IfcSlabType slabType)
            : base(document)
        {
            _ifcTypeProduct = slabType;
        }

        private void BaseInit(string name)
        {
            _ifcTypeProduct = _document.Model.New<IfcSlabType>();
            _ifcTypeProduct.Name = name;
        }
        #endregion

        #region helpers
        internal IfcSlabType IfcSlabType { get { return IfcTypeProduct as IfcSlabType; } }
        #endregion

        //public NRMSlabTypeQuantities NRMQuantities { get { return new NRMSlabTypeQuantities(this); } }
        public XbimSlabTypeEnum PredefinedType { get { return IfcSlabType.PredefinedType.XbimSlabTypeEnum(); } }
    }
}
