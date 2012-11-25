using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Xbim.SceneJSWebViewer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows.Media.Media3D;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class BoundingBox
    {
        public bool IsValid = false;
        public Point3D PointMin = new Point3D();
        public Point3D PointMax = new Point3D();

        public BoundingBox()
        {

        }

        public BoundingBox(Point3D pMin, Point3D pMax)
        {
            PointMin = pMin;
            PointMax = pMax;
            IsValid = true;
        }
        public BoundingBox(double srXmin, double srYmin, double srZmin, double srXmax, double srYmax, double srZmax )
        {
            PointMin = new Point3D(srXmin, srYmin, srZmin);
            PointMin = new Point3D(srXmax, srYmax, srZmax);
            IsValid = true;
        }
        /// <summary>
        /// Returns a bounding box from the byte array
        /// </summary>
        /// <returns></returns>
        public static BoundingBox FromArray(byte[] array)
        {

            MemoryStream ms = new MemoryStream(array);
            BinaryReader bw = new BinaryReader(ms);
            return new BoundingBox(bw.ReadDouble(), bw.ReadDouble(), bw.ReadDouble(), bw.ReadDouble(), bw.ReadDouble(), bw.ReadDouble());

        }
        /// <summary>
        /// Extends to include the specified point; returns true if boundaries were changed.
        /// </summary>
        /// <param name="Point"></param>
        /// <returns></returns>
        internal bool IncludePoint(Point3D Point)
        {
            if (!IsValid)
            {
                PointMin = Point;
                PointMax = Point;
                IsValid = true;
                return true;
            }
            bool ret = false;

            if (PointMin.X > Point.X)
            { PointMin.X = Point.X; ret = true; }
            if (PointMin.Y > Point.Y)
            { PointMin.Y = Point.Y; ret = true; }
            if (PointMin.Z > Point.Z)
            { PointMin.Z = Point.Z; ret = true; }

            if (PointMax.X < Point.X)
            { PointMax.X = Point.X; ret = true; }
            if (PointMax.Y < Point.Y)
            { PointMax.Y = Point.Y; ret = true; }
            if (PointMax.Z < Point.Z)
            { PointMax.Z = Point.Z; ret = true; }

            IsValid = true;

            return ret;
        }

        internal void IncludeBoundingBox(BoundingBox childBB)
        {
            if (!childBB.IsValid)
                return;
            this.IncludePoint(childBB.PointMin);
            this.IncludePoint(childBB.PointMax);
        }
        
        public BoundingBox TransformBy(Matrix3D Matrix)
        {
            return new BoundingBox(Matrix.Transform(PointMin), Matrix.Transform(PointMax));
        }

        private bool IncludePoint(Point3D Point, Matrix3D Matrix)
        {
            Point3D t = Point3D.Multiply(Point, Matrix);
            return IncludePoint(t);
        }
    }
}