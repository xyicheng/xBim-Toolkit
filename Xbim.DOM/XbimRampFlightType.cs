using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM
{
    public class XbimRampFlightType : XbimBuildingElementType
    {
        #region constructors
        //overloaded internal constructors:
        internal XbimRampFlightType(XbimDocument document, string name) 
            : base(document)
        {
            BaseInit(name);
            IfcRampFlightType.PredefinedType = IfcRampFlightTypeEnum.NOTDEFINED;
        }

        internal XbimRampFlightType(XbimDocument document, string name, string description, XbimRampFlightTypeEnum type)
            : base(document)
        {
            BaseInit(name);
            IfcRampFlightType.Description = description;
            IfcRampFlightType.PredefinedType = type.IfcRampFlightTypeEnum();
        }

        internal XbimRampFlightType(XbimDocument document, IfcRampFlightType railingType)
            : base(document)
        {
            IfcRampFlightType = railingType;
        }

        private void BaseInit(string name)
        {
            IfcRampFlightType = _document.Model.Instances.New<IfcRampFlightType>();
            IfcRampFlightType.Name = name;
            _document.RampFlightTypes.Add(this);
        }
        #endregion

        #region helpers
        private IfcRampFlightType IfcRampFlightType
        {
            get { return _ifcTypeProduct as IfcRampFlightType; }
            set { _ifcTypeProduct = value; }
        }
        #endregion

        //public NRMRampFlightTypeQuantities NRMQuantities { get { return new NRMRampFlightTypeQuantities(this); } }

        public XbimRampFlightTypeEnum PredefinedType { get { return IfcRampFlightType.PredefinedType.XbimRampFlightTypeEnum(); } }
    }
}
