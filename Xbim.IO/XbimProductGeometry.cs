using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;

namespace Xbim.IO
{
    /// <summary>
    /// The  geometries for a given product
    /// </summary>
    public class XbimProductGeometry : List<XbimGeometryData>
    {
        readonly public int ProductLabel;
        public XbimProductGeometry(int productLabel, IEnumerable<XbimGeometryData> geometryData)
            : base(geometryData)
        {
            Debug.Assert(geometryData.Any()); //must be at least one
            ProductLabel = productLabel;
        }
        public XbimGeometryType GeometryType
        {
            get
            {
                if (this.Any())
                    return this[0].GeometryType;
                else
                    return XbimGeometryType.Undefined;
            }
            
        }

        public IEnumerable<XbimGeometryData> Geometry
        {
            get
            {
                return this;
            }
        }


    }
}
