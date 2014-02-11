﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;

namespace Xbim.ModelGeometry.Converter
{
    public class XbimShapeGeometry : XbimShape
    {
        
        public int RepresentationItemLabel;
        public Type RepresentationItemType;

        public XbimRect3D BoundingBox; 
        private string mesh;
        public int ReferenceCount;


        public XbimShapeGeometry(int hash)
            : base(hash)
        {

        }

        public XbimShapeGeometry(int geometryLabel, 
                            int representationItemLabel,
                            Type representationItemType,
                            XbimRect3D boundingBox,
                            String geometry,
                            int styleLabel,
                            int gemetryHash,
                            int referenceCount,
                            XbimMatrix3D? transform = null)
            : base(gemetryHash)
        {
            this.geometryLabel = geometryLabel;
            RepresentationItemLabel = representationItemLabel;
            RepresentationItemType = representationItemType;
            BoundingBox = boundingBox;
            this.mesh = geometry;
            this.styleLabel = styleLabel;
            ReferenceCount = referenceCount; 
            this.transform = transform;
        }

        public override String Mesh
        {
            get
            {
                return this.mesh;
            }
        }

        public override bool HasStyle
        {
            get { return styleLabel > 0; }
        }

        public override int StyleLabel
        {
            get { return this.styleLabel; }
        }

        public override void WriteToStream(StringWriter sw)
        {
            string str = string.Format("S {0},{1},{2},{3}", this.GeometryLabel, StyleLabel > 0 ? StyleLabel.ToString() : "", ReferenceCount > 0 ? ReferenceCount.ToString() : "", this.Transform.HasValue ? Transform.Value.ToString() : "");
            str = str.TrimEnd(',');
            sw.WriteLine(str);
            sw.Write(mesh);

        }
    }
}
