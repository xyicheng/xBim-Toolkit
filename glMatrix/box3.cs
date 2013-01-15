using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace glMatrix
{
    /// <summary>
    /// Represents a Bounding Box based on 3 dimensions
    /// </summary>
    public class box3
    {
        private box3() { }
        public static box3 create() { return new box3(); }

        public vec3 Min = vec3.create(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue);
        public vec3 Max = vec3.create(Int32.MinValue, Int32.MinValue, Int32.MinValue);

        public void includePoint(vec3 point)
        {
            if (point.X < Min.X) Min.X = point.X;
            if (point.Y < Min.Y) Min.Y = point.Y;
            if (point.Z < Min.Z) Min.Z = point.Z;

            if (point.X > Max.X) Max.X = point.X;
            if (point.Y > Max.Y) Max.Y = point.Y;
            if (point.Z > Max.Z) Max.Z = point.Z;
        }

        public void includeBox(box3 box)
        {
            includePoint(box.Min);
            includePoint(box.Max);
        }
        public void TransformByMatrix(mat4 matrix)
        {
            mat4.multiplyVec3(matrix, Min, Min);
            mat4.multiplyVec3(matrix, Max, Max);
        }
    }
}
