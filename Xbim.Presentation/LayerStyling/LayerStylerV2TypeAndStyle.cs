using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStyling 
{
    public class LayerStylerV2TypeAndStyle : ILayerStylerV2
    {
        private Dictionary<XbimTexture, XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>> _Layers;
        public Dictionary<XbimTexture, XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>> Layers
        {
            get
            {
                return _Layers;
            }
        }

        public void Init()
        {
            _Layers = new Dictionary<XbimTexture, XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>>();
            _colourMap = new XbimColourMap(StandardColourMaps.IfcProductTypeMap);            
        }

        private XbimColourMap _colourMap;

        XbimModel _model;

        private XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> _typeLayer;

        public void NewProduct(ModelGeometry.Converter.XbimProductShape productShape, XbimModel model)
        {
            _model = model;
            Type productType = productShape.ProductType;
            XbimTexture texture = new XbimTexture().CreateTexture(_colourMap[productType.Name]); //get the colour to use
            
            if (!_Layers.TryGetValue(texture, out _typeLayer))
            {
                _typeLayer = new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, texture);
                _Layers.Add(texture, _typeLayer);
            }
        }

        public XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(ModelGeometry.Converter.XbimShape shape, ModelGeometry.Converter.XbimProductShape productShape)
        {
            XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> shapeLayer;
            //get the style of the shape if it has one and create or find a layer to use
            if (shape.HasStyle)
            {
                IfcSurfaceStyle ifcStyle = _model.Instances[shape.StyleLabel] as IfcSurfaceStyle;
                XbimTexture shapeTexture = new XbimTexture().CreateTexture(ifcStyle);
                if (!_Layers.TryGetValue(shapeTexture, out shapeLayer))
                {
                    shapeLayer = new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(_model, shapeTexture);
                    _Layers.Add(shapeTexture, shapeLayer);
                }
            }
            else
                shapeLayer = _typeLayer; //use the type layer as default
            //work out all transformations required
            return shapeLayer;
        }


    }
}
