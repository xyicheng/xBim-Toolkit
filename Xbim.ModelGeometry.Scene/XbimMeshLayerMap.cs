using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.ModelGeometry.Scene
{
    /// <summary>
    /// A map that holds layers by their int key
    /// Typically the int key is the Entity Label of the IfcSurfaceStyle that the render material is portraying
    /// </summary>
    public class XbimMeshLayerMap<TVISIBLE, TMATERIAL> : Dictionary<int, XbimMeshLayer<TVISIBLE, TMATERIAL>>
        where TVISIBLE : IXbimMeshGeometry3D, new()
        where TMATERIAL : IXbimRenderMaterial, new()
    {
    }
}
