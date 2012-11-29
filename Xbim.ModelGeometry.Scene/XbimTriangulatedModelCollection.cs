using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace Xbim.ModelGeometry.Scene
{
    public class XbimTriangulatedModelCollection : List<byte[]>
    {
      
        public static readonly XbimTriangulatedModelCollection Empty;
        

        static XbimTriangulatedModelCollection()
        {
            Empty = new XbimTriangulatedModelCollection(0);    
        }

        public XbimTriangulatedModelCollection()
        {
        }

        public XbimTriangulatedModelCollection(byte[] model) : base(1)
        {
            this.Add(model);
           
        }

        public XbimTriangulatedModelCollection(int c) : base(c)
        {
           
        }

        public XbimTriangulatedModelCollection Add(XbimTriangulatedModelCollection collection)
        {
            foreach (var item in collection)
                this.Add(item);
            return this;
        }
    }
}
