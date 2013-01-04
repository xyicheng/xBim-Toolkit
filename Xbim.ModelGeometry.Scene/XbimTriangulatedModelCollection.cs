using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace Xbim.ModelGeometry.Scene
{
    public class XbimTriangulatedModelCollection : List<XbimTriangulatedModel>
    {
      
        public static readonly XbimTriangulatedModelCollection Empty;
        

        static XbimTriangulatedModelCollection()
        {
            Empty = new XbimTriangulatedModelCollection();    
        }

        public XbimTriangulatedModelCollection():base(1)
        {

        }

        public XbimTriangulatedModelCollection(byte[] triangles, int representationLabel, int surfaceStyleLabel) :base(1)
        {
            this.Add(new XbimTriangulatedModel(triangles,  representationLabel,  surfaceStyleLabel));
        }

       
    }
}
