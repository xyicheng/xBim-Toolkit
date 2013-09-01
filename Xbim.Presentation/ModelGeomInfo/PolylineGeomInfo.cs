using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Xbim.Common.Geometry;

namespace Xbim.Presentation.ModelGeomInfo
{
    public class PolylineGeomInfo
    {
        private List<PointGeomInfo> _points;

        public PolylineGeomInfo(List<PointGeomInfo> Init)
        {
            _points = Init;
        }

        public double GetLenght()
        {
            if (_points == null)
                return 0;

            double ret = 0;
            for (int i = 1; i < _points.Count(); i++)
            {
                ret += _points[i - 1].Point.DistanceTo(_points[i].Point);
            }
            return ret;
        }

        public double GetArea()
        {
            // the normal can be taken from the product of two segments on the polyline
            if (Count() < 3)
                return double.NaN;

            XbimVector3D normal = this.Normal() * -1;
            XbimVector3D firstSegment = this.firstSegment();
            XbimVector3D up = XbimVector3D.CrossProduct(normal, firstSegment);
            
            XbimVector3D campos = new XbimVector3D(
                _points[0].Point.X,
                _points[0].Point.Y,
                _points[0].Point.Z
                ); 
            XbimVector3D target = campos + normal;
            XbimMatrix3D m = XbimMatrix3D.CreateLookAt(campos, target, up);


            XbimPoint3D[] point = new XbimPoint3D[Count()];
            for (int i = 0; i < point.Length; i++)
            {
                XbimPoint3D pBefore = new XbimPoint3D(
                    _points[i].Point.X,
                    _points[i].Point.Y,
                    _points[i].Point.Z
                    );
                XbimPoint3D pAft = m.Transform(pBefore);
                point[i] = pAft;
            }

            // http://stackoverflow.com/questions/2553149/area-of-a-irregular-shape
            // it assumes that the last point is NOT the same of the first one, but it tolerates the case.
            double area = 0.0f;
            
            int numVertices = Count();
            for (int i = 0; i < numVertices - 1; ++i)
            {
                area += point[i].X * point[i + 1].Y - point[i + 1].X * point[i].Y;
            }
            area += point[numVertices - 1].X * point[0].Y - point[0].X * point[numVertices - 1].Y;
            area /= 2.0;
            return area;
        }

        private XbimVector3D firstSegment()
        {
            Vector3D ret = _points[1].Point - _points[0].Point;
            return new XbimVector3D(ret.X, ret.Y, ret.Z);
        }

        private XbimVector3D Normal()
        {
            Vector3D seg1 = _points[1].Point - _points[0].Point;
            Vector3D seg2 = _points[2].Point - _points[1].Point;
            var ret = Vector3D.CrossProduct(seg1, seg2);
            ret.Normalize();
            return new XbimVector3D(ret.X, ret.Y, ret.Z);
        }

        internal void SetToVisual(MeshVisual3D Highlighted)
        {
            if (_points == null)
                return;
            var axesMeshBuilder = new MeshBuilder();

            List<Point3D> path = new List<Point3D>();
            foreach (var item in _points)
            {
                axesMeshBuilder.AddSphere(item.Point, 0.1);
                path.Add(item.Point);
            }
            if (_points.Count > 1)
            {
                double LineThickness = 0.05;
                axesMeshBuilder.AddTube(path, LineThickness, 9, false);
            }
            Highlighted.Content = new GeometryModel3D(axesMeshBuilder.ToMesh(), Materials.Yellow);
        }

        internal int Count()
        {
            if (_points == null)
                return 0;
            return _points.Count;
        }
    }
}
