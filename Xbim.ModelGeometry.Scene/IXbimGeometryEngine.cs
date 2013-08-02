using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.IO;

namespace Xbim.ModelGeometry.Scene
{
    public interface IXbimGeometryEngine
    {
        IXbimGeometryModel GetGeometry3D(IfcProduct product, ConcurrentDictionary<int, object> maps = null);
        IXbimGeometryModel GetGeometry3D(IfcSolidModel solid, ConcurrentDictionary<int, object> maps = null);
        /// <summary>
        /// Initialises the geometry engine and resets any cached data
        /// </summary>
        /// <param name="model"></param>
        void Init(XbimModel model);
    }
}
