using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xbim.Common.Exceptions;

namespace Xbim.Common.Geometry
{
    public struct XbimMatrix3D
    {
        #region members

        private static XbimMatrix3D _identity = new XbimMatrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        public Single M11;
        public Single M12;
        public Single M13;
        public Single M14;
        public Single M21;
        public Single M22;
        public Single M23;
        public Single M24;
        public Single M31;
        public Single M32;
        public Single M33;
        public Single M34;
        public Single OffsetX;
        public Single OffsetY;
        public Single OffsetZ;
        public Single M44;
        private const Single FLOAT_EPSILON = 0.000001f;

        #endregion

        public static XbimMatrix3D Identity
        {
            get
            {
                return _identity;
            }
        }

        public bool IsIdentity 
        {
            get
            {
                return XbimMatrix3D.Equal(this, _identity);
            }
        }

        /// <summary>
        /// Creates a new instance of a XbimMatrix3D, initializing it with the given arguments
        /// </summary>
        public XbimMatrix3D(Single m00, Single m01, Single m02, Single m03, Single m10, Single m11, Single m12, Single m13, Single m20, Single m21, Single m22, Single m23, Single m30, Single m31, Single m32, Single m33)
        {    
              
            this.M11 = m00;
            this.M12 = m01;
            this.M13 = m02;
            this.M14 = m03;
            this.M21 = m10;
            this.M22 = m11;
            this.M23 = m12;
            this.M24 = m13;
            this.M31 = m20;
            this.M32 = m21;
            this.M33 = m22;
            this.M34 = m23;
            this.OffsetX = m30;
            this.OffsetY = m31;
            this.OffsetZ = m32;
            this.M44 = m33;
        }
        /// <summary>
        /// Initialises with doubles, there is a possible loss of data as this matrix uses floats internally
        /// </summary>
        /// <param name="m00"></param>
        /// <param name="m01"></param>
        /// <param name="m02"></param>
        /// <param name="m03"></param>
        /// <param name="m10"></param>
        /// <param name="m11"></param>
        /// <param name="m12"></param>
        /// <param name="m13"></param>
        /// <param name="m20"></param>
        /// <param name="m21"></param>
        /// <param name="m22"></param>
        /// <param name="m23"></param>
        /// <param name="m30"></param>
        /// <param name="m31"></param>
        /// <param name="m32"></param>
        /// <param name="m33"></param>
        public XbimMatrix3D(double m00, double m01, double m02, double m03, double m10, double m11, double m12, double m13, double m20, double m21, double m22, double m23, double m30, double m31, double m32, double m33)
        {

            this.M11 = (float)m00;
            this.M12 = (float)m01;
            this.M13 = (float)m02;
            this.M14 = (float)m03;
            this.M21 = (float)m10;
            this.M22 = (float)m11;
            this.M23 = (float)m12;
            this.M24 = (float)m13;
            this.M31 = (float)m20;
            this.M32 = (float)m21;
            this.M33 = (float)m22;
            this.M34 = (float)m23;
            this.OffsetX = (float)m30;
            this.OffsetY = (float)m31;
            this.OffsetZ = (float)m32;
            this.M44 = (float)m33;
        }

