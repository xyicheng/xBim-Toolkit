using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3;
using Xbim.Ifc2x3.ProductExtension;

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

        public Dictionary<string, XbimGeometryHandleCollection> FilterByBuildingElementTypes()
        {
            Dictionary<string, XbimGeometryHandleCollection> result = new Dictionary<string, XbimGeometryHandleCollection>();
            IfcType baseType = IfcMetaData.IfcType(typeof(IfcBuildingElement));
            foreach (var subType in baseType.NonAbstractSubTypes)
            {
                short ifcTypeId = IfcMetaData.IfcTypeId(subType);
                XbimGeometryHandleCollection handles = new XbimGeometryHandleCollection(this.Where(g=>g.IfcTypeId==ifcTypeId));
                if (handles.Count > 0) result.Add(subType.Name, handles);
            }
            return result;
        }

        /// <summary>
        /// Returns all handles that are not of type to exclude
        /// </summary>
        /// <param name="exclude"></param>
        /// <returns></returns>
        public IEnumerable<XbimGeometryHandle> Exclude(params IfcEntityNameEnum[] exclude)
        {
            HashSet<IfcEntityNameEnum> excludeSet = new HashSet<IfcEntityNameEnum>(exclude);
            foreach (var ex in exclude)
            {
                IfcType ifcType = IfcMetaData.IfcType((short)ex);
                foreach (var sub in ifcType.IfcSubTypes)
                    excludeSet.Add(sub.IfcTypeEnum);
            }

            foreach (var h in this)
                if (!excludeSet.Contains((IfcEntityNameEnum)h.IfcTypeId)) yield return h;
           
        }

        /// <summary>
        /// returns all handles that of of type to include
        /// </summary>
        /// <param name="include"></param>
        /// <returns></returns>
        public IEnumerable<XbimGeometryHandle> Include(params IfcEntityNameEnum[] include)
        {
            HashSet<IfcEntityNameEnum> includeSet = new HashSet<IfcEntityNameEnum>(include);
            foreach (var inc in include)
            {
                IfcType ifcType = IfcMetaData.IfcType((short)inc);
                foreach (var sub in ifcType.IfcSubTypes)
                    includeSet.Add(sub.IfcTypeEnum);
            }
            foreach (var h in this)
                if (includeSet.Contains((IfcEntityNameEnum)h.IfcTypeId)) yield return h;

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
