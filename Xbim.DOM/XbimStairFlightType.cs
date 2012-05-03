using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.SharedBldgElements;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM
{
    public class XbimStairFlightType : XbimBuildingElementType
    {
        #region constructors
        //overloaded internal constructors:
        internal XbimStairFlightType(XbimDocument document, string name) 
            : base(document)
        {
            BaseInit(name);
            IfcStairFlightType.PredefinedType = IfcStairFlightTypeEnum.NOTDEFINED;
        }

        internal XbimStairFlightType(XbimDocument document, string name, string description, XbimStairFlightTypeEnum type)
            : base(document)
        {
            BaseInit(name);

            IfcStairFlightType.Description = description;
            IfcStairFlightType.PredefinedType = type.IfcStairFlightTypeEnum();
        }

        internal XbimStairFlightType(XbimDocument document, IfcStairFlightType beamType)
            : base(document)
        {
            _ifcTypeProduct = beamType;
        }

        private void BaseInit(string name)
        {
            IfcStairFlightType = _document.Model.New<IfcStairFlightType>();
            IfcStairFlightType.Name = name;
            _document.StairFlightTypes.Add(this);

        }
        #endregion

        #region helpers
        private IfcStairFlightType IfcStairFlightType
        {
            get { return this._ifcTypeProduct as IfcStairFlightType; }
            set { _ifcTypeProduct = value; }
        }
        #endregion

        //public NRMStairFlightTypeQuantities NRMQuantities { get { return new NRMStairFlightTypeQuantities(this); } }

        public XbimStairFlightTypeEnum PredefinedType { get { return IfcStairFlightType.PredefinedType.XbimStairFlightTypeEnum(); } }
    }
}
