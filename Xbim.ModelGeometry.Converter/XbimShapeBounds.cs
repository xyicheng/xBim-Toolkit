using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.IO;

namespace Xbim.ModelGeometry.Converter
{
    [DataContract]
    public class XbimShapeBounds : IXbimShapeBounds
    {
        XbimShapeInstance _instance;
        XbimShapeGeometry _geometry;
        public XbimShapeBounds(XbimShapeInstance instance,  XbimShapeGeometry geometry)
        {
            _instance = instance;
            _geometry = geometry;
        }

        [DataMember(Name="Id")]
        public int InstanceLabel
        {
            get { return _instance.InstanceLabel; }
        }

        [DataMember(Name = "Type")]
        public short IfcTypeId
        {
            get { return _instance.IfcTypeId; }
        }

        [DataMember(Name = "Prod")]
        public int IfcProductLabel
        {
            get { return _instance.IfcProductLabel; }
        }
        /// <summary>
        /// Will retrun the Ifc Style if it has been defined otherwise it will return the negative of the ifc type to allow a default material selection
        /// </summary>
        [DataMember(Name = "Style")]
        public int StyleLabel
        {
            get { return _instance.StyleLabel == 0 ? -_instance.IfcTypeId : _instance.StyleLabel; }
        }

        [DataMember(Name = "Shape")]
        public int ShapeLabel
        {
            get { return _instance.ShapeGeometryLabel; }
        }

        [DataMember(Name = "Trans")]
        public XbimMatrix3D Transformation
        {
            get { return _instance.Transformation; }
        }

        [DataMember(Name = "Box")]
        public XbimRect3D BoundingBox
        {
            get { return _geometry.BoundingBox; }
        }

        [IgnoreDataMember]
        public uint Cost
        {
            get
            {
                return _geometry.Cost;
            }
        }

        [DataMember(Name = "Refs")]
        public uint ReferenceCount
        {
            get { return _geometry.ReferenceCount; }
        }

       
      
    }
}
