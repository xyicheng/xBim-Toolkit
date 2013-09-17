using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;

namespace Xbim.ModelGeometry.Scene
{
    public class XbimRegion
    {
        public string Name;
        public XbimVector3D Size;
        public XbimPoint3D Centre;
        public int Population = -1;

        public XbimRegion(string name, XbimRect3D bounds, int population)
        {
            // TODO: Complete member initialization
            this.Name = name;
            this.Size = new XbimVector3D(bounds.SizeX,bounds.SizeY,bounds.SizeZ);
            this.Centre = bounds.Centroid();
            this.Population = population;
        }

        public XbimRegion()
        {
           
        }

    }
}
