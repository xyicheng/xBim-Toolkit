#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    FramedGeometryModel3D.cs
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
    public class FramedGeometryModel3D
    {
        private PrimitiveModel3D _model3D;
        private List<Point3DCollection> _wireFrames = new List<Point3DCollection>();
        private Transform3D _transform;
        private Matrix3D _matrixTransform;

        public Matrix3D MatrixTransform
        {
            get { return _matrixTransform; }
            set { _matrixTransform = value; }
        }


        private List<FramedGeometryModel3D> _children;

        public List<FramedGeometryModel3D> Children
        {
            get
            {
                if (_children == null) _children = new List<FramedGeometryModel3D>();
                return _children;
            }
        }

        public Transform3D Transform
        {
            get { return _transform; }
            set { _transform = value; }
        }

        public PrimitiveModel3D Model3D
        {
            get { return _model3D; }
            set { _model3D = value; }
        }


        public List<Point3DCollection> WireFrames
        {
            get { return _wireFrames; }
            set { _wireFrames = value; }
        }

        public void Freeze()
        {
            if (_transform != null && !_transform.IsFrozen) _transform.Freeze();
            foreach (Point3DCollection wf in _wireFrames)
                if (!wf.IsFrozen) wf.Freeze();
            if (Children != null)
                foreach (FramedGeometryModel3D fg in _children)
                    fg.Freeze();
            if (_model3D != null) _model3D.Freeze();
        }
    }
}