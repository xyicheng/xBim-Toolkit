using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.SharedBldgElements;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM
{
    public class XbimBeamType : XbimBuildingElementType
    {
        #region constructors
        //overloaded internal constructors:
        internal XbimBeamType(XbimDocument document, string name) 
            : base(document)
        {
            BaseInit(name);
            _document.BeamTypes.Add(this);
            IfcBeamType.PredefinedType = IfcBeamTypeEnum.NOTDEFINED;
        }

        internal XbimBeamType(XbimDocument document, string name, string description, XbimBeamTypeEnum type)
            : base(document)
        {
            BaseInit(name);

            IfcBeamType.Description = description;
            IfcBeamType.PredefinedType = type.IfcBeamTypeEnum();
            
            _document.BeamTypes.Add(this);
        }

        internal XbimBeamType(XbimDocument document, IfcBeamType beamType)
            : base(document)
        {
            _ifcTypeProduct = beamType;
        }

        private void BaseInit(string name)
        {
            IfcBeamType = _document.Model.New<IfcBeamType>();
            IfcBeamType.Name = name;
        }
        #endregion

        #region helpers
        private IfcBeamType IfcBeamType { get { return this._ifcTypeProduct as IfcBeamType; }
            set { _ifcTypeProduct = value; }
        }
        #endregion

        public XbimBeamTypeEnum PredefinedType { get { return IfcBeamType.PredefinedType.XbimBeamTypeEnum(); } }

        //public NRMBeamTypeQuantities NRMQuantities { get { return new NRMBeamTypeQuantities(this); } }
    }
}
