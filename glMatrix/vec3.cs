using System;

namespace glMatrix
{
    public class vec3
    {
        #region Constructors
        //private constructor as this is a static helper
        private vec3()
        {
        }
        public static vec3 create()
        {
            return new vec3();
        }
        public static vec3 create(Double[] vec)
        {
            if (vec == null) throw new ArgumentNullException("vec");

            if (vec.Length != 3) throw new ArgumentOutOfRangeException("vec", "vec must have a length of 3");

            vec3 v = new vec3();
            v[0] = vec[0];
            v[1] = vec[1];
            v[2] = vec[2];
            return v;
        }
        public static vec3 create(Single[] vec)
        {
            if (vec == null) throw new ArgumentNullException("vec");

            if (vec.Length != 3) throw new ArgumentOutOfRangeException("vec", "vec must have a length of 3");

            return vec3.create(Array.ConvertAll<Single, Double>(vec, Convert.ToDouble));
        }
        public static vec3 create(Double x, Double y, Double z)
        {
            vec3 v = new vec3();
            v[0] = x;
            v[1] = y;
            v[2] = z;
            return v;
        }
        private static vec3 createFrom(Double p1, Double p2, Double p3)
        {
            return vec3.create(p1, p2, p3);
        }
        #endregion

        #region members
        private Double[] vals = new Double[3];
        private const Double FLOAT_EPSILON = 0.000001f;
        public Double X { get { return vals[0]; } set { vals[0] = value; } }
        public Double Y { get { return vals[1]; } set { vals[1] = value; } }
        public Double Z { get { return vals[2]; } set { vals[2] = value; } }
        #endregion

        #region Operators
        public Double this[int key]
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
        public static explicit operator Double[](vec3 a)
        {
            return a.vals;
        }
        public static explicit operator Single[](vec3 a)
        {
            return Array.ConvertAll<Double, Single>(a.vals, Convert.ToSingle);
        }
        public static explicit operator vec3(Double[] a)
        {
            return vec3.create(a);
        }
        public static vec3 operator *(vec3 a, vec3 b)
        {
            return vec3.multiply(a, b);
        }
        public static vec3 operator +(vec3 a, vec3 b)
        {
            return vec3.add(a, b);
        }
        public static vec3 operator -(vec3 a, vec3 b)
        {
            return vec3.subtract(a, b);
        }
        public override string ToString()
        {
            return vec3.str(this);
        }
        public override bool Equals(Object obj)
        {
            vec3 b = obj as vec3;
            if (b != null)
                return vec3.Equals(this, b);
            else
                return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
        #endregion

        public static vec3 add(vec3 vec, vec3 vec2, vec3 dest = null)
        {
            if (dest == null || vec == dest)
            {
                vec[0] += vec2[0];
                vec[1] += vec2[1];
                vec[2] += vec2[2];
                return vec;
            }

            dest[0] = vec[0] + vec2[0];
            dest[1] = vec[1] + vec2[1];
            dest[2] = vec[2] + vec2[2];
            return dest;
        }
        public static vec3 add(vec3 a, Double b)
        {
            throw new NotImplementedException();
        }
        public static vec3 subtract(vec3 vec, vec3 vec2, vec3 dest = null)
        {
            if (dest == null || vec == dest)
            {
                vec[0] -= vec2[0];
                vec[1] -= vec2[1];
                vec[2] -= vec2[2];
                return vec;
            }

            dest[0] = vec[0] - vec2[0];
            dest[1] = vec[1] - vec2[1];
            dest[2] = vec[2] - vec2[2];
            return dest;
        }
        public static vec3 multiply(vec3 vec, vec3 vec2, vec3 dest = null)
        {
            if (dest == null || vec == dest)
            {
                vec[0] *= vec2[0];
                vec[1] *= vec2[1];
                vec[2] *= vec2[2];
                return vec;
            }

            dest[0] = vec[0] * vec2[0];
            dest[1] = vec[1] * vec2[1];
            dest[2] = vec[2] * vec2[2];
            return dest;
        }
        public static vec3 negate(vec3 vec, vec3 dest)
        {
            if (dest == null) { dest = vec; }

            dest[0] = -vec[0];
            dest[1] = -vec[1];
            dest[2] = -vec[2];
            return dest;
        }
        public static vec3 set(vec3 vec, vec3 dest = null)
        {
            if (dest == null) dest = vec3.create();

            dest[0] = vec[0];
            dest[1] = vec[1];
            dest[2] = vec[2];

            return dest;
        }
        public static vec3 scale(vec3 vec, Double val, vec3 dest)
        {
            if (dest == null || vec == dest)
            {
                vec[0] *= val;
                vec[1] *= val;
                vec[2] *= val;
                return vec;
            }

            dest[0] = vec[0] * val;
            dest[1] = vec[1] * val;
            dest[2] = vec[2] * val;
            return dest;
        }
        public static Boolean equal(vec3 a, vec3 b)
        {
            return a == b || (
                (Double)Math.Abs(a[0] - b[0]) < FLOAT_EPSILON &&
                (Double)Math.Abs(a[1] - b[1]) < FLOAT_EPSILON &&
                (Double)Math.Abs(a[2] - b[2]) < FLOAT_EPSILON);
        }
        public static vec3 normalize(vec3 vec, vec3 dest = null)
        {
            if (dest == null) { dest = vec; }

            var x = vec[0];
            var y = vec[1];
            var z = vec[2];
            var len = (Double)Math.Sqrt(x * x + y * y + z * z);

            if (len == 0)
            {
                dest[0] = 0;
                dest[1] = 0;
                dest[2] = 0;
                return dest;
            }
            else if (len == 1)
            {
                dest[0] = x;
                dest[1] = y;
                dest[2] = z;
                return dest;
            }

            len = 1 / len;
            dest[0] = x * len;
            dest[1] = y * len;
            dest[2] = z * len;
            return dest;
        }
        public static vec3 cross(vec3 vec, vec3 vec2, vec3 dest = null)
        {
            if (dest == null) { dest = vec; }

            var x = vec[0];
            var y = vec[1];
            var z = vec[2];
            var x2 = vec2[0];
            var y2 = vec2[1];
            var z2 = vec2[2];

            dest[0] = y * z2 - z * y2;
            dest[1] = z * x2 - x * z2;
            dest[2] = x * y2 - y * x2;
            return dest;
        }

        public static Double length(vec3 vec)
        {
            var x = vec[0];
            var y = vec[1];
            var z = vec[2];
            return (Double)Math.Sqrt(x * x + y * y + z * z);
        }

        public static Double squaredLength(vec3 vec)
        {
            var x = vec[0];
            var y = vec[1];
            var z = vec[2];
            return x * x + y * y + z * z;
        }

        public static Double dot(vec3 vec, vec3 vec2)
        {
            return vec[0] * vec2[0] + vec[1] * vec2[1] + vec[2] * vec2[2];
        }

        public static vec3 direction(vec3 vec, vec3 vec2, vec3 dest = null)
        {
            if (dest == null) { dest = vec; }

            var x = vec[0] - vec2[0];
            var y = vec[1] - vec2[1];
            var z = vec[2] - vec2[2];
            var len = (Double)Math.Sqrt(x * x + y * y + z * z);

            if (len == 0)
            {
                dest[0] = 0;
                dest[1] = 0;
                dest[2] = 0;
                return dest;
            }

            len = 1 / len;
            dest[0] = x * len;
            dest[1] = y * len;
            dest[2] = z * len;
            return dest;
        }

        public static vec3 lerp(vec3 vec, vec3 vec2, Double lerp, vec3 dest = null)
        {
            if (dest == null) { dest = vec; }

            dest[0] = vec[0] + lerp * (vec2[0] - vec[0]);
            dest[1] = vec[1] + lerp * (vec2[1] - vec[1]);
            dest[2] = vec[2] + lerp * (vec2[2] - vec[2]);

            return dest;
        }

        public static Double dist(vec3 vec, vec3 vec2)
        {
            var x = vec2[0] - vec[0];
            var y = vec2[1] - vec[1];
            var z = vec2[2] - vec[2];

            return (Double)Math.Sqrt(x * x + y * y + z * z);
        }

        public static vec3 unproject(vec3 vec, mat4 view, mat4 proj, Double[] viewport, vec3 dest)
        {
            if (dest == null) { dest = vec; }

            var unprojectMat = mat4.create();
            var unprojectVec = new Double[4];

            var m = unprojectMat;
            var v = unprojectVec;

            v[0] = (vec[0] - viewport[0]) * 2.0f / viewport[2] - 1.0f;
            v[1] = (vec[1] - viewport[1]) * 2.0f / viewport[3] - 1.0f;
            v[2] = 2.0f * vec[2] - 1.0f;
            v[3] = 1.0f;

            mat4.multiply(proj, view, m);
            if (mat4.inverse(m) == null) { return null; }

            mat4.multiplyVec4(m, v);
            if (v[3] == 0.0) { return null; }

            dest[0] = v[0] / v[3];
            dest[1] = v[1] / v[3];
            dest[2] = v[2] / v[3];

            return dest;
        }

        private static vec3 xUnitVec3 = vec3.createFrom(1, 0, 0);
        private static vec3 yUnitVec3 = vec3.createFrom(0, 1, 0);
        private static vec3 zUnitVec3 = vec3.createFrom(0, 0, 1);

        //public static quat4 rotationTo(vec3 a, vec3 b, quat4 dest=null) {
        //    if (dest==null) { dest = quat4.create(); }

        //    var tmpvec3 = vec3.create();

        //    var d = vec3.dot(a, b);
        //    var axis = tmpvec3;
        //    if (d >= 1.0) {
        //        quat4.set(identityQuat4, dest);
        //    } else if (d < (0.000001 - 1.0)) {
        //        vec3.cross(xUnitVec3, a, axis);
        //        if (vec3.length(axis) < 0.000001)
        //            vec3.cross(yUnitVec3, a, axis);
        //        if (vec3.length(axis) < 0.000001)
        //            vec3.cross(zUnitVec3, a, axis);
        //        vec3.normalize(axis);
        //        quat4.fromAngleAxis(Math.PI, axis, dest);
        //    } else {
        //        var s = (Double)Math.sqrt((1.0 + d) * 2.0);
        //        var sInv = 1.0 / s;
        //        vec3.cross(a, b, axis);
        //        dest[0] = axis[0] * sInv;
        //        dest[1] = axis[1] * sInv;
        //        dest[2] = axis[2] * sInv;
        //        dest[3] = s * 0.5;
        //        quat4.normalize(dest);
        //    }
        //    if (dest[3] > 1.0) dest[3] = 1.0;
        //    else if (dest[3] < -1.0) dest[3] = -1.0;
        //    return dest;
        //}

        public static String str(vec3 vec)
        {
            return "[" + vec[0] + ", " + vec[1] + ", " + vec[2] + "]";
        }
    }
}
