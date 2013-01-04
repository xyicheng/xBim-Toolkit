using System;

namespace glMatrix
{
    public class mat4
    {
        #region Constructors
        //private constructor as this is a static helper
        private mat4()
        {
        }
        /// <summary>
        /// Creates a new instance of a mat4
        /// </summary>
        /// <returns>New mat4</returns>
        public static mat4 create()
        {
            return new mat4();
        }
        /// <summary>
        /// Creates a new instance of a mat4
        /// </summary>
        /// <param name="mat">Single[16] containing values to initialize with</param>
        /// <returns>New mat4New mat4</returns>
        public static mat4 create(Single[] mat)
        {
            if (mat == null) throw new ArgumentNullException("mat");

            if (mat.Length != 16) throw new ArgumentOutOfRangeException("mat", "mat must have a length of 16");

            mat4 v = new mat4();
            Array.Copy(mat, v.vals, 16);
            return v;
        }
        /// <summary>
        /// Creates a new instance of a mat4, initializing it with the given arguments
        /// </summary>
        /// <returns>New mat4</returns>
        public static mat4 createFrom(Single m00, Single m01, Single m02, Single m03, Single m10, Single m11, Single m12, Single m13, Single m20, Single m21, Single m22, Single m23, Single m30, Single m31, Single m32, Single m33)
        {
            var dest = new mat4();

            dest[0] = m00;
            dest[1] = m01;
            dest[2] = m02;
            dest[3] = m03;
            dest[4] = m10;
            dest[5] = m11;
            dest[6] = m12;
            dest[7] = m13;
            dest[8] = m20;
            dest[9] = m21;
            dest[10] = m22;
            dest[11] = m23;
            dest[12] = m30;
            dest[13] = m31;
            dest[14] = m32;
            dest[15] = m33;

            return dest;
        }
        #endregion

        #region members
        private Single[] vals = new Single[16];
        private static Single[] _identity = new Single[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
        public Single m00 { get { return this[0]; } set { this[0] = value; } }
        public Single m01 { get { return this[1]; } set { this[1] = value; } }
        public Single m02 { get { return this[2]; } set { this[2] = value; } }
        public Single m03 { get { return this[3]; } set { this[3] = value; } }
        public Single m10 { get { return this[4]; } set { this[4] = value; } }
        public Single m11 { get { return this[5]; } set { this[5] = value; } }
        public Single m12 { get { return this[6]; } set { this[6] = value; } }
        public Single m13 { get { return this[7]; } set { this[7] = value; } }
        public Single m20 { get { return this[8]; } set { this[8] = value; } }
        public Single m21 { get { return this[9]; } set { this[9] = value; } }
        public Single m22 { get { return this[10]; } set { this[10] = value; } }
        public Single m23 { get { return this[11]; } set { this[11] = value; } }
        public Single m30 { get { return this[12]; } set { this[12] = value; } }
        public Single m31 { get { return this[13]; } set { this[13] = value; } }
        public Single m32 { get { return this[14]; } set { this[14] = value; } }
        public Single m33 { get { return this[15]; } set { this[15] = value; } }
        private const Single FLOAT_EPSILON = 0.000001f;
        #endregion

        #region Operators
        public Single this[int key]
        {
            get
            {
                return vals[key];
            }
            set
            {
                vals[key] = value;
            }
        }
        public static explicit operator Single[](mat4 a)
        {
            return a.vals;
        }
        public static explicit operator mat4(Single[] a)
        {
            return mat4.create(a);
        }
        public static mat4 operator *(mat4 a, mat4 b)
        {
            return mat4.multiply(a, b);
        }
        public override bool Equals(object obj)
        {
            mat4 b = obj as mat4;
            if (b != null)
                return mat4.equal(this, b);
            else
                return base.Equals(obj);
        }
        public override string ToString()
        {
            return mat4.str(this);
        }
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
        #endregion

        #region glMatrix.js
        /// <summary>
        /// Compares two matrices for equality within a certain margin of error
        /// </summary>
        /// <param name="a">a First matrix</param>
        /// <param name="b">b Second matrix</param>
        /// <returns>True if a is equivalent to b</returns>
        public static bool equal(mat4 a, mat4 b)
        {
            return a == b || (
                Math.Abs(a[0] - b[0]) < FLOAT_EPSILON &&
                Math.Abs(a[1] - b[1]) < FLOAT_EPSILON &&
                Math.Abs(a[2] - b[2]) < FLOAT_EPSILON &&
                Math.Abs(a[3] - b[3]) < FLOAT_EPSILON &&
                Math.Abs(a[4] - b[4]) < FLOAT_EPSILON &&
                Math.Abs(a[5] - b[5]) < FLOAT_EPSILON &&
                Math.Abs(a[6] - b[6]) < FLOAT_EPSILON &&
                Math.Abs(a[7] - b[7]) < FLOAT_EPSILON &&
                Math.Abs(a[8] - b[8]) < FLOAT_EPSILON &&
                Math.Abs(a[9] - b[9]) < FLOAT_EPSILON &&
                Math.Abs(a[10] - b[10]) < FLOAT_EPSILON &&
                Math.Abs(a[11] - b[11]) < FLOAT_EPSILON &&
                Math.Abs(a[12] - b[12]) < FLOAT_EPSILON &&
                Math.Abs(a[13] - b[13]) < FLOAT_EPSILON &&
                Math.Abs(a[14] - b[14]) < FLOAT_EPSILON &&
                Math.Abs(a[15] - b[15]) < FLOAT_EPSILON
            );
        }
        /// <summary>
        /// Sets a mat4 to an identity matrix
        /// </summary>
        /// <param name="dest">dest mat4 to set</param>
        /// <returns>dest</returns>
        public static mat4 identity(mat4 dest = null)
        {
            if (dest == null) dest = new mat4();
            dest.vals = mat4._identity;
            return dest;
        }

        /// <summary>
        /// Rotates a matrix by the given angle around the X axis
        /// </summary>
        /// <param name="mat">mat mat4 to rotate</param>
        /// <param name="angle">angle Angle (in radians) to rotate</param>
        /// <param name="dest">mat4 receiving operation result. If not specified result is written to mat</param>
        /// <returns>dest if specified, mat otherwise</returns>
        public static mat4 rotateX(mat4 mat, Single angle, mat4 dest = null)
        {
            Single s = (Single)(Single)Math.Sin(angle),
            c = (Single)(Single)Math.Cos(angle),
            a10 = mat[4],
            a11 = mat[5],
            a12 = mat[6],
            a13 = mat[7],
            a20 = mat[8],
            a21 = mat[9],
            a22 = mat[10],
            a23 = mat[11];

            if (dest == null)
            {
                dest = mat;
            }
            else if (mat != dest)
            { // If the source and destination differ, copy the unchanged rows
                dest[0] = mat[0];
                dest[1] = mat[1];
                dest[2] = mat[2];
                dest[3] = mat[3];

                dest[12] = mat[12];
                dest[13] = mat[13];
                dest[14] = mat[14];
                dest[15] = mat[15];
            }

            // Perform axis-specific matrix multiplication
            dest[4] = a10 * c + a20 * s;
            dest[5] = a11 * c + a21 * s;
            dest[6] = a12 * c + a22 * s;
            dest[7] = a13 * c + a23 * s;

            dest[8] = a10 * -s + a20 * c;
            dest[9] = a11 * -s + a21 * c;
            dest[10] = a12 * -s + a22 * c;
            dest[11] = a13 * -s + a23 * c;
            return dest;
        }

        /// <summary>
        /// Rotates a matrix by the given angle around the Z axis
        /// </summary>
        /// <param name="mat">mat mat4 to rotate</param>
        /// <param name="angle">angle Angle (in radians) to rotate</param>
        /// <param name="dest">mat4 receiving operation result. If not specified result is written to mat</param>
        /// <returns>dest if specified, mat otherwise</returns>
        public static mat4 rotateZ(mat4 mat, Single angle, mat4 dest = null)
        {
            Single s = (Single)Math.Sin(angle),
            c = (Single)Math.Cos(angle),
            a00 = mat[0],
            a01 = mat[1],
            a02 = mat[2],
            a03 = mat[3],
            a10 = mat[4],
            a11 = mat[5],
            a12 = mat[6],
            a13 = mat[7];

            if (dest == null)
            {
                dest = mat;
            }
            else if (mat != dest)
            { // If the source and destination differ, copy the unchanged last row
                dest[8] = mat[8];
                dest[9] = mat[9];
                dest[10] = mat[10];
                dest[11] = mat[11];

                dest[12] = mat[12];
                dest[13] = mat[13];
                dest[14] = mat[14];
                dest[15] = mat[15];
            }

            // Perform axis-specific matrix multiplication
            dest[0] = a00 * c + a10 * s;
            dest[1] = a01 * c + a11 * s;
            dest[2] = a02 * c + a12 * s;
            dest[3] = a03 * c + a13 * s;

            dest[4] = a00 * -s + a10 * c;
            dest[5] = a01 * -s + a11 * c;
            dest[6] = a02 * -s + a12 * c;
            dest[7] = a03 * -s + a13 * c;

            return dest;
        }

        /// <summary>
        /// Rotates a matrix by the given angle around the Y axis
        /// </summary>
        /// <param name="mat">mat mat4 to rotate</param>
        /// <param name="angle">angle Angle (in radians) to rotate</param>
        /// <param name="dest">mat4 receiving operation result. If not specified result is written to mat</param>
        /// <returns>dest if specified, mat otherwise</returns>
        public static mat4 rotateY(mat4 mat, Single angle, mat4 dest = null)
        {
            Single s = (Single)Math.Sin(angle),
            c = (Single)Math.Cos(angle),
            a00 = mat[0],
            a01 = mat[1],
            a02 = mat[2],
            a03 = mat[3],
            a20 = mat[8],
            a21 = mat[9],
            a22 = mat[10],
            a23 = mat[11];

            if (dest == null)
            {
                dest = mat;
            }
            else if (mat != dest)
            { // If the source and destination differ, copy the unchanged rows
                dest[4] = mat[4];
                dest[5] = mat[5];
                dest[6] = mat[6];
                dest[7] = mat[7];

                dest[12] = mat[12];
                dest[13] = mat[13];
                dest[14] = mat[14];
                dest[15] = mat[15];
            }

            // Perform axis-specific matrix multiplication
            dest[0] = a00 * c + a20 * -s;
            dest[1] = a01 * c + a21 * -s;
            dest[2] = a02 * c + a22 * -s;
            dest[3] = a03 * c + a23 * -s;

            dest[8] = a00 * s + a20 * c;
            dest[9] = a01 * s + a21 * c;
            dest[10] = a02 * s + a22 * c;
            dest[11] = a03 * s + a23 * c;
            return dest;
        }

        /// <summary>
        /// Rotates a matrix by the given angle around the specified axis
        /// If rotating around a primary axis (X,Y,Z) one of the specialized rotation functions should be used instead for performance
        /// </summary>
        /// <param name="mat">mat mat4 to rotate</param>
        /// <param name="angle">angle Angle (in radians) to rotate</param>
        /// <param name="axis">axis vec3 representing the axis to rotate around</param>
        /// <param name="dest">mat4 receiving operation result. If not specified result is written to mat</param>
        /// <returns>dest if specified, mat otherwise</returns>
        public static mat4 rotate(mat4 mat, Single angle, vec3 axis, mat4 dest = null)
        {
            Single x = axis[0], y = axis[1], z = axis[2],
                len = (Single)Math.Sqrt(x * x + y * y + z * z),
                s, c, t,
                a00, a01, a02, a03,
                a10, a11, a12, a13,
                a20, a21, a22, a23,
                b00, b01, b02,
                b10, b11, b12,
                b20, b21, b22;

            if (len == 0) { return null; }
            if (len != 1)
            {
                len = 1 / len;
                x *= len;
                y *= len;
                z *= len;
            }

            s = (Single)Math.Sin(angle);
            c = (Single)Math.Cos(angle);
            t = 1 - c;

            a00 = mat[0]; a01 = mat[1]; a02 = mat[2]; a03 = mat[3];
            a10 = mat[4]; a11 = mat[5]; a12 = mat[6]; a13 = mat[7];
            a20 = mat[8]; a21 = mat[9]; a22 = mat[10]; a23 = mat[11];

            // Construct the elements of the rotation matrix
            b00 = x * x * t + c; b01 = y * x * t + z * s; b02 = z * x * t - y * s;
            b10 = x * y * t - z * s; b11 = y * y * t + c; b12 = z * y * t + x * s;
            b20 = x * z * t + y * s; b21 = y * z * t - x * s; b22 = z * z * t + c;

            if (dest == null)
            {
                dest = mat;
            }
            else if (mat != dest)
            { // If the source and destination differ, copy the unchanged last row
                dest[12] = mat[12];
                dest[13] = mat[13];
                dest[14] = mat[14];
                dest[15] = mat[15];
            }

            // Perform rotation-specific matrix multiplication
            dest[0] = a00 * b00 + a10 * b01 + a20 * b02;
            dest[1] = a01 * b00 + a11 * b01 + a21 * b02;
            dest[2] = a02 * b00 + a12 * b01 + a22 * b02;
            dest[3] = a03 * b00 + a13 * b01 + a23 * b02;

            dest[4] = a00 * b10 + a10 * b11 + a20 * b12;
            dest[5] = a01 * b10 + a11 * b11 + a21 * b12;
            dest[6] = a02 * b10 + a12 * b11 + a22 * b12;
            dest[7] = a03 * b10 + a13 * b11 + a23 * b12;

            dest[8] = a00 * b20 + a10 * b21 + a20 * b22;
            dest[9] = a01 * b20 + a11 * b21 + a21 * b22;
            dest[10] = a02 * b20 + a12 * b21 + a22 * b22;
            dest[11] = a03 * b20 + a13 * b21 + a23 * b22;
            return dest;
        }

        /// <summary>
        /// Translates a matrix by the given vector
        /// </summary>
        /// <param name="mat">mat mat4 to translate</param>
        /// <param name="vec">vec vec3 specifying the translation</param>
        /// <param name="dest">mat4 receiving operation result. If not specified result is written to mat</param>
        /// <returns>dest if specified, mat otherwise</returns>
        public static mat4 translate(mat4 mat, vec3 vec, mat4 dest = null)
        {
            var x = vec[0];
            var y = vec[1];
            var z = vec[2];
            Single a00, a01, a02, a03,
            a10, a11, a12, a13,
            a20, a21, a22, a23;

            if (dest == null || mat == dest)
            {
                mat[12] = mat[0] * x + mat[4] * y + mat[8] * z + mat[12];
                mat[13] = mat[1] * x + mat[5] * y + mat[9] * z + mat[13];
                mat[14] = mat[2] * x + mat[6] * y + mat[10] * z + mat[14];
                mat[15] = mat[3] * x + mat[7] * y + mat[11] * z + mat[15];
                return mat;
            }

            a00 = mat[0]; a01 = mat[1]; a02 = mat[2]; a03 = mat[3];
            a10 = mat[4]; a11 = mat[5]; a12 = mat[6]; a13 = mat[7];
            a20 = mat[8]; a21 = mat[9]; a22 = mat[10]; a23 = mat[11];

            dest[0] = a00; dest[1] = a01; dest[2] = a02; dest[3] = a03;
            dest[4] = a10; dest[5] = a11; dest[6] = a12; dest[7] = a13;
            dest[8] = a20; dest[9] = a21; dest[10] = a22; dest[11] = a23;

            dest[12] = a00 * x + a10 * y + a20 * z + mat[12];
            dest[13] = a01 * x + a11 * y + a21 * z + mat[13];
            dest[14] = a02 * x + a12 * y + a22 * z + mat[14];
            dest[15] = a03 * x + a13 * y + a23 * z + mat[15];
            return dest;
        }

        /// <summary>
        /// Calculates the inverse matrix of a mat4
        /// </summary>
        /// <param name="mat">mat mat4 to calculate inverse of</param>
        /// <param name="dest">mat4 receiving inverse matrix. If not specified result is written to mat</param>
        /// <returns>dest if specified, mat otherwise, null if matrix cannot be inverted</returns>
        public static mat4 inverse(mat4 mat, mat4 dest = null)
        {
            if (dest == null) { dest = mat; }

            // Cache the matrix values (makes for huge speed increases!)
            Single a00 = mat[0], a01 = mat[1], a02 = mat[2], a03 = mat[3],
                a10 = mat[4], a11 = mat[5], a12 = mat[6], a13 = mat[7],
                a20 = mat[8], a21 = mat[9], a22 = mat[10], a23 = mat[11],
                a30 = mat[12], a31 = mat[13], a32 = mat[14], a33 = mat[15],

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
            if (d == 0) { return null; }
            invDet = 1 / d;

            dest[0] = (a11 * b11 - a12 * b10 + a13 * b09) * invDet;
            dest[1] = (-a01 * b11 + a02 * b10 - a03 * b09) * invDet;
            dest[2] = (a31 * b05 - a32 * b04 + a33 * b03) * invDet;
            dest[3] = (-a21 * b05 + a22 * b04 - a23 * b03) * invDet;
            dest[4] = (-a10 * b11 + a12 * b08 - a13 * b07) * invDet;
            dest[5] = (a00 * b11 - a02 * b08 + a03 * b07) * invDet;
            dest[6] = (-a30 * b05 + a32 * b02 - a33 * b01) * invDet;
            dest[7] = (a20 * b05 - a22 * b02 + a23 * b01) * invDet;
            dest[8] = (a10 * b10 - a11 * b08 + a13 * b06) * invDet;
            dest[9] = (-a00 * b10 + a01 * b08 - a03 * b06) * invDet;
            dest[10] = (a30 * b04 - a31 * b02 + a33 * b00) * invDet;
            dest[11] = (-a20 * b04 + a21 * b02 - a23 * b00) * invDet;
            dest[12] = (-a10 * b09 + a11 * b07 - a12 * b06) * invDet;
            dest[13] = (a00 * b09 - a01 * b07 + a02 * b06) * invDet;
            dest[14] = (-a30 * b03 + a31 * b01 - a32 * b00) * invDet;
            dest[15] = (a20 * b03 - a21 * b01 + a22 * b00) * invDet;

            return dest;
        }

        /// <summary>
        /// Generates a frustum matrix with the given bounds
        /// </summary>
        /// <param name="left">left Left bound of the frustum</param>
        /// <param name="right">right Right bound of the frustum</param>
        /// <param name="bottom">bottom Bottom bound of the frustum</param>
        /// <param name="top">top Top bound of the frustum</param>
        /// <param name="near">near Near bound of the frustum</param>
        /// <param name="far">far Far bound of the frustum</param>
        /// <param name="dest">mat4 frustum matrix will be written into</param>
        /// <returns>dest if specified, a new mat4 otherwise</returns>
        public static mat4 frustum(Single left, Single right, Single bottom, Single top, Single near, Single far, mat4 dest = null)
        {
            if (dest == null) { dest = mat4.create(); }
            Single rl = (right - left),
                tb = (top - bottom),
                fn = (far - near);
            dest[0] = (near * 2) / rl;
            dest[1] = 0;
            dest[2] = 0;
            dest[3] = 0;
            dest[4] = 0;
            dest[5] = (near * 2) / tb;
            dest[6] = 0;
            dest[7] = 0;
            dest[8] = (right + left) / rl;
            dest[9] = (top + bottom) / tb;
            dest[10] = -(far + near) / fn;
            dest[11] = -1;
            dest[12] = 0;
            dest[13] = 0;
            dest[14] = -(far * near * 2) / fn;
            dest[15] = 0;
            return dest;
        }

        /// <summary>
        /// Generates a perspective projection matrix with the given bounds
        /// </summary>
        /// <param name="fovy">fovy Vertical field of view</param>
        /// <param name="aspect">aspect Aspect ratio. typically viewport width/height</param>
        /// <param name="near">near Near bound of the frustum</param>
        /// <param name="far">far Far bound of the frustum</param>
        /// <param name="dest">mat4 frustum matrix will be written into</param>
        /// <returns>dest if specified, a new mat4 otherwise</returns>
        public static mat4 perspective(Single fovy, Single aspect, Single near, Single far, mat4 dest = null)
        {
            Single top = near * (Single)Math.Tan(fovy * (Single)Math.PI / 360.0), right = top * aspect;
            return mat4.frustum(-right, right, -top, top, near, far, dest);
        }

        /// <summary>
        /// Generates a orthogonal projection matrix with the given bounds
        /// </summary>
        /// <param name="left">left Left bound of the frustum</param>
        /// <param name="right">right Right bound of the frustum</param>
        /// <param name="bottom">bottom Bottom bound of the frustum</param>
        /// <param name="top">top Top bound of the frustum</param>
        /// <param name="near">near Near bound of the frustum</param>
        /// <param name="far">far Far bound of the frustum</param>
        /// <param name="dest">mat4 frustum matrix will be written into</param>
        /// <returns>dest if specified, a new mat4 otherwise</returns>
        public static mat4 ortho(Single left, Single right, Single bottom, Single top, Single near, Single far, mat4 dest = null)
        {
            if (dest == null) { dest = mat4.create(); }
            Single rl = (right - left),
                tb = (top - bottom),
                fn = (far - near);
            dest[0] = 2 / rl;
            dest[1] = 0;
            dest[2] = 0;
            dest[3] = 0;
            dest[4] = 0;
            dest[5] = 2 / tb;
            dest[6] = 0;
            dest[7] = 0;
            dest[8] = 0;
            dest[9] = 0;
            dest[10] = -2 / fn;
            dest[11] = 0;
            dest[12] = -(left + right) / rl;
            dest[13] = -(top + bottom) / tb;
            dest[14] = -(far + near) / fn;
            dest[15] = 1;
            return dest;
        }

        /// <summary>
        /// Generates a look-at matrix with the given eye position, focal point, and up axis
        /// </summary>
        /// <param name="eye">eye Position of the viewer</param>
        /// <param name="center">center Point the viewer is looking at</param>
        /// <param name="up">up vec3 pointing "up"</param>
        /// <param name="dest">mat4 frustum matrix will be written into</param>
        /// <returns></returns>
        public static mat4 lookAt(vec3 eye, vec3 center, vec3 up, mat4 dest = null)
        {
            if (dest == null) { dest = mat4.create(); }

            Single x0, x1, x2, y0, y1, y2, z0, z1, z2, len,
                eyex = eye[0],
                eyey = eye[1],
                eyez = eye[2],
                upx = up[0],
                upy = up[1],
                upz = up[2],
                centerx = center[0],
                centery = center[1],
                centerz = center[2];

            if (eyex == centerx && eyey == centery && eyez == centerz)
            {
                return mat4.identity(dest);
            }

            //vec3.direction(eye, center, z);
            z0 = eyex - centerx;
            z1 = eyey - centery;
            z2 = eyez - centerz;

            // normalize (no check needed for 0 because of early return)
            len = (Single)(1 / Math.Sqrt(z0 * z0 + z1 * z1 + z2 * z2));
            z0 *= len;
            z1 *= len;
            z2 *= len;

            //vec3.normalize(vec3.cross(up, z, x));
            x0 = upy * z2 - upz * z1;
            x1 = upz * z0 - upx * z2;
            x2 = upx * z1 - upy * z0;
            len = (Single)Math.Sqrt(x0 * x0 + x1 * x1 + x2 * x2);
            if (len == 0)
            {
                x0 = 0;
                x1 = 0;
                x2 = 0;
            }
            else
            {
                len = 1 / len;
                x0 *= len;
                x1 *= len;
                x2 *= len;
            }

            //vec3.normalize(vec3.cross(z, x, y));
            y0 = z1 * x2 - z2 * x1;
            y1 = z2 * x0 - z0 * x2;
            y2 = z0 * x1 - z1 * x0;

            len = (Single)Math.Sqrt(y0 * y0 + y1 * y1 + y2 * y2);
            if (len == 0)
            {
                y0 = 0;
                y1 = 0;
                y2 = 0;
            }
            else
            {
                len = 1 / len;
                y0 *= len;
                y1 *= len;
                y2 *= len;
            }

            dest[0] = x0;
            dest[1] = y0;
            dest[2] = z0;
            dest[3] = 0;
            dest[4] = x1;
            dest[5] = y1;
            dest[6] = z1;
            dest[7] = 0;
            dest[8] = x2;
            dest[9] = y2;
            dest[10] = z2;
            dest[11] = 0;
            dest[12] = -(x0 * eyex + x1 * eyey + x2 * eyez);
            dest[13] = -(y0 * eyex + y1 * eyey + y2 * eyez);
            dest[14] = -(z0 * eyex + z1 * eyey + z2 * eyez);
            dest[15] = 1;

            return dest;
        }

        /// <summary>
        /// Transforms a vec3 with the given matrix. 4th vector component is implicitly '1'
        /// </summary>
        /// <param name="mat">mat mat4 to transform the vector with</param>
        /// <param name="vec">vec vec3 to transform</param>
        /// <param name="dest">vec3 receiving operation result. If not specified result is written to vec</param>
        /// <returns>dest if specified, vec otherwise</returns>
        public static vec3 multiplyVec3(mat4 mat, vec3 vec, vec3 dest = null)
        {
            if (dest == null) { dest = vec; }

            var x = vec[0];
            var y = vec[1];
            var z = vec[2];

            dest[0] = mat[0] * x + mat[4] * y + mat[8] * z + mat[12];
            dest[1] = mat[1] * x + mat[5] * y + mat[9] * z + mat[13];
            dest[2] = mat[2] * x + mat[6] * y + mat[10] * z + mat[14];

            return dest;
        }

        /// <summary>
        /// Transforms a vec4 with the given matrix
        /// </summary>
        /// <param name="mat">mat mat4 to transform the vector with</param>
        /// <param name="vec">vec vec4 to transform</param>
        /// <param name="dest">vec4 receiving operation result. If not specified result is written to vec</param>
        /// <returns>dest if specified, vec otherwise</returns>
        public static Single[] multiplyVec4(mat4 mat, Single[] vec, Single[] dest = null)
        {
            if (dest == null) { dest = vec; }

            var x = vec[0];
            var y = vec[1];
            var z = vec[2];
            var w = vec[3];

            dest[0] = mat[0] * x + mat[4] * y + mat[8] * z + mat[12] * w;
            dest[1] = mat[1] * x + mat[5] * y + mat[9] * z + mat[13] * w;
            dest[2] = mat[2] * x + mat[6] * y + mat[10] * z + mat[14] * w;
            dest[3] = mat[3] * x + mat[7] * y + mat[11] * z + mat[15] * w;

            return dest;
        }

        /// <summary>
        /// Performs a matrix multiplication
        /// </summary>
        /// <param name="mat">mat First operand</param>
        /// <param name="mat2">mat2 Second operand</param>
        /// <param name="dest">mat4 receiving operation result. If not specified result is written to mat</param>
        /// <returns>dest if specified, mat otherwise</returns>
        public static mat4 multiply(mat4 mat, mat4 mat2, mat4 dest = null)
        {
            if (dest == null) { dest = mat; }

            // Cache the matrix values (makes for huge speed increases!)
            var a00 = mat[0];
            var a01 = mat[1];
            var a02 = mat[2];
            var a03 = mat[3];
            var a10 = mat[4];
            var a11 = mat[5];
            var a12 = mat[6];
            var a13 = mat[7];
            var a20 = mat[8];
            var a21 = mat[9];
            var a22 = mat[10];
            var a23 = mat[11];
            var a30 = mat[12];
            var a31 = mat[13];
            var a32 = mat[14];
            var a33 = mat[15];

            // Cache only the current line of the second matrix
            var b0 = mat2[0];
            var b1 = mat2[1];
            var b2 = mat2[2];
            var b3 = mat2[3];
            dest[0] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
            dest[1] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
            dest[2] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
            dest[3] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

            b0 = mat2[4];
            b1 = mat2[5];
            b2 = mat2[6];
            b3 = mat2[7];
            dest[4] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
            dest[5] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
            dest[6] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
            dest[7] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

            b0 = mat2[8];
            b1 = mat2[9];
            b2 = mat2[10];
            b3 = mat2[11];
            dest[8] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
            dest[9] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
            dest[10] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
            dest[11] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

            b0 = mat2[12];
            b1 = mat2[13];
            b2 = mat2[14];
            b3 = mat2[15];
            dest[12] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
            dest[13] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
            dest[14] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
            dest[15] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

            return dest;
        }

        /// <summary>
        /// Scales a matrix by the given vector
        /// </summary>
        /// <param name="mat">mat mat4 to scale</param>
        /// <param name="vec">vec vec3 specifying the scale for each axis</param>
        /// <param name="dest">mat4 receiving operation result. If not specified result is written to mat</param>
        /// <returns>dest if specified, mat otherwise</returns>
        public static mat4 scale(mat4 mat, vec3 vec, mat4 dest = null)
        {
            var x = vec[0];
            var y = vec[1];
            var z = vec[2];

            if (dest == null || mat == dest)
            {
                mat[0] *= x;
                mat[1] *= x;
                mat[2] *= x;
                mat[3] *= x;
                mat[4] *= y;
                mat[5] *= y;
                mat[6] *= y;
                mat[7] *= y;
                mat[8] *= z;
                mat[9] *= z;
                mat[10] *= z;
                mat[11] *= z;
                return mat;
            }

            dest[0] = mat[0] * x;
            dest[1] = mat[1] * x;
            dest[2] = mat[2] * x;
            dest[3] = mat[3] * x;
            dest[4] = mat[4] * y;
            dest[5] = mat[5] * y;
            dest[6] = mat[6] * y;
            dest[7] = mat[7] * y;
            dest[8] = mat[8] * z;
            dest[9] = mat[9] * z;
            dest[10] = mat[10] * z;
            dest[11] = mat[11] * z;
            dest[12] = mat[12];
            dest[13] = mat[13];
            dest[14] = mat[14];
            dest[15] = mat[15];
            return dest;
        }

        /// <summary>
        /// Returns a string representation of a mat4
        /// </summary>
        /// <param name="mat">mat mat4 to represent as a string</param>
        /// <returns>String representation of mat</returns>
        public static string str(mat4 mat)
        {
            return "[" + mat[0] + ", " + mat[1] + ", " + mat[2] + ", " + mat[3] +
                ", " + mat[4] + ", " + mat[5] + ", " + mat[6] + ", " + mat[7] +
                ", " + mat[8] + ", " + mat[9] + ", " + mat[10] + ", " + mat[11] +
                ", " + mat[12] + ", " + mat[13] + ", " + mat[14] + ", " + mat[15] + "]";
        }

        /// <summary>
        /// Copies the upper 3x3 elements of a mat4 into another mat4
        /// </summary>
        /// <param name="mat">mat mat4 containing values to copy</param>
        /// <param name="dest">[dest] mat4 receiving copied values</param>
        /// <returns>dest if specified, a new mat4 otherwise</returns>
        public static mat4 toRotationMat(mat4 mat, mat4 dest = null)
        {
            if (dest == null) { dest = mat4.create(); }

            dest[0] = mat[0];
            dest[1] = mat[1];
            dest[2] = mat[2];
            dest[3] = mat[3];
            dest[4] = mat[4];
            dest[5] = mat[5];
            dest[6] = mat[6];
            dest[7] = mat[7];
            dest[8] = mat[8];
            dest[9] = mat[9];
            dest[10] = mat[10];
            dest[11] = mat[11];
            dest[12] = 0;
            dest[13] = 0;
            dest[14] = 0;
            dest[15] = 1;

            return dest;
        }

        /// <summary>
        /// Calculates the determinant of a mat4
        /// </summary>
        /// <param name="mat">mat mat4 to calculate determinant of</param>
        /// <returns>determinant of mat</returns>
        public static Single determinant(mat4 mat)
        {
            // Cache the matrix values (makes for huge speed increases!)
            Single a00 = mat[0], a01 = mat[1], a02 = mat[2], a03 = mat[3],
                a10 = mat[4], a11 = mat[5], a12 = mat[6], a13 = mat[7],
                a20 = mat[8], a21 = mat[9], a22 = mat[10], a23 = mat[11],
                a30 = mat[12], a31 = mat[13], a32 = mat[14], a33 = mat[15];

            return (a30 * a21 * a12 * a03 - a20 * a31 * a12 * a03 - a30 * a11 * a22 * a03 + a10 * a31 * a22 * a03 +
                    a20 * a11 * a32 * a03 - a10 * a21 * a32 * a03 - a30 * a21 * a02 * a13 + a20 * a31 * a02 * a13 +
                    a30 * a01 * a22 * a13 - a00 * a31 * a22 * a13 - a20 * a01 * a32 * a13 + a00 * a21 * a32 * a13 +
                    a30 * a11 * a02 * a23 - a10 * a31 * a02 * a23 - a30 * a01 * a12 * a23 + a00 * a31 * a12 * a23 +
                    a10 * a01 * a32 * a23 - a00 * a11 * a32 * a23 - a20 * a11 * a02 * a33 + a10 * a21 * a02 * a33 +
                    a20 * a01 * a12 * a33 - a00 * a21 * a12 * a33 - a10 * a01 * a22 * a33 + a00 * a11 * a22 * a33);
        }

        /// <summary>
        /// Transposes a mat4 (flips the values over the diagonal)
        /// </summary>
        /// <param name="mat">mat mat4 to transpose</param>
        /// <param name="dest">mat4 receiving transposed values. If not specified result is written to mat</param>
        /// <returns>dest if specified, mat otherwise</returns>
        public static mat4 transpose(mat4 mat, mat4 dest)
        {
            // If we are transposing ourselves we can skip a few steps but have to cache some values
            if (dest == null || mat == dest)
            {
                Single a01 = mat[1], a02 = mat[2], a03 = mat[3],
                    a12 = mat[6], a13 = mat[7],
                    a23 = mat[11];

                mat[1] = mat[4];
                mat[2] = mat[8];
                mat[3] = mat[12];
                mat[4] = a01;
                mat[6] = mat[9];
                mat[7] = mat[13];
                mat[8] = a02;
                mat[9] = a12;
                mat[11] = mat[14];
                mat[12] = a03;
                mat[13] = a13;
                mat[14] = a23;
                return mat;
            }

            dest[0] = mat[0];
            dest[1] = mat[4];
            dest[2] = mat[8];
            dest[3] = mat[12];
            dest[4] = mat[1];
            dest[5] = mat[5];
            dest[6] = mat[9];
            dest[7] = mat[13];
            dest[8] = mat[2];
            dest[9] = mat[6];
            dest[10] = mat[10];
            dest[11] = mat[14];
            dest[12] = mat[3];
            dest[13] = mat[7];
            dest[14] = mat[11];
            dest[15] = mat[15];
            return dest;
        }

        /// <summary>
        /// Copies the values of one mat4 to another
        /// </summary>
        /// <param name="mat">mat mat4 containing values to copy</param>
        /// <param name="dest">dest mat4 receiving copied values</param>
        /// <returns>dest</returns>
        public static mat4 set(mat4 mat, mat4 dest = null)
        {
            if (dest == null) dest = new mat4();
            dest[0] = mat[0];
            dest[1] = mat[1];
            dest[2] = mat[2];
            dest[3] = mat[3];
            dest[4] = mat[4];
            dest[5] = mat[5];
            dest[6] = mat[6];
            dest[7] = mat[7];
            dest[8] = mat[8];
            dest[9] = mat[9];
            dest[10] = mat[10];
            dest[11] = mat[11];
            dest[12] = mat[12];
            dest[13] = mat[13];
            dest[14] = mat[14];
            dest[15] = mat[15];
            return dest;
        }
        #endregion
    }
}
