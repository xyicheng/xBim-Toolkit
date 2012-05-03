using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.MaterialResource;
using Xbim.Ifc.Extensions;

namespace Xbim.DOM
{
    public class XbimBeam: XbimBuildingElement
    {
         #region constructors

        internal XbimBeam(XbimDocument document, XbimBeamType xbimBeamType)
            : base(document)
        {
            BaseInit(xbimBeamType);
        }

        internal XbimBeam(XbimDocument document, IfcBeam beam)
            : base(document)
        {
            _ifcBuildingElement = beam;
        }

        private void BaseInit(XbimBeamType xbimBeamType)
        {
            _document.Beams.Add(this);
            _ifcBuildingElement = _document.Model.New<IfcBeam>();
            _ifcBuildingElement.SetDefiningType(xbimBeamType.IfcTypeProduct, _document.Model);
        }
        #endregion

        public override XbimBuildingElementType ElementType
        {
            get { return IfcTypeObject == null ? null : new XbimBeamType(_document, IfcTypeObject as IfcBeamType); }
        }
    }
}
