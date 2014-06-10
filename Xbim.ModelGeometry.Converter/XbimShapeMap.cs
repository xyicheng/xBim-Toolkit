using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;

namespace Xbim.ModelGeometry.Converter
{
    /// <summary>
    /// A map of an XbimShapeGeometry
    /// </summary>
    public class XbimShapeMap : XbimShape
    {
        
       
        private XbimShapeGeometry shapeGeometry;
       
        public XbimShapeMap(int geometryLabel, XbimShapeGeometry shape, XbimMatrix3D transform, int stylelabel = 0)
            : base(shape.GeometryHash^transform.GetHashCode())
        {
            throw new NotImplementedException();
            //this.shapeGeometry = shape;
            //if (shape.Transform.HasValue) 
            //    this.transform = shape.Transform.Value * transform;
            //else
            //    this.transform = transform;
            //this.geometryLabel = geometryLabel;
            //this.styleLabel = stylelabel;
        }

        public override string MeshString
        {
            get
            {
                throw new NotImplementedException();
               // return shapeGeometry.MeshString;
            }
        }

        public override byte[] MeshData
        {
            get
            {
                throw new NotImplementedException();
               // return shapeGeometry.MeshData;
            }
        }
        public override bool HasStyle
        {
            get 
            { throw new NotImplementedException(); //return shapeGeometry.HasStyle || this.styleLabel > 0; 
            }
        }

        public override int StyleLabel
        {
            get 
            { throw new NotImplementedException();//if (shapeGeometry.HasStyle) return shapeGeometry.StyleLabel; else return this.styleLabel; 
            }
        }

        public override void WriteToStream(StringWriter sw)
        {
            throw new NotImplementedException();
            //string str = string.Format("R {0},{1},{2}", shapeGeometry.GeometryLabel, StyleLabel > 0 ? StyleLabel.ToString() : "", this.Transform.HasValue ? Transform.Value.ToString() : "");
            //str = str.TrimEnd(',');
            //sw.WriteLine(str);
           
        }
    }
}
 