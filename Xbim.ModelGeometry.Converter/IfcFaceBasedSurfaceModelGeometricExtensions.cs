using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.TopologyResource;

namespace Xbim.ModelGeometry.Converter
{

    public static class IfcFaceBasedSurfaceModelGeometricExtensions
    {
       
        /// <summary>
        /// returns a Hash for the geometric behaviour of this object
        /// </summary>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static int GetGeometryHashCode(this  IfcFaceBasedSurfaceModel fbsm)
        {
            int hash = fbsm.FbsmFaces.Count;
            foreach (var cfs in fbsm.FbsmFaces)
            {
                hash ^= cfs.GetGeometryHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Compares two objects for geomtric equality
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b">object to compare with</param>
        /// <returns></returns>
        public static bool GeometricEquals(this  IfcFaceBasedSurfaceModel a, IfcRepresentationItem b)
        {
            IfcFaceBasedSurfaceModel p = b as IfcFaceBasedSurfaceModel;
            if (p == null) return false; //different type
            List<IfcConnectedFaceSet> fsa = a.FbsmFaces.ToList();
            List<IfcConnectedFaceSet> fsb = p.FbsmFaces.ToList();
            if (fsa.Count != fsb.Count) return false;
            for (int i = 0; i < fsa.Count; i++)
            {
                if (!fsa[i].GeometricEquals(fsb[i])) return false;
            }
            return true;

        }
    }
}
