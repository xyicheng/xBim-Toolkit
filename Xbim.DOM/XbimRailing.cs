using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.Extensions;

namespace Xbim.DOM
{
    public class XbimRailing: XbimBuildingElement
    {
         #region constructors

        internal XbimRailing(XbimDocument document, XbimRailingType type)
            : base(document)
        {
            BaseInit(type);
        }

        internal XbimRailing(XbimDocument document, IfcRailing element)
            : base(document)
        {
            _ifcBuildingElement = element;
        }

        private void BaseInit(XbimRailingType type)
        {
            _document.Railings.Add(this);
            _ifcBuildingElement = _document.Model.New<IfcRailing>();
            _ifcBuildingElement.SetDefiningType(type.IfcTypeProduct, _document.Model);
        }
        #endregion

        public override XbimBuildingElementType ElementType
        {
            get { return IfcTypeObject == null ? null : new XbimRailingType(_document, IfcTypeObject as IfcRailingType); }
        }
    }
}
