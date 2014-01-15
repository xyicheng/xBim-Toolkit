using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.TopologyResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Extensions;

namespace Xbim.DOM
{
    public class XbimExtrudedAreaSolid :XbimSweptAreaSolid
    {
        private IfcExtrudedAreaSolid IfcExtrudedAreaSolid { get { return IfcSweptAreaSolid as IfcExtrudedAreaSolid; } }
     
        internal XbimExtrudedAreaSolid(XbimDocument document,  double depth, XbimXYZ direction)
            :base (document)
        {
            BaseInit<IfcExtrudedAreaSolid>();
            InitToCompositCurveProfile();

            IfcExtrudedAreaSolid.Depth = depth;
            IfcExtrudedAreaSolid.ExtrudedDirection = direction.CreateIfcDirection(Document);
        }

        /// <summary>
        /// Creates solid extrured from rectangle profile
        /// </summary>
        /// <param name="document"></param>
        /// <param name="depth">depth of the extrusion</param>
        /// <param name="width">Width of the rectangle profile</param>
        /// <param name="length">Length of the rectangle profile</param>
        /// <param name="direction">Direction of the extrusion</param>
        internal XbimExtrudedAreaSolid(XbimDocument document, double depth, double width, double length, XbimXYZ direction)
            : base(document)
        {
            BaseInit<IfcExtrudedAreaSolid>();
            InitToRectangleProfile(width, length);

            IfcExtrudedAreaSolid.Depth = depth;
            IfcExtrudedAreaSolid.ExtrudedDirection = direction.CreateIfcDirection(Document);
        }
    }
}
