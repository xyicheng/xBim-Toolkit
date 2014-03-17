using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.IO;
using Xbim.IO.GroupingAndStyling;
using Xbim.ModelGeometry.Converter;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStyling
{
    public interface ILayerStylerV2 
    {
        void Init();
        ConcurrentDictionary<XbimTexture, XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>> Layers { get; }
        void NewProduct(XbimProductShape productShape, XbimModel model);
        XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer( XbimModel model, XbimShape shape, XbimProductShape productShape);
    }
}
