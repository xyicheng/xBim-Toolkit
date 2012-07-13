using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.Extensions;

namespace Xbim.DOM
{
    public class XbimCurtainWall:XbimBuildingElement
    {
        #region constructors

        internal XbimCurtainWall(XbimDocument document, XbimCurtainWallType type)
            : base(document)
        {
            BaseInit(type);
        }

        internal XbimCurtainWall(XbimDocument document, IfcCurtainWall element)
            : base(document)
        {
            _ifcBuildingElement = element;
        }

        private void BaseInit(XbimCurtainWallType type)
        {
            _document.CurtainWalls.Add(this);
            _ifcBuildingElement = _document.Model.New<IfcCurtainWall>();
            _ifcBuildingElement.SetDefiningType(type.IfcTypeProduct, _document.Model);
        }
        #endregion

        public override XbimBuildingElementType ElementType
        {
            get { return IfcTypeObject == null ? null : new XbimCurtainWallType(_document, IfcTypeObject as IfcCurtainWallType); }
        }
    }
}
