using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.Xplorer
{
    public class GLGeometry
    {
        public GLGeometry()
        {
            Picked = false;
            Highlighted = false;
        }
        public Int64 EntityLabel { get; set; }
        public String Layer { get; set; }
        public List<Single> Positions { get; set; }
        public List<Single> Normals { get; set; }
        public Boolean Picked { get; set; }
        public Boolean Highlighted { get; set; }
        public Int32 StartIndex { get; set; }
    }
}
