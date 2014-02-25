using System;
using System.Collections.Concurrent;
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
        private ConcurrentDictionary<XbimTexture, XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>> _Layers;
        public ConcurrentDictionary<XbimTexture, XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>> Layers
        {
            get
            {
                return _Layers;
            }
        }

        public void Init()
        {
            _Layers = new ConcurrentDictionary<XbimTexture, XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>>();
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
            _typeLayer = _Layers.GetOrAdd(texture, new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, texture));
        }

        public XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(ModelGeometry.Converter.XbimShape shape, ModelGeometry.Converter.XbimProductShape productShape)
        {
            XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> shapeLayer;
            //get the style of the shape if it has one and create or find a layer to use
            if (shape.HasStyle)
            {
                IfcSurfaceStyle ifcStyle = _model.Instances[shape.StyleLabel] as IfcSurfaceStyle;
                XbimTexture shapeTexture = new XbimTexture().CreateTexture(ifcStyle);
                shapeLayer = _Layers.GetOrAdd(shapeTexture, new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(_model, shapeTexture));
            }
            else
                shapeLayer = _typeLayer; //use the type layer as default
            //work out all transformations required
            return shapeLayer;
        }


    }
}
