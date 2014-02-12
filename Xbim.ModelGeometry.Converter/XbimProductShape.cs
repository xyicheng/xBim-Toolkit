using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        private int[] _shapeGeomLabels;
        private XbimShapeGroup _shapeGroup;
        private Xbim3DModelContext _context;
        /// <summary>
        /// Retrieves the product for this Shape from the model
        /// </summary>
        public IfcProduct Product 
        {
            get
            {
                return (IfcProduct) Model.Instances[ProductLabel];
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
                    return (IfcMaterialSelect)Model.Instances[MaterialLabel];
                else
                    return null;
            }
        }
        /// <summary>
        /// Returns the shapes that define this product, must be one or more shapes
        /// </summary>
        public XbimShapeGroup Shapes
        {
            get
            {
                if (_shapeGroup == null)
                    _shapeGroup = new XbimShapeGroup(_context, _shapeGeomLabels);
                return _shapeGroup;              
            }
        }

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

        public XbimModel Model
        {
            get
            {
                return _context.Model;
            }
        }

        public Xbim3DModelContext Context
        {
            get { return _context; }
        }

        public XbimProductShape(Xbim3DModelContext context, XbimGeometryData data)
        {
            _context = context;
            
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

        /// <summary>
        /// Write the meta dat for the product shape to a string, does not include full mesh geometry
        /// Use these method for lightweight streaming of product shapes
        /// </summary>
        /// <returns></returns>
        public void WriteMetaData(TextWriter tw)
        {
            string prodLine = string.Format("P {0},{1},{2}", ProductLabel, ProductType.Name, Placement);
            tw.WriteLine(prodLine);
            XbimGeometryCursor geomTable = Model.GetGeometryTable();
            try
            {
                using (var transaction = geomTable.BeginReadOnlyTransaction())
                {
                    string shapeString="M I";
                    foreach (var geomLabel in _shapeGeomLabels)
                    {
                        XbimGeometryData data = geomTable.GetGeometryData(geomLabel);
                        if (data.GeometryType == XbimGeometryType.PolyhedronMap)
                        {
                            string mapString = System.Text.Encoding.ASCII.GetString(data.ShapeData);     
                            tw.WriteLine("M " + mapString); //write the transform matrix
                        }
                        else //it must be a shape
                        {
                            shapeString += ("," + geomLabel);
                        }
                    }
                    if (shapeString.Length > 3) //we have soem shapes
                        tw.WriteLine(shapeString);
                   
                }
            }
            finally
            {
                Model.FreeTable(geomTable);
            }
        }

        public Object GetSimpleMetadataObject()
        {
            var ShapeCollection = new List<Int32>();
            var ShapeMaps = new List<Object>();

            XbimGeometryCursor geomTable = Model.GetGeometryTable();
            try
            {
                using (var transaction = geomTable.BeginReadOnlyTransaction())
                {
                    foreach (var geomLabel in _shapeGeomLabels)
                    {
                        XbimGeometryData data = geomTable.GetGeometryData(geomLabel);
                        if (data.GeometryType == XbimGeometryType.PolyhedronMap)
                        {
                            string mapString = System.Text.Encoding.ASCII.GetString(data.ShapeData);
                            string[] itms = mapString.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            //XbimMatrix3D transform = XbimMatrix3D.FromString(itms[0]);
                            var ShapeMapCollection = new List<Int32>();
                                for (int i = 1; i < itms.Length; i++)
                                {
                                    ShapeMapCollection.Add(Int32.Parse(itms[i]));
                                }
                                ShapeMaps.Add(new {MapID=geomLabel, Transform=itms[0], Items=ShapeMapCollection});
                        }
                        else //it must be a shape
                        {
                            ShapeCollection.Add(geomLabel);
                        }
                    }                   
                }
            }
            finally
            {
                Model.FreeTable(geomTable);
            }

            return new
            {
                ProductLabel = ProductLabel,
                Placement = Placement.ToString(),
                Shapes = ShapeCollection,
                MappedShapes = ShapeMaps
            };
        }
    }
}
