using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;

namespace Xbim.ModelGeometry.Converter
{
    public class XbimShape
    {
        public int RepresentationItemLabel;
        public Type RepresentationItemType;
        public XbimRect3D BoundingBox;
        public XbimMatrix3D? Transform;
        public IXbimGeometryModel Geometry;
        public int GeometryHash;
        public int StyleLabel;
        public int ReferenceCount;

        public XbimShape(int representationItemLabel,
                            Type representationItemType,
                            XbimRect3D boundingBox,
                            IXbimGeometryModel geometry,
                            int styleLabel,
                            int gemetryHash,
                            int referenceCount,
                            XbimMatrix3D? transform = null)
        {
            RepresentationItemLabel = representationItemLabel;
            RepresentationItemType = representationItemType;
            BoundingBox = boundingBox;
            GeometryHash = gemetryHash;
            Geometry = geometry;
            StyleLabel = styleLabel;
            ReferenceCount = referenceCount; 
            Transform = transform;
        }


    }
}
