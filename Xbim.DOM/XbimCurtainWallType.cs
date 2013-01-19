using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM
{
    public class XbimCurtainWallType :XbimBuildingElementType
    {
        #region constructors
        //overloaded internal constructors:
        internal XbimCurtainWallType(XbimDocument document, string name) 
            : base(document)
        {
            IfcCurtainWallType = _document.Model.Instances.New<IfcCurtainWallType>();
            IfcCurtainWallType.Name = name;
            _document.CurtainWallTypes.Add(this);
            IfcCurtainWallType.PredefinedType = IfcCurtainWallTypeEnum.NOTDEFINED;
        }

        internal XbimCurtainWallType(XbimDocument document, IfcCurtainWallType doorStyle)
            : base(document)
        {
            _ifcTypeProduct = doorStyle;
        }
        #endregion

        #region helpers
        private IfcCurtainWallType IfcCurtainWallType
        {
            get { return this._ifcTypeProduct as IfcCurtainWallType; }
            set { _ifcTypeProduct = value; }
        }
        #endregion

        //public NRMCurtainWallTypeQuantities NRMQuantities { get { return new NRMCurtainWallTypeQuantities(this); } }
    }
}
