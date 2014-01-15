using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Extensions;

namespace Xbim.DOM
{
    public class XbimAxis2Placement3D : Xbim.DOM.IBimAxis2Placement3D
    {
        internal IfcAxis2Placement3D _ifcAxis2Placement;

        public XbimAxis2Placement3D(XbimDocument document)
        {
            _ifcAxis2Placement = document.Model.Instances.New<IfcAxis2Placement3D>();
        }

        public void SetLocation(double X, double Y, double Z)
        {
            _ifcAxis2Placement.SetNewLocation(X, Y, Z);
        }

        public void SetLocation(XbimXYZ location)
        {
            _ifcAxis2Placement.SetNewLocation(location.X, location.Y, location.Z);
        }

        public void SetDirections(XbimXYZ X_axisDirection, XbimXYZ Z_axisDirection)
        {
            _ifcAxis2Placement.SetNewDirectionOf_XZ(X_axisDirection.X, X_axisDirection.Y, X_axisDirection.Z, Z_axisDirection.X, Z_axisDirection.Y, Z_axisDirection.Z);
        }

        public bool IsValid
        {
            get
            {
                bool test1 = _ifcAxis2Placement.Location != null;
                bool test2 = _ifcAxis2Placement.RefDirection != null && _ifcAxis2Placement.Axis != null;
                bool test3 = _ifcAxis2Placement.RefDirection == null && _ifcAxis2Placement.Axis == null;

                return test1 && (test2 || test3);
            }
        }

    }
}
