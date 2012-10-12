using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM
{
    public class XbimPlateType : XbimBuildingElementType
    {
        #region constructors
        //overloaded internal constructors:
        internal XbimPlateType(XbimDocument document, string name) 
            : base(document)
        {
            BaseInit(name);
            _document.PlateTypes.Add(this);
            IfcPlateType.PredefinedType = IfcPlateTypeEnum.NOTDEFINED;
        }

        internal XbimPlateType(XbimDocument document, string name, string description, XbimPlateTypeEnum plateType)
            : base(document)
        {
            BaseInit(name);

            _ifcTypeProduct.Description = description;
            _document.PlateTypes.Add(this);
            IfcPlateType.PredefinedType = plateType.IfcPlateTypeEnum();
        }

        internal XbimPlateType(XbimDocument document, IfcPlateType slabType)
            : base(document)
        {
            _ifcTypeProduct = slabType;
        }

        private void BaseInit(string name)
        {
            _ifcTypeProduct = _document.Model.Instances.New<IfcPlateType>();
            _ifcTypeProduct.Name = name;
        }
        #endregion

        #region helpers
        internal IfcPlateType IfcPlateType { get { return _ifcTypeProduct as IfcPlateType; } }
        #endregion

        //public NRMPlateTypeQuantities NRMQuantities { get { return new NRMPlateTypeQuantities(this); } }
    }
}
