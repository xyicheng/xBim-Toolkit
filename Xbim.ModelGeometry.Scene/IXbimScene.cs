using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.ModelGeometry.Scene
{
    public interface IXbimScene
    {
        void Close();
        bool ReOpen();
        XbimTriangulatedModelStream Triangulate(TransformNode node);
        TransformGraph Graph { get; }
        XbimLOD LOD { get; set; }
    }
}
