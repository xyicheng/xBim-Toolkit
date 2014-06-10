using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;

namespace Xbim.ModelGeometry.Converter
{
    public abstract class XbimShape
    {
        protected XbimMatrix3D? transform;

        public int GeometryHash;
        protected int styleLabel;

        protected XbimShape(int hashCode)
        {
            GeometryHash = hashCode;
        }
        protected int geometryLabel;

        public abstract byte[] MeshData {get;}
        public abstract string MeshString { get; }
        public XbimMatrix3D? Transform { get { return transform; } }

        public int GeometryLabel
        {
            get
            {
                return geometryLabel;
            }
        }

        public abstract bool HasStyle { get;}
        public abstract int StyleLabel { get; }

        public abstract void WriteToStream(StringWriter sw);
    }
}
