#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    GeometryPrimitive3DGroup.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Windows.Media.Media3D;

#endregion

namespace Xbim.XbimExtensions
{
    public class GeometryPrimitive3DGroup : PrimitiveModel3D
    {
        private NonVisualModel3DCollection _children = new NonVisualModel3DCollection();

        public NonVisualModel3DCollection Children
        {
            get { return _children; }
            set { _children = value; }
        }

        /// <summary>
        ///   Creates a visual Model3DGroup and populates with NonVisualModel3DGroup data
        /// </summary>
        public override Model3D ToVisualModel3D()
        {
            Model3DGroup grp3D = new Model3DGroup();
            foreach (PrimitiveModel3D child in _children)
            {
                grp3D.Children.Add(child.ToVisualModel3D());
            }
            return grp3D;
        }

        /// <summary>
        ///   Freezes all children models. nb this is a none visual class and cannot itself be frozen
        /// </summary>
        public override void Freeze()
        {
            foreach (PrimitiveModel3D child in _children)
                child.Freeze();
        }
    }
}