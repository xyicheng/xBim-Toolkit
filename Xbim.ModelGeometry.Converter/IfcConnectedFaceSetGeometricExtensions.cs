using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.TopologyResource;

namespace Xbim.ModelGeometry.Converter
{
    public static class IfcConnectedFaceSetGeometricExtensions
    {
        /// <summary>
        /// returns a Hash for the geometric behaviour of this object
        /// </summary>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static int GetGeometryHashCode(this IfcConnectedFaceSet cfs)
        {
            int hash = cfs.CfsFaces.Count;
            if (hash > 20) return hash; //probably enought for a uniquish hash
            foreach (var face in cfs.CfsFaces)
                hash ^= face.GetGeometryHashCode();
            return hash;
        }

        /// <summary>
        /// Compares two objects for geometric equality
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b">object to compare with</param>
        /// <returns></returns>
        public static bool GeometricEquals(this IfcConnectedFaceSet a, IfcConnectedFaceSet b)
        {         
            if (a.Equals(b)) return true;
            if(a.CfsFaces.Count!=b.CfsFaces.Count) return false;
            List<IfcFace> aFaces = a.CfsFaces.ToList();
            List<IfcFace> bFaces = b.CfsFaces.ToList();
            for (int i = 0; i < aFaces.Count; i++)
			{
			 if(!(aFaces[i].GeometricEquals(bFaces[i])))
                 return false;
			}
            return true;
        }
    }
}
