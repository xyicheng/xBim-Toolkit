using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.SharedBldgElements;
using Xbim.DOM.PropertiesQuantities;
using Xbim.Ifc.ProductExtension;

namespace Xbim.DOM
{
    public class XbimBuildingElementProxyType : XbimBuildingElementType
    {
        #region constructors
        //overloaded internal constructors:
        internal XbimBuildingElementProxyType(XbimDocument document, string name)
            : base(document)
        {
            BaseInit(name);
        }

        internal XbimBuildingElementProxyType(XbimDocument document, IfcBuildingElementProxyType proxyType)
            : base(document)
        {
            IfcBuildingElementProxyType = proxyType;
        }

        private void BaseInit(string name)
        {
            IfcBuildingElementProxyType = _document.Model.New<IfcBuildingElementProxyType>();
            IfcBuildingElementProxyType.Name = name;
            _document.BuildingElementProxyTypes.Add(this);
        }
        #endregion

        #region helpers
        private IfcBuildingElementProxyType IfcBuildingElementProxyType
        {
            get { return _ifcTypeProduct as IfcBuildingElementProxyType; }
            set { _ifcTypeProduct = value; }
        }
        #endregion

        //public NRMColumnTypeQuantities NRMQuantities { get { return new NRMColumnTypeQuantities(this); } }
    }
}
