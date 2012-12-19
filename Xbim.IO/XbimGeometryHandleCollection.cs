using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.IO
{
    /// <summary>
    /// An ordered Collection of geometry handles
    /// </summary>
    public class XbimGeometryHandleCollection : List<XbimGeometryHandle>
    {
       
        public XbimGeometryHandleCollection(IEnumerable<XbimGeometryHandle> enumerable)
            : base(enumerable)
        {
           
        }

        public XbimGeometryHandleCollection()
            : base()
        {
            // TODO: Complete member initialization
        }
        /// <summary>
        /// Returns a list of unique surface style for this collection
        /// </summary>
        /// <returns></returns>
        public IEnumerable<XbimSurfaceStyle> GetSurfaceStyles()
        {
            HashSet<XbimSurfaceStyle> uniqueStyles = new HashSet<XbimSurfaceStyle>();
            foreach (var item in this)
            {
                uniqueStyles.Add(item.SurfaceStyle);
            }
            return uniqueStyles;
        }

        /// <summary>
        /// Returns all the Geometry Handles for a specified SurfaceStyle
        /// Obtain the SurfaceStyle by calling the GetSurfaceStyles function
        /// </summary>
        /// <param name="forStyle"></param>
        public IEnumerable<XbimGeometryHandle> GetGeometryHandles(XbimSurfaceStyle forStyle)
        {
            foreach (var item in this.Where(gh => gh.SurfaceStyle.Equals(forStyle)))
                yield return item;
        }

        /// <summary>
        /// Returns a map of all the unique surface style and the geometry objects that the style renders
        /// </summary>
        /// <returns></returns>
        public XbimSurfaceStyleMap ToSurfaceStyleMap()
        {
            XbimSurfaceStyleMap result = new XbimSurfaceStyleMap();
            foreach (var style in this.GetSurfaceStyles())
            {
                result.Add(style, new XbimGeometryHandleCollection());
            }
            foreach (var item in this)
            {
                result[item.SurfaceStyle].Add(item);
            }
            return result;
        }
    }
}