        public static XbimMatrix3D FromArray(byte[] array, bool useDouble = true)
        {
            MemoryStream ms = new MemoryStream(array);
            BinaryReader strm = new BinaryReader(ms);
           
            if (useDouble)
                return new XbimMatrix3D(
                (float)strm.ReadDouble(),
                (float)strm.ReadDouble(),
                (float)strm.ReadDouble(),
                (float)strm.ReadDouble(),
                (float)strm.ReadDouble(),
                (float)strm.ReadDouble(),
                (float)strm.ReadDouble(),
                (float)strm.ReadDouble(),
                (float)strm.ReadDouble(),
                (float)strm.ReadDouble(),
                (float)strm.ReadDouble(),
                (float)strm.ReadDouble(),
                (float)strm.ReadDouble(),
                (float)strm.ReadDouble(),
                (float)strm.ReadDouble(),
                (float)strm.ReadDouble()
                );
            else
              return new XbimMatrix3D(
                strm.ReadSingle(),
                strm.ReadSingle(),
                strm.ReadSingle(),
                strm.ReadSingle(),
                strm.ReadSingle(),
                strm.ReadSingle(),
                strm.ReadSingle(),
                strm.ReadSingle(),
                strm.ReadSingle(),
                strm.ReadSingle(),
                strm.ReadSingle(),
                strm.ReadSingle(),
                strm.ReadSingle(),
                strm.ReadSingle(),
                strm.ReadSingle(),
                strm.ReadSingle()
                );
        }

        public XbimPoint3D Transform(XbimPoint3D p)
        {
            return XbimPoint3D.Multiply(p, this);
        }

        #region Operators


        public static XbimMatrix3D operator *(XbimMatrix3D a, XbimMatrix3D b)
        {
            return XbimMatrix3D.Multiply(a, b);
        }
        public override bool Equals(object obj)
        {
            if (obj is XbimMatrix3D )
            {

                return XbimMatrix3D.Equal(this, (XbimMatrix3D)obj);
            }
            else
                return false;
        }
        public override string ToString()
        {
            return this.Str();
        }
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
        #endregion


        #region Functions

        /// <summary>
        /// Performs a matrix multiplication
        /// </summary>
        /// <param name="mat">mat First operand</param>
        /// <param name="mat2">mat2 Second operand</param>
        /// <returns>dest if specified, mat otherwise</returns>
        public static XbimMatrix3D Multiply(XbimMatrix3D mat, XbimMatrix3D mat2)
        {
            XbimMatrix3D dest = new XbimMatrix3D();

            // Cache the matrix values (makes for huge speed increases!)
            var a00 = mat.M11;
            var a01 = mat.M12;
            var a02 = mat.M13;
            var a03 = mat.M14;
            var a10 = mat.M21;
            var a11 = mat.M22;
            var a12 = mat.M23;
            var a13 = mat.M24;
            var a20 = mat.M31;
            var a21 = mat.M32;
            var a22 = mat.M33;
            var a23 = mat.M34;
            var a30 = mat.OffsetX;
            var a31 = mat.OffsetY;
            var a32 = mat.OffsetZ;
            var a33 = mat.M44;

            // Cache only the current line of the second matrix
            var b0 = mat2.M11;
            var b1 = mat2.M12;
            var b2 = mat2.M13;
            var b3 = mat2.M14;
            dest.M11 = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
            dest.M12 = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
            dest.M13 = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
            dest.M14 = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

            b0 = mat2.M21;
            b1 = mat2.M22;
            b2 = mat2.M23;
            b3 = mat2.M24;
            dest.M21 = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
            dest.M22 = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
            dest.M23 = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
            dest.M24 = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

            b0 = mat2.M31;
            b1 = mat2.M32;
            b2 = mat2.M33;
            b3 = mat2.M34;
            dest.M31 = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
            dest.M32 = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
            dest.M33 = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
            dest.M34 = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

            b0 = mat2.OffsetX;
            b1 = mat2.OffsetY;
            b2 = mat2.OffsetZ;
            b3 = mat2.M44;
            dest.OffsetX = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
            dest.OffsetY = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
            dest.OffsetZ = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
            dest.M44 = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

            return dest;
        }
        /// <summary>
        /// Compares two matrices for equality within a certain margin of error
        /// </summary>
        /// <param name="a">a First matrix</param>
        /// <param name="b">b Second matrix</param>
        /// <returns>True if a is equivalent to b</returns>
        public static bool Equal(XbimMatrix3D a, XbimMatrix3D b)
        {
            return   System.Object.ReferenceEquals(a, b) || (
                Math.Abs(a.M11 - b.M11) < FLOAT_EPSILON &&
                Math.Abs(a.M12 - b.M12) < FLOAT_EPSILON &&
                Math.Abs(a.M13 - b.M13) < FLOAT_EPSILON &&
                Math.Abs(a.M14 - b.M14) < FLOAT_EPSILON &&
                Math.Abs(a.M21 - b.M21) < FLOAT_EPSILON &&
                Math.Abs(a.M22 - b.M22) < FLOAT_EPSILON &&
                Math.Abs(a.M23 - b.M23) < FLOAT_EPSILON &&
                Math.Abs(a.M24 - b.M34) < FLOAT_EPSILON &&
                Math.Abs(a.M31 - b.M31) < FLOAT_EPSILON &&
                Math.Abs(a.M32 - b.M32) < FLOAT_EPSILON &&
                Math.Abs(a.M33 - b.M33) < FLOAT_EPSILON &&
                Math.Abs(a.M34 - b.M34) < FLOAT_EPSILON &&
                Math.Abs(a.OffsetX - b.OffsetX) < FLOAT_EPSILON &&
                Math.Abs(a.OffsetY - b.OffsetY) < FLOAT_EPSILON &&
                Math.Abs(a.OffsetZ - b.OffsetZ) < FLOAT_EPSILON &&
                Math.Abs(a.M44 - b.M44) < FLOAT_EPSILON
            );
        }

