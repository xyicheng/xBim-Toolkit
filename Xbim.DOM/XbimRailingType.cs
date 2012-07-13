using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM
{
    public class XbimRailingType : XbimBuildingElementType
    {
         #region constructors
        //overloaded internal constructors:
        internal XbimRailingType(XbimDocument document, string name) 
            : base(document)
        {
            BaseInit(name);
            IfcRailingType.PredefinedType = IfcRailingTypeEnum.NOTDEFINED;
        }

        internal XbimRailingType(XbimDocument document, string name, string description, XbimRailingTypeEnum type)
            : base(document)
        {
            BaseInit(name);
            IfcRailingType.Description = description;
            IfcRailingType.PredefinedType = type.IfcRailingTypeEnum();
        }

        internal XbimRailingType(XbimDocument document, IfcRailingType railingType)
            : base(document)
        {
            IfcRailingType = railingType;
        }

        private void BaseInit(string name)
        {
            IfcRailingType = _document.Model.New<IfcRailingType>();
            IfcRailingType.Name = name;
            _document.RailingTypes.Add(this);
        }
        #endregion

        #region helpers
        private IfcRailingType IfcRailingType
        {
            get { return _ifcTypeProduct as IfcRailingType; }
            set { _ifcTypeProduct = value; }
        }
        #endregion

        //public NRMRailingTypeQuantities NRMQuantities { get { return new NRMRailingTypeQuantities(this); } }
        public XbimRailingTypeEnum PredefinedType { get { return IfcRailingType.PredefinedType.XbimRailingTypeEnum(); } }
    }
}
