﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;

namespace Xbim.DOM
{
    public class XbimRevolvedAreaSolid: XbimSweptAreaSolid
    {
        private IfcRevolvedAreaSolid IfcRevolvedAreaSolid { get { return IfcSweptAreaSolid as IfcRevolvedAreaSolid; } }
        
        internal XbimRevolvedAreaSolid(XbimDocument document, double angle, XbimXYZ spindleDirection, XbimXYZ spindleLocation)
            :base (document)
        {
            BaseInit<IfcRevolvedAreaSolid>();

            IfcRevolvedAreaSolid.Angle = angle;
            IfcRevolvedAreaSolid.Axis = Document.Model.Instances.New<IfcAxis1Placement>
                (ax => {
                    ax.Axis = spindleDirection.CreateIfcDirection(Document);
                    ax.Location = spindleLocation.CreateIfcCartesianPoint(Document);
                });
        }
    }
}