        /// <summary>
        /// Creates a new instance of a mat4
        /// </summary>
        /// <param name="mat">Single[16] containing values to initialize with</param>
        /// <returns>New mat4New mat4</returns>
        public static XbimMatrix3D Copy(XbimMatrix3D m)
        {
            return new XbimMatrix3D(m.M11 , m.M12 , m.M13, m.M14 ,
                  m.M21 , m.M22 , m.M23, m.M24 ,
                  m.M31 , m.M32 , m.M33, m.M34 ,
                  m.OffsetX , m.OffsetY , m.OffsetZ , m.M44);
            
            
        }

        /// <summary>
        /// Returns a string representation of a mat4
        /// </summary>
        /// <param name="mat">mat mat4 to represent as a string</param>
        /// <returns>String representation of mat</returns>
        public  string Str()
        {
            return "[" + M11 + ", " + M12 + ", " + M13 + ", " + M14 +
                  ", " + M21 + ", " + M22 + ", " + M23 + ", " + M24 +
                  ", " + M31 + ", " + M32 + ", " + M33 + ", " + M34 +
                  ", " + OffsetX + ", " + OffsetY + ", " + OffsetZ + ", " + M44 + "]";
        }
        #endregion




        public XbimVector3D Transform(XbimVector3D xbimVector3D)
        {
            return XbimVector3D.Multiply(xbimVector3D, this);
        }

