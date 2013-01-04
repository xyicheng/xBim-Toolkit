using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.Extensions;

namespace Xbim.DOM
{
    public class XbimStairFlight: XbimBuildingElement
    {
         #region constructors

        internal XbimStairFlight(XbimDocument document, XbimStairFlightType type)
            : base(document)
        {
            BaseInit(type);
        }

        internal XbimStairFlight(XbimDocument document, IfcStairFlight element)
            : base(document)
        {
            _ifcBuildingElement = element;
        }

        private void BaseInit(XbimStairFlightType type)
        {
            _document.StairFlights.Add(this);
            _ifcBuildingElement = _document.Model.Instances.New<IfcStairFlight>();
            _ifcBuildingElement.SetDefiningType(type.IfcTypeProduct, _document.Model);
        }
        #endregion

        public override XbimBuildingElementType ElementType
        {
            get { return IfcTypeObject == null ? null : new XbimStairFlightType(_document, IfcTypeObject as IfcStairFlightType); }
        }
    }
}
