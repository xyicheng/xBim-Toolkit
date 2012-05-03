#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    SkyBox.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

#endregion

namespace Xbim.Presentation
{
    public class SkyBox : ModelVisual3D
    {
        #region Fields

        private ScaleTransform3D scale;

        #endregion

        #region Dependency Properties

        public double Size
        {
            get { return (double) GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register("Size", typeof (double),
                                        typeof (SkyBox),
                                        new FrameworkPropertyMetadata(OnSizeChanged)
                );

        private static void OnSizeChanged(DependencyObject sender,
                                          DependencyPropertyChangedEventArgs e)
        {
            SkyBox sb = sender as SkyBox;

            double size = sb.Size;

            sb.scale.ScaleX = size;
            sb.scale.ScaleY = size;
            sb.scale.ScaleZ = size;
        }

        #endregion

        #region Constructor

        public SkyBox()
        {
            Model3DGroup sides = new Model3DGroup();

            Point3D[] p = new Point3D[]
                              {
                                  new Point3D(-1, 1, -1),
                                  new Point3D(-1, -1, -1),
                                  new Point3D(1, -1, -1),
                                  new Point3D(1, 1, -1),
                                  new Point3D(1, 1, 1),
                                  new Point3D(1, -1, 1),
                                  new Point3D(-1, -1, 1),
                                  new Point3D(-1, 1, 1)
                              };

            Int32Collection triangleIndices = new Int32Collection(
                new int[] {0, 1, 2, 2, 3, 0});

            PointCollection textCoords = new PointCollection(new Point[]
                                                                 {
                                                                     new Point(0, 0),
                                                                     new Point(0, 1),
                                                                     new Point(1, 1),
                                                                     new Point(1, 0)
                                                                 });

            MeshGeometry3D quad = new MeshGeometry3D();
            quad.Positions.Add(p[0]);
            quad.Positions.Add(p[1]);
            quad.Positions.Add(p[2]);
            quad.Positions.Add(p[3]);
            quad.TriangleIndices = triangleIndices;
            quad.TextureCoordinates = textCoords;
            sides.Children.Add(new GeometryModel3D(quad, GetSideMaterial("north")));

            quad = new MeshGeometry3D();
            quad.Positions.Add(p[4]);
            quad.Positions.Add(p[5]);
            quad.Positions.Add(p[6]);
            quad.Positions.Add(p[7]);
            quad.TriangleIndices = triangleIndices;
            quad.TextureCoordinates = textCoords;
            sides.Children.Add(new GeometryModel3D(quad, GetSideMaterial("south")));

            quad = new MeshGeometry3D();
            quad.Positions.Add(p[1]);
            quad.Positions.Add(p[6]);
            quad.Positions.Add(p[5]);
            quad.Positions.Add(p[2]);
            quad.TriangleIndices = triangleIndices;
            quad.TextureCoordinates = textCoords;
            sides.Children.Add(new GeometryModel3D(quad, GetSideMaterial("down")));

            quad = new MeshGeometry3D();
            quad.Positions.Add(p[7]);
            quad.Positions.Add(p[6]);
            quad.Positions.Add(p[1]);
            quad.Positions.Add(p[0]);
            quad.TriangleIndices = triangleIndices;
            quad.TextureCoordinates = textCoords;
            sides.Children.Add(new GeometryModel3D(quad, GetSideMaterial("west")));

            quad = new MeshGeometry3D();
            quad.Positions.Add(p[3]);
            quad.Positions.Add(p[2]);
            quad.Positions.Add(p[5]);
            quad.Positions.Add(p[4]);
            quad.TriangleIndices = triangleIndices;
            quad.TextureCoordinates = textCoords;
            sides.Children.Add(new GeometryModel3D(quad, GetSideMaterial("east")));

            quad = new MeshGeometry3D();
            quad.Positions.Add(p[7]);
            quad.Positions.Add(p[0]);
            quad.Positions.Add(p[3]);
            quad.Positions.Add(p[4]);
            quad.TriangleIndices = triangleIndices;
            quad.TextureCoordinates = textCoords;
            sides.Children.Add(new GeometryModel3D(quad, GetSideMaterial("up")));


            this.scale = new ScaleTransform3D(1, 1, 1);
            this.Transform = this.scale;
            this.Content = sides;
        }

        private Material GetSideMaterial(string sideFilename)
        {
            ImageBrush ib = new ImageBrush(
                new BitmapImage(
                    new Uri("pack://application:,,,/Xbim.Presentation;component/SkyBoxImages/" + sideFilename + ".jpg",
                            UriKind.Absolute)
                    ));

            ib.ViewportUnits = BrushMappingMode.Absolute;
            ib.TileMode = TileMode.None;
            return new DiffuseMaterial(ib);
        }

        #endregion
    }
}