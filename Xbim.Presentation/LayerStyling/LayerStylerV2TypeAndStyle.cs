using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.IO;
using Xbim.ModelGeometry.Converter;
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

        public XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(XbimModel model, XbimShape shape, XbimProductShape productShape)
        {
            XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> shapeLayer;
            //get the style of the shape if it has one and create or find a layer to use
            if (shape.HasStyle)
            {
                IfcSurfaceStyle ifcStyle = model.Instances[shape.StyleLabel] as IfcSurfaceStyle;
                XbimTexture shapeTexture = new XbimTexture().CreateTexture(ifcStyle);
                shapeLayer = _Layers.GetOrAdd(shapeTexture, new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(shapeTexture));
            }
            else
            {
                Type productType = productShape.ProductType;
                XbimTexture texture = new XbimTexture().CreateTexture(_colourMap[productType.Name]); //get the colour to use
                shapeLayer = _Layers.GetOrAdd(texture, new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(texture));
            }

            return shapeLayer;
        }




        public void NewProduct(XbimProductShape productShape, XbimModel model)
        {
           //do nothing
        }
    }
}
