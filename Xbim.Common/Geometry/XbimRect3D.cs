using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Xbim.Common.Geometry
{
    public struct XbimRect3D
    {


        public XbimPoint3D Location;
        public float SizeX ; 
        public float SizeZ ;
        public float SizeY;

        public float X
        {
            get
            {
                return Location.X;
            }
            set
            {
                Location.X = value;
            }
        }
        public float Y
        {
            get
            {
                return Location.Y;
            }
            set
            {
                Location.Y = value;
            }
        }
        public float Z
        {
            get
            {
                return Location.Z;
            }
            set
            {
                Location.Z = value;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return SizeX < 0.0;
            }
        }

        public XbimRect3D(float x, float y, float z, float sizeX, float sizeY, float sizeZ)
        {
            Location = new XbimPoint3D(x, y, z);
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
        }

        public XbimRect3D(XbimPoint3D highpt)
        {
            Location = highpt;
            SizeX = (float)0.0;
            SizeY = (float)0.0;
            SizeZ = (float)0.0;
        }
        /// <summary>
        /// Reinitialises the rectangle 3d from the byte array
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="array">6 doubles, definine, min and max values of the boudning box</param>
        public static XbimRect3D FromArray(byte[] array)
        {
            MemoryStream ms = new MemoryStream(array);
            BinaryReader bw = new BinaryReader(ms);
            XbimRect3D rect = new XbimRect3D();
            float srXmin = (float)bw.ReadDouble(); //legacy when using windows rect3d and doubles
            float srYmin = (float)bw.ReadDouble();
            float srZmin = (float)bw.ReadDouble();
            float srXmax = (float)bw.ReadDouble();
            float srYmax = (float)bw.ReadDouble();
            float srZmax = (float)bw.ReadDouble();
            rect.Location = new XbimPoint3D(srXmin, srYmin, srZmin);
            rect.SizeX = srXmax - srXmin;
            rect.SizeY = srYmax - srYmin;
            rect.SizeZ = srZmax - srZmin;
            return rect;
        }

        static public XbimRect3D Inflate( double x, double y, double z)
        {
            XbimRect3D rect = new XbimRect3D();
            rect.X -= (float)x; rect.Y -= (float)y; rect.Z -= (float)z;
            rect.SizeX += (float)x * 2; rect.SizeY += (float)y * 2; rect.SizeZ += (float)z * 2;
            return rect;
        }

        static public XbimRect3D Inflate(float x, float y, float z)
        {
            XbimRect3D rect = new XbimRect3D();
            rect.X -= x; rect.Y -= y; rect.Z -= z;
            rect.SizeX += x * 2; rect.SizeY += y * 2; rect.SizeZ += z * 2;
            return rect;
        }

        static public XbimRect3D Inflate( double d)
        {
            XbimRect3D rect = new XbimRect3D();
            rect.X -= (float)d; rect.Y -= (float)d; rect.Z -= (float)d;
            rect.SizeX += (float)d * 2; rect.SizeY += (float)d * 2; rect.SizeZ += (float)d * 2;
            return rect;
        }

        static public XbimRect3D Inflate(float d)
        {
            XbimRect3D rect = new XbimRect3D();
            rect.X -= d; rect.Y -= d; rect.Z -= d;
            rect.SizeX += d * 2; rect.SizeY += d * 2; rect.SizeZ += d * 2;
            return rect;
        }


        /// <summary>
        /// Calculates the centre of the 3D rect
        /// </summary>
        /// <param name="rect3D"></param>
        /// <returns></returns>
        public XbimPoint3D Centroid()
        {
            return new XbimPoint3D((X + SizeX / 2), (Y + SizeY / 2), (Z + SizeZ / 2));
        }


        public void TransformBy(XbimMatrix3D matrix3d)
        {
            Location = XbimPoint3D.Multiply(Location, matrix3d);
        }

        public void Union(XbimRect3D bb)
        {
            if (IsEmpty)
            {
                this = bb;
            }
            else if (!bb.IsEmpty)
            {
                float numX = Math.Min(X, bb.X);
                float numY = Math.Min(Y, bb.Y);
                float numZ = Math.Min(Z, bb.Z);
                SizeX = Math.Max((float)(X + SizeX), (float)(bb.X + bb.SizeX)) - numX;
                SizeY = Math.Max((float)(Y + SizeY), (float)(bb.Y + bb.SizeY)) - numY;
                SizeZ = Math.Max((float)(Z + SizeZ), (float)(bb.Z + bb.SizeZ)) - numZ;
                X = numX;
                Y = numY;
                Z = numZ;
            }
        }

        public void Union(XbimPoint3D highpt)
        {
            Union(new XbimRect3D(highpt));
        }
    }
}