        public void Invert()
        {
            
            // Cache the matrix values (makes for huge speed increases!)
            Single a00 = M11, a01 = M12, a02 = M13, a03 = M14,
                a10 = M21, a11 = M22, a12 = M23, a13 = M24,
                a20 = M31, a21 = M32, a22 = M33, a23 = M34,
                a30 = OffsetX, a31 = OffsetY, a32 = OffsetZ, a33 = M44,

                b00 = a00 * a11 - a01 * a10,
                b01 = a00 * a12 - a02 * a10,
                b02 = a00 * a13 - a03 * a10,
                b03 = a01 * a12 - a02 * a11,
                b04 = a01 * a13 - a03 * a11,
                b05 = a02 * a13 - a03 * a12,
                b06 = a20 * a31 - a21 * a30,
                b07 = a20 * a32 - a22 * a30,
                b08 = a20 * a33 - a23 * a30,
                b09 = a21 * a32 - a22 * a31,
                b10 = a21 * a33 - a23 * a31,
                b11 = a22 * a33 - a23 * a32,
                d = (b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06),
                invDet;

            // Calculate the determinant
            if (d == 0) { throw new XbimException("Matrix does not have an inverse"); }
            invDet = 1 / d;

            M11 = (a11 * b11 - a12 * b10 + a13 * b09) * invDet;
            M12 = (-a01 * b11 + a02 * b10 - a03 * b09) * invDet;
            M13 = (a31 * b05 - a32 * b04 + a33 * b03) * invDet;
            M14= (-a21 * b05 + a22 * b04 - a23 * b03) * invDet;
            M21 = (-a10 * b11 + a12 * b08 - a13 * b07) * invDet;
            M22 = (a00 * b11 - a02 * b08 + a03 * b07) * invDet;
            M23 = (-a30 * b05 + a32 * b02 - a33 * b01) * invDet;
            M24 = (a20 * b05 - a22 * b02 + a23 * b01) * invDet;
            M31 = (a10 * b10 - a11 * b08 + a13 * b06) * invDet;
            M32 = (-a00 * b10 + a01 * b08 - a03 * b06) * invDet;
            M33 = (a30 * b04 - a31 * b02 + a33 * b00) * invDet;
            M34 = (-a20 * b04 + a21 * b02 - a23 * b00) * invDet;
            OffsetX = (-a10 * b09 + a11 * b07 - a12 * b06) * invDet;
            OffsetY = (a00 * b09 - a01 * b07 + a02 * b06) * invDet;
            OffsetZ = (-a30 * b03 + a31 * b01 - a32 * b00) * invDet;
            M44 = (a20 * b03 - a21 * b01 + a22 * b00) * invDet;

        }





        public byte[] ToArray(bool useDouble = true)
        {
            if (useDouble)
            {
                Byte[] b = new Byte[16 * sizeof(double)];
                MemoryStream ms = new MemoryStream(b);
                BinaryWriter strm = new BinaryWriter(ms);
                strm.Write((double)M11);
                strm.Write((double)M12);
                strm.Write((double)M13);
                strm.Write((double)M14);
                strm.Write((double)M21);
                strm.Write((double)M22);
                strm.Write((double)M23);
                strm.Write((double)M24);
                strm.Write((double)M31);
                strm.Write((double)M32);
                strm.Write((double)M33);
                strm.Write((double)M34);
                strm.Write((double)OffsetX);
                strm.Write((double)OffsetY);
                strm.Write((double)OffsetZ);
                strm.Write((double)M44);
                return b;
            }
            else
            {
                Byte[] b = new Byte[16 * sizeof(float)];
                MemoryStream ms = new MemoryStream(b);
                BinaryWriter strm = new BinaryWriter(ms);
                strm.Write(M11);
                strm.Write(M12);
                strm.Write(M13);
                strm.Write(M14);
                strm.Write(M21);
                strm.Write(M22);
                strm.Write(M23);
                strm.Write(M24);
                strm.Write(M31);
                strm.Write(M32);
                strm.Write(M33);
                strm.Write(M34);
                strm.Write(OffsetX);
                strm.Write(OffsetY);
                strm.Write(OffsetZ);
                strm.Write(M44);
                return b;
            }
        }
        public void Scale(double s)
        {
            Scale((float)s);
        }
        public void Scale(float s)
        {
            
            M11 *= s;
            M12 *= s;
            M13 *= s;
            M14 *= s;
            M21 *= s;
            M22 *= s;
            M23 *= s;
            M24 *= s;
            M31 *= s;
            M32 *= s;
            M33 *= s;
            M34 *= s;
        }

        public void Scale(XbimVector3D xbimVector3D)
        {
            var x = xbimVector3D.X;
            var y = xbimVector3D.Y;
            var z = xbimVector3D.Z;
            M11 *= x;
            M12 *= x;
            M13 *= x;
            M14 *= x;
            M21 *= y;
            M22 *= y;
            M23 *= y;
            M24 *= y;
            M31 *= z;
            M32 *= z;
            M33 *= z;
            M34 *= z;
        }

       
    }
}
