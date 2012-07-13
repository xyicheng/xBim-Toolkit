using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.ProductExtension;

namespace Xbim.DOM
{
    public class XbimCoveringType : XbimBuildingElementType
    {
         #region constructors
        //overloaded internal constructors:
        internal XbimCoveringType(XbimDocument document, string name) 
            : base(document)
        {
            BaseInit(name);
            _document.CoveringTypes.Add(this);
            IfcCoveringType.PredefinedType = IfcCoveringTypeEnum.NOTDEFINED;
        }

        internal XbimCoveringType(XbimDocument document, string name, string description, XbimCoveringTypeEnum coveringType)
            : base(document)
        {
            BaseInit(name);

            _ifcTypeProduct.Description = description;
            _document.CoveringTypes.Add(this);
            IfcCoveringType.PredefinedType = coveringType.IfcCoveringTypeEnum();
        }

        internal XbimCoveringType(XbimDocument document, IfcCoveringType type)
            : base(document)
        {
            _ifcTypeProduct = type;
        }

        private void BaseInit(string name)
        {
            _ifcTypeProduct = _document.Model.New<IfcCoveringType>();
            _ifcTypeProduct.Name = name;
        }
        #endregion

        #region helpers
        internal IfcCoveringType IfcCoveringType { get { return _ifcTypeProduct as IfcCoveringType; } }
        #endregion
    }
}
