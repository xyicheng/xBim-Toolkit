using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common;

namespace Xbim.ModelGeometry.Scene
{
    public interface IXbimPolyhedron : IXbimGeometryModel
    {
        /// <summary>
        /// Writes the polyhedron to a file in the Stanford PLY format
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="ascii"></param>
        /// <returns></returns>
        bool WritePly(string fileName, bool ascii=true);
    }
}
