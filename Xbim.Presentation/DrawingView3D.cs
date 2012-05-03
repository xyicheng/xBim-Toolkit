#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    DrawingView3D.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Windows.Media.Media3D;

#endregion

namespace Xbim.Presentation
{
    public class DrawingView3D : ModelVisual3D
    {
        private SelectionService3D _selectionService;

        public DrawingView3D()
        {
            _selectionService = new SelectionService3D(this);
        }
    }
}