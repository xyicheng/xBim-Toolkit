using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.XbimExtensions;

namespace Xbim.DOM
{
    public interface IXbimGeometry
    {
        XbimDocument Document {get;}
        IfcGeometricRepresentationItem GetIfcGeometricRepresentation();
    }
}
