#region MS Permissive Header
//---------------------------------------------------------------------------
//
// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Limited Permissive License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/limitedpermissivelicense.mspx
// All other rights reserved.
//
// This file is part of the 3D Tools for Windows Presentation Foundation
// project.  For more information, see:
// 
// http://CodePlex.com/Wiki/View.aspx?ProjectName=3DTools
//
// The following article discusses the mechanics behind this
// trackball implementation: http://viewport3d.com/trackball.htm
//
// Reading the article is not required to use this sample code,
// but skimming it might be useful.
//
//---------------------------------------------------------------------------
#endregion

#region Directives

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

#endregion

// IAddChild, ContentPropertyAttribute

namespace Xbim.Presentation
{
    public class TrackballDecorator : Viewport3DDecorator
    {
        public TrackballDecorator()
        {
            // the transform that will be applied to the viewport 3d's camera
            _transform = new Transform3DGroup();
            _transform.Children.Add(_scale);
            _transform.Children.Add(_translate);
            _transform.Children.Add(new RotateTransform3D(_rotation));
           
            
            
            this.Loaded += new RoutedEventHandler(TrackballDecorator_Loaded);
        }

        private void TrackballDecorator_Loaded(object sender, RoutedEventArgs e)
        {
            EventSource = new Border();
            EventSource.Background = Brushes.Transparent;
            PreViewportChildren.Add(EventSource);
            DependencyObject child = this;
            while (child != null)
            {
                DependencyObject parent = LogicalTreeHelper.GetParent(child);
                Window mainWin = parent as Window;
                if (mainWin != null)
                {
                    mainWin.KeyDown += new KeyEventHandler(OnKeyDown);
                    return;
                }
                child = parent;
            }
        }

        ///// <summary>
        /////     The FrameworkElement we listen to for mouse events.
        ///// </summary>
        public Border EventSource
        {
            get { return _eventSource; }

            set
            {
                _eventSource = value;
            }
        }

        /// <summary>
        ///   A transform to move the camera or scene to the trackball's
        ///   current orientation and scale.
        /// </summary>
        public Transform3D Transform
        {
            get { return _transform; }
        }

        #region Event Handling

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            _previousPosition2D = e.GetPosition(this);
            _previousPosition3D = ProjectToTrackball(ActualWidth,
                                                     ActualHeight,
                                                     _previousPosition2D);
            if (Mouse.Captured == null)
            {
                Mouse.Capture(this, CaptureMode.Element);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (IsMouseCaptured)
            {
                Mouse.Capture(this, CaptureMode.None);
            }
        }

        //protected override void OnMouseDown(MouseButtonEventArgs e)
        //{
        //    base.OnMouseDown(e);

        //    _previousPosition2D = e.GetPosition(this);
        //    _previousPosition3D = ProjectToTrackball(ActualWidth,
        //                                             ActualHeight,
        //                                             _previousPosition2D);
        //    if (Mouse.Captured == null)
        //    {
        //        Mouse.Capture(this, CaptureMode.Element);
        //    }
        //}

        //protected override void OnMouseUp(MouseButtonEventArgs e)
        //{
        //    base.OnMouseUp(e);

        //    if (IsMouseCaptured)
        //    {
        //        Mouse.Capture(this, CaptureMode.None);
        //    }
        //}

        //protected override void OnMouseMove(MouseEventArgs e)
        //{
        //    base.OnMouseMove(e);

        //    if (IsMouseCaptured)
        //    {
        //        Point currentPosition = e.GetPosition(this);

        //        // avoid any zero axis conditions
        //        if (currentPosition == _previousPosition2D) return;

        //        //camera is going to be moved
        //        _cameraMoved = true;

        //        // Prefer tracking to zooming if both buttons are pressed.
        //        if (e.LeftButton == MouseButtonState.Pressed)
        //        {
        //            Track(currentPosition);
        //        }

        //        _previousPosition2D = currentPosition;

        //        Viewport3D viewport3D = this.Viewport3D;
        //        if (viewport3D != null)
        //        {
        //            if (viewport3D.Camera != null)
        //            {
        //                if (viewport3D.Camera.IsFrozen)
        //                {
        //                    viewport3D.Camera = viewport3D.Camera.Clone();
        //                }

        //                if (viewport3D.Camera.Transform != _transform)
        //                {
        //                    viewport3D.Camera.Transform = _transform;
        //                }
        //            }
        //        }
        //    }
        //}

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (IsMouseCaptured)
            {
                Point currentPosition = e.GetPosition(this);
                // avoid any zero axis conditions
                if (currentPosition == _previousPosition2D) return;

                //camera is going to be moved
                _cameraMoved = true;
                if (e.LeftButton == MouseButtonState.Pressed && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
                {
                    Look(currentPosition);
                    _previousPosition2D = currentPosition;
                    UpdateCamera();
                }
                else if (e.LeftButton == MouseButtonState.Pressed)
                {
                    Pan(currentPosition);
                    _previousPosition2D = currentPosition;
                    UpdateCamera();
                }
                else if (e.RightButton == MouseButtonState.Pressed)
                {
                    Zoom(currentPosition);
                    _previousPosition2D = currentPosition;
                    UpdateCamera();
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down
                || e.Key == Key.PageUp || e.Key == Key.PageDown || e.Key == Key.Home || e.Key == Key.End ||
                e.Key == Key.Escape)
            {
                Viewport3D viewport3D = this.Viewport3D;
                if (viewport3D.Camera != null)
                {

                    PerspectiveCamera pc = viewport3D.Camera as PerspectiveCamera;
                    if (pc != null)
                    {
                        if (viewport3D.Camera.IsFrozen)
                        {
                            viewport3D.Camera = viewport3D.Camera.Clone();
                        }
                        Vector3D cup = _transform.Transform(pc.UpDirection);
                        Vector3D ld = _transform.Transform(pc.LookDirection);
                        AxisAngleRotation3D axa = null;
                        switch (e.Key)
                        {
                            case Key.Home:
                            case Key.Escape:
                                _previousPosition3D = new Vector3D(0, 0, 1);
                                _translate.OffsetX = 0;
                                _translate.OffsetY = 0;
                                _translate.OffsetZ = 0;
                                _scale.ScaleX = 1;
                                _scale.ScaleY = 1;
                                _scale.ScaleZ = 1;
                                _rotation.Axis = new Vector3D(0, 0, 1);
                                _rotation.Angle = 0;
                                if (viewport3D.Camera.Transform != _transform)
                                    viewport3D.Camera.Transform = _transform;
                                break;
                            case Key.Left:
                                PanLeft();
                                //Point newPosition = _previousPosition2D;
                                //newPosition.Offset(-ActualWidth / 20, 0);
                                //Pan(newPosition);
                                break;
                            case Key.Right:
                                axa = new AxisAngleRotation3D(new Vector3D(0, 0, 1), -.5);
                                break;
                            case Key.Up:
                                Vector3D nvu = pc.LookDirection;
                                nvu.Normalize();
                                pc.Position += nvu * _stepFactor;
                                break;
                            case Key.Down:
                                Vector3D nvd = pc.LookDirection;
                                nvd.Normalize();
                                pc.Position -= nvd * _stepFactor;
                                break;
                            case Key.PageUp:
                                TranslateTransform3D trup = new TranslateTransform3D(0, 0, .5);
                                pc.Position = trup.Transform(pc.Position);
                                break;
                            case Key.PageDown:
                                TranslateTransform3D trdown = new TranslateTransform3D(0, 0, -.5);
                                pc.Position = trdown.Transform(pc.Position);
                                break;
                            //case Key.Home:
                            //    RotateTransform3D rtup =
                            //        new RotateTransform3D(
                            //            new AxisAngleRotation3D(
                            //                Vector3D.CrossProduct(pc.LookDirection, pc.UpDirection), .5));
                            //    rtup.CenterX = pc.Position.X;
                            //    rtup.CenterY = pc.Position.Y;
                            //    rtup.CenterZ = pc.Position.Z;
                            //    pc.LookDirection = rtup.Transform(pc.LookDirection);

                            //    pc.UpDirection = rtup.Transform(pc.UpDirection);
                            //    break;
                            case Key.End:
                                RotateTransform3D rtdown =
                                    new RotateTransform3D(
                                        new AxisAngleRotation3D(
                                            Vector3D.CrossProduct(pc.LookDirection, pc.UpDirection), -.5));
                                rtdown.CenterX = pc.Position.X;
                                rtdown.CenterY = pc.Position.Y;
                                rtdown.CenterZ = pc.Position.Z;
                                pc.LookDirection = rtdown.Transform(pc.LookDirection);

                                pc.UpDirection = rtdown.Transform(pc.UpDirection);
                                break;

                            default:
                                break;
                        }
                        if (axa != null)
                        {
                            RotateTransform3D rt = new RotateTransform3D(axa);
                            rt.CenterX = pc.Position.X;
                            rt.CenterY = pc.Position.Y;
                            rt.CenterZ = pc.Position.Z;
                            pc.LookDirection = rt.Transform(pc.LookDirection);
                        }

                        //camera is going to be moved
                        _cameraMoved = true;
                    }
                }
            }
        }

       

        private void PanLeft()
        {
            

            Vector3D changeVector = new Vector3D(ActualWidth/20, 0, 0);

            _translate.OffsetX += changeVector.X * .004;
            _translate.OffsetY -= changeVector.Y * .004;
            _translate.OffsetZ += changeVector.Z * .004;

            
        }
        void UpdateCamera()
        {
            Viewport3D viewport3D = this.Viewport3D;
            if (viewport3D != null)
            {
                if (viewport3D.Camera != null)
                {
                    if (viewport3D.Camera.IsFrozen)
                    {
                        viewport3D.Camera = viewport3D.Camera.Clone();
                    }

                    if (viewport3D.Camera.Transform != _transform)
                    {
                        viewport3D.Camera.Transform = _transform;
                    }
                }
            }
        }
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            //Code to zoom on mose wheel move
            base.OnMouseWheel(e);
            _previousPosition2D = e.GetPosition(this);
            _previousPosition3D = ProjectToTrackball(ActualWidth,
                                                     ActualHeight,
                                                     _previousPosition2D);

            double yDelta = e.Delta;
            double scale = Math.Exp(yDelta/1000); // e^(yDelta/1000) is fairly arbitrary.

            _scale.ScaleX /= scale;
            _scale.ScaleY /= scale;
            _scale.ScaleZ /= scale;
            UpdateCamera();
            //code to change cut plane
            //////Viewport3D viewport3D = this.Viewport3D;
            //////if (viewport3D != null)
            //////{
            //////    if (viewport3D.Camera != null)
            //////    {
            //////        if (viewport3D.Camera.IsFrozen)
            //////        {
            //////            viewport3D.Camera = viewport3D.Camera.Clone();
            //////        }
            //////        PerspectiveCamera pCam = viewport3D.Camera as PerspectiveCamera;
            //////        if (pCam != null)
            //////        {
            //////            double nd = pCam.NearPlaneDistance + (yDelta / 100);
            //////            pCam.NearPlaneDistance = Math.Max(0.125, nd);

            //////        }
            //////        if (viewport3D.Camera.Transform != _transform)
            //////        {
            //////            viewport3D.Camera.Transform = _transform;
            //////        }
            //////    }
            //////}
        }

        #endregion Event Handling

      
        private void Look(Point currentPosition)
        {
            Vector3D currentPosition3D = ProjectToTrackball(ActualWidth, ActualHeight, currentPosition);

            Vector3D axis = Vector3D.CrossProduct(_previousPosition3D, currentPosition3D);
            double angle = Vector3D.AngleBetween(_previousPosition3D, currentPosition3D);

            // Get the viewport's camera.
            Camera vpCamera = this.Viewport3D.Camera;

            // Get the camera's current view matrix.
            Matrix3D viewMatrix = MathUtils.GetViewMatrix(vpCamera);
            viewMatrix.Invert();

            // Transform the trackball rotation axis relative to the camera
            // orientation.
            axis = viewMatrix.Transform(axis);

            // quaterion will throw if this happens - sometimes we can get 3D positions that
            // are very similar, so we avoid the throw by doing this check and just ignoring
            // the event 
            if (axis.Length == 0) return;

            Quaternion delta = new Quaternion(axis, -angle);

            // Get the current orientantion from the RotateTransform3D

            Quaternion q = new Quaternion(_rotation.Axis, _rotation.Angle);

            // Compose the delta with the previous orientation
            q *= delta;

            // Write the new orientation back to the Rotation3D
            _rotation.Axis = q.Axis;
            _rotation.Angle = q.Angle;

            _previousPosition3D = currentPosition3D;
        }

        private void Pan(Point currentPosition)
        {
            Vector3D currentPosition3D = ProjectToTrackball(
                this.ActualWidth, this.ActualHeight, currentPosition);

            Vector change = Point.Subtract(_previousPosition2D, currentPosition);

            Vector3D changeVector = new Vector3D(change.X, change.Y, 0);

            _translate.OffsetX += changeVector.X * .04;
            _translate.OffsetY -= changeVector.Y * .04;
            _translate.OffsetZ += changeVector.Z * .04;

            _previousPosition3D = currentPosition3D;
           
        }

        private Vector3D ProjectToTrackball(double width, double height, Point point)
        {
            double x = point.X / (width / 2);    // Scale so bounds map to [0,0] - [2,2]
            double y = point.Y / (height / 2);

            x = x - 1;                           // Translate 0,0 to the center
            y = 1 - y;                           // Flip so +Y is up instead of down

            double z2 = 1 - x * x - y * y;       // z^2 = 1 - x^2 - y^2
            double z = z2 > 0 ? Math.Sqrt(z2) : 0;

            return new Vector3D(x, y, z);
        }

        private void Zoom(Point currentPosition)
        {
            double yDelta = currentPosition.Y - _previousPosition2D.Y;

            double scale = Math.Exp(yDelta / 100);    // e^(yDelta/100) is fairly arbitrary.

            _scale.ScaleX *= scale;
            _scale.ScaleY *= scale;
            _scale.ScaleZ *= scale;
        }



        /// <summary>
        ///   The amount the camera should be moved per step of the viewer
        /// </summary>
        public double StepFactor
        {
            get { return _stepFactor; }
            set { _stepFactor = value; }
        }

        /// <summary>
        ///   Gets or sets the flag which determines if the camera has been moved. If the user repositions the camera the flag will be set to true
        /// </summary>
        public bool CameraMoved
        {
            get { return _cameraMoved; }
            set { _cameraMoved = value; }
        }

        //--------------------------------------------------------------------
        //
        // Private data
        //
        //--------------------------------------------------------------------

        private Border _eventSource;
        private Point _previousPosition2D;
        private Vector3D _previousPosition3D = new Vector3D(0, 0, 1);

        private Transform3DGroup _transform;
        private ScaleTransform3D _scale = new ScaleTransform3D();
        private AxisAngleRotation3D _rotation = new AxisAngleRotation3D();
        private TranslateTransform3D _translate = new TranslateTransform3D();
        private double _stepFactor;
        private bool _cameraMoved = false;


        
    }
}