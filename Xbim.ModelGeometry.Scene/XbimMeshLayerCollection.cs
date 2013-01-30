using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Xbim.ModelGeometry.Scene
{
    public class XbimMeshLayerCollection<TVISIBLE, TMATERIAL> : KeyedCollection<string, XbimMeshLayer<TVISIBLE, TMATERIAL>> 
        where TVISIBLE : IXbimMeshGeometry3D, new()
        where TMATERIAL : IXbimRenderMaterial, new()
    {

        public XbimMeshLayerCollection()
        {

        }

        protected override string GetKeyForItem(XbimMeshLayer<TVISIBLE, TMATERIAL> item)
        {
            return item.Name;
        }
    }
}
