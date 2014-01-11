using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xbim.Common.Exceptions;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.SelectTypes;

namespace Xbim.ModelGeometry.Converter
{
    public class XbimProductShape
    {
        public Type ProductType;
        public int ProductLabel;
        public XbimMatrix3D Placement;
        public XbimRect3D BoundingBox;
       
        public int MaterialLabel;

        private XbimModel _model;
        private int[] _shapeGeomLabels;

        /// <summary>
        /// Retrieves the product for this Shape from the model
        /// </summary>
        public IfcProduct Product 
        {
            get
            {
                return (IfcProduct) _model.Instances[ProductLabel];
            }
        }

        /// <summary>
        /// Retrieves from the model the material this product is made of, returns null if no material is defined
        /// </summary>
        public IfcMaterialSelect Material
        {
            get
            {
                if (HasMaterial)
                    return (IfcMaterialSelect)_model.Instances[MaterialLabel];
                else
                    return null;
            }
        }
        /// <summary>
        /// Returns the shapes that define this product, must be one or more shapes
        /// </summary>
        //public XbimShapeGroup Shapes
        //{
        //    get 
        //    {
        //        XbimGeometryCursor geomTable = _model.GetGeometryTable();
        //        try
        //        {
        //            using (var transaction = geomTable.BeginReadOnlyTransaction())
        //            {

        //                foreach (var geomId in _shapeGeomLabels)
        //                {
        //                    XbimGeometryData data = geomTable.GetGeometryData(geomId);
        //                    if (data.GeometryType == XbimGeometryType.PolyhedronMap)
        //                    {
        //                    }
        //                    else if (data.GeometryType == XbimGeometryType.Polyhedron)
        //                    {
        //                    }
        //                    else
        //                        throw new XbimGeometryException("Unexpected geometry type " + Enum.GetName(typeof(XbimGeometryType), data.GeometryType));
                            
        //                }
        //            }
        //        }
        //        finally
        //        {
        //            _model.FreeTable(geomTable);
        //        }
        //        return Shapes; 
        //    }
        //}

        /// <summary>
        /// True if the product shape has a material definition
        /// </summary>
        public bool HasMaterial
        {
            get
            {
                return MaterialLabel > 0;
            }
        }

        public XbimProductShape(XbimModel model, XbimGeometryData data)
        {
            _model = model;
            ProductLabel = data.IfcProductLabel;
            ProductType = IfcMetaData.GetType(data.IfcTypeId);
            
            string shapeDataString = System.Text.Encoding.ASCII.GetString(data.ShapeData);
            String[] labelStrings = shapeDataString.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            Debug.Assert(labelStrings.Length > 1);
            //The first part is the placement string
            Placement = XbimMatrix3D.FromString(labelStrings[0]);
            _shapeGeomLabels = new int[labelStrings.Length - 1];
            for (int i = 1; i < labelStrings.Length; i++)
            {
                _shapeGeomLabels[i - 1] = Convert.ToInt32(labelStrings[i]);
            }
            
            MaterialLabel = data.StyleLabel;
            string boundsString = System.Text.Encoding.ASCII.GetString(data.DataArray2);
            BoundingBox.FromString(boundsString);
        }

       
    }
}
