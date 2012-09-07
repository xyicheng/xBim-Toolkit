#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    DrawingViewport3D.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.Presentation
{
    public class DrawingViewport3D : Viewport3D
    {
        #region Fields

       

        #endregion

        public DrawingViewport3D()
        {
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            //SkyBox sb = new SkyBox();
            //sb.Size = 10000000; // or whatever you'd like. This sets the bounds of the box.
            //this.Children.Add(sb);
            //SkySphere ball = new SkySphere();

            //ball.ImageSource = "pack://application:,,,/Xbim.Presentation;component/SkyBoxImages/SphericalOvercast.jpg";
            //ball.Scale = 100000;
            
            //this.Children.Add(ball);
        }

       

        internal long? GetProductAt(MouseButtonEventArgs e)
        {
            Point location = e.GetPosition(this);
            HitTestResult result = VisualTreeHelper.HitTest(this, location);
            
            if( result != null &&
                result.VisualHit is ModelVisual3D)
            {
                ModelVisual3D mv3D = (ModelVisual3D) result.VisualHit;
                while (mv3D != null)
                {
                    long? prod = mv3D.GetValue(TagProperty) as long?;
                    if (prod.HasValue) return prod.Value;
                    mv3D = VisualTreeHelper.GetParent(mv3D) as ModelVisual3D; //look up tree to find parent
                }
            }
            return null;
        }

        private object PropertiesProvider(Point location)
        {
            HitTestResult result = VisualTreeHelper.HitTest(this, location);
            object dc = this.DataContext;
            if (result != null &&
                result.VisualHit is ModelVisual3D)
            {
                ModelVisual3D mv3D = (ModelVisual3D) result.VisualHit;
                while (mv3D != null)
                {
                    long? prod = mv3D.GetValue(TagProperty) as long?;
                    if (prod.HasValue)
                        return prod.ToString();
                    mv3D = VisualTreeHelper.GetParent(mv3D) as ModelVisual3D; //look up tree to find parent
                }
            }
            return null;
        }


        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point location = e.GetPosition(this);
            ToolTipController.Move(PropertiesProvider, location);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            ToolTipController.Hide();
        }
    }
}