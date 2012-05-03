#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    PrimitiveModel3D.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic;
using System.Windows.Media.Media3D;

#endregion

namespace Xbim.XbimExtensions
{
    public class NonVisualModel3DCollection : List<PrimitiveModel3D>
    {
    }

    public abstract class PrimitiveModel3D
    {
        /// <summary>
        ///   Creates a visual Model3D, if NonVisualGeometryModel3D a GeometryModel3D is returned, if NonVisualModel3DGroup a Model3DGroup is returned
        /// </summary>
        public abstract Model3D ToVisualModel3D();

        public abstract void Freeze();
    }
}