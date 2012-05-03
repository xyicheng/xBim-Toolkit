#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    DrawingItem3D.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Windows;
using System.Windows.Media.Media3D;
using Xbim.Ifc.Kernel;

#endregion

namespace Xbim.Presentation
{
    public class DrawingItem3D : ModelVisual3D, ISelectable, IGroupable
    {
        #region ISelectable Members

        public bool IsSelected
        {
            get { return (bool) GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected",
                                        typeof (bool),
                                        typeof (DrawingItem3D),
                                        new FrameworkPropertyMetadata(false));

        #endregion

        #region IGroupable Members

        public Guid ID
        {
            get
            {
                IfcRoot root = GetValue(FrameworkElement.TagProperty) as IfcRoot;
                if (root != null)
                    return root.GlobalId;
                else //this drawingItem does not represent anything, shouldn't really happen
                    return Guid.NewGuid();
            }
        }

        public Guid ParentID
        {
            get { return (Guid) GetValue(ParentIDProperty); }
            set { SetValue(ParentIDProperty, value); }
        }

        public static readonly DependencyProperty ParentIDProperty = DependencyProperty.Register("ParentID",
                                                                                                 typeof (Guid),
                                                                                                 typeof (DrawingItem3D));

        public bool IsGroup
        {
            get { return (bool) GetValue(IsGroupProperty); }
            set { SetValue(IsGroupProperty, value); }
        }

        public static readonly DependencyProperty IsGroupProperty = DependencyProperty.Register("IsGroup", typeof (bool),
                                                                                                typeof (DrawingItem3D));

        #endregion
    }
}