using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Xbim.SceneJSWebViewer
{
    internal enum Command
    {
        ModelBasicProperties = 0,
        SharedMaterials = 1,
        Types = 2,
        GeometryHeaders = 3,
        GeometryData = 4,
        Data = 5,
        QueryData = 6
    }
}