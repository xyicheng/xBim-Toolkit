using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.Extensions;

namespace Xbim.DOM
{
    public class XbimRampFlight: XbimBuildingElement
    {
                 #region constructors

        internal XbimRampFlight(XbimDocument document, XbimRampFlightType type)
            : base(document)
        {
            BaseInit(type);
        }

        internal XbimRampFlight(XbimDocument document, IfcRampFlight element)
            : base(document)
        {
            _ifcBuildingElement = element;
        }

        private void BaseInit(XbimRampFlightType type)
        {
            _document.RampFlights.Add(this);
            _ifcBuildingElement = _document.Model.Instances.New<IfcRampFlight>();
            _ifcBuildingElement.SetDefiningType(type.IfcTypeProduct, _document.Model);
        }
        #endregion

        public override XbimBuildingElementType ElementType
        {
            get { return IfcTypeObject == null ? null : new XbimRampFlightType(_document, IfcTypeObject as IfcRampFlightType); }
        }

    }
}
