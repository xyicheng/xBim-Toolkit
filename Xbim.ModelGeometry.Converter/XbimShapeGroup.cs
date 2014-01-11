using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;

namespace Xbim.ModelGeometry.Converter
{
    /// <summary>
    /// A collection of one or more shapes that define a product shape
    /// </summary>
    public class XbimShapeGroup : IEnumerable<XbimShape>
    {
        private XbimModel _model;
        private List<XbimShape> _shapes;

        public XbimShapeGroup(XbimModel model, IEnumerable<int> shapeLabels )
        {
            _model = model;

            //_shapeLabels = shapeLabels;
        }

        public IEnumerator<XbimShape> GetEnumerator()
        {
            return _shapes.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _shapes.GetEnumerator();
        }
    }
}
