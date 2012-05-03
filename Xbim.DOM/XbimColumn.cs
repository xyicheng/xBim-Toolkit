using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.MaterialResource;
using Xbim.Ifc.Extensions;


namespace Xbim.DOM
{
    public class XbimColumn: XbimBuildingElement
    {
        #region constructors

        internal XbimColumn(XbimDocument document, XbimColumnType xbimColumnType)
            : base(document)
        {
            BaseInit(xbimColumnType);
        }

        internal XbimColumn(XbimDocument document, IfcColumn column)
            : base(document)
        {
            _ifcBuildingElement = column;
        }

        private void BaseInit(XbimColumnType xbimColumnType)
        {
            _document.Columns.Add(this);
            _ifcBuildingElement = _document.Model.New<IfcColumn>();
            _ifcBuildingElement.SetDefiningType(xbimColumnType.IfcTypeProduct, _document.Model);
        }
        #endregion

        public override XbimBuildingElementType ElementType
        {
            get { return IfcTypeObject == null ? null : new XbimColumnType(_document, IfcTypeObject as IfcColumnType); }
        }
    }
}
