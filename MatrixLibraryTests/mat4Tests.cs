using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using glMatrix;
using OpenTK;

namespace MatrixLibraryTests
{
    [TestClass]
    public class mat4Tests
    {
        [TestMethod]
        public void MatrixCreate()
        {
            mat4 v;
            Matrix4 mv;
            CreateMat(out v, out mv);

            AreEqual(v, mv);
        }

        [TestMethod]
        public void CreatePerspectiveMatrix()
        {
            float fov = 65f;
            float aspect = 1.65f;
            float near = 1.0f;
            float far = 4096f;

            Matrix4 m4 = OpenTK.Matrix4.CreatePerspectiveFieldOfView(fov * DEG2RAD, aspect, near, far);

            mat4 m = mat4.perspective(fov, aspect, near, far);

            AreEqual(m, m4);
        }

        [TestMethod]
        public void MatrixMultiply()
        {
            mat4 v;
            Matrix4 mv;
            CreateMat(out v, out mv);

            mat4 v2;
            Matrix4 mv2;
            CreateMat(out v2, out mv2);

            //note the mats are reversed - because we do b*a, and Matrix4 does a*b (and matrix multiplication is not commutative)
            AreEqual(mat4.multiply(v2, v), Matrix4.Mult(mv, mv2));
        }
        [TestMethod]
        public void MatrixRotateX()
        {
            mat4 v = mat4.identity();
            Single[] angle = GetVals(1);
            AreEqual(mat4.rotateX(v, angle[0]), Matrix4.CreateRotationX(angle[0]));
        }
        [TestMethod]
        public void MatrixRotateY()
        {
            mat4 v = mat4.identity();
            Single[] angle = GetVals(1);
            AreEqual(mat4.rotateY(v, angle[0]), Matrix4.CreateRotationY(angle[0]));
        }
        [TestMethod]
        public void MatrixRotateZ()
        {
            mat4 v = mat4.identity();
            Single[] angle = GetVals(1);
            AreEqual(mat4.rotateZ(v, angle[0]), Matrix4.CreateRotationZ(angle[0]));
        }
        [TestMethod]
        public void MatrixPerspective()
        {
            mat4 v = mat4.perspective(65f, 1024 / 768, 1.0f, 4096f);
            Matrix4 mv = Matrix4.CreatePerspectiveFieldOfView(65 * DEG2RAD, 1024 / 768, 1.0f, 4096f);
            AreEqual(v, mv);
        }
        [TestMethod]
        public void MultiplyVec3()
        {

            vec3 v = vec3.create(1, 2, 3);
            mat4 m = mat4.create();
            vec3 v2 = mat4.multiplyVec3(m, v);

            Assert.AreEqual(v, v2);
        }
        [TestMethod]
        public void MatrixTranspose()
        {
            mat4 v;
            Matrix4 mv;
            CreateMat(out v, out mv);

            mat4 v2 = mat4.identity();
            Matrix4 mv2 = new Matrix4();

            mat4.transpose(v, v2);
            mv2 = Matrix4.Transpose(mv);

            AreEqual(v2, mv2);
        }
        private void AreEqual(mat4 m, Matrix4 mv)
        {
            Assert.AreEqual(m.m00, mv.M11, epsilon);
            Assert.AreEqual(m.m01, mv.M12, epsilon);
            Assert.AreEqual(m.m02, mv.M13, epsilon);
            Assert.AreEqual(m.m03, mv.M14, epsilon);

            Assert.AreEqual(m.m10, mv.M21, epsilon);
            Assert.AreEqual(m.m11, mv.M22, epsilon);
            Assert.AreEqual(m.m12, mv.M23, epsilon);
            Assert.AreEqual(m.m13, mv.M24, epsilon);

            Assert.AreEqual(m.m20, mv.M31, epsilon);
            Assert.AreEqual(m.m21, mv.M32, epsilon);
            Assert.AreEqual(m.m22, mv.M33, epsilon);
            Assert.AreEqual(m.m23, mv.M34, epsilon);

            Assert.AreEqual(m.m30, mv.M41, epsilon);
            Assert.AreEqual(m.m31, mv.M42, epsilon);
            Assert.AreEqual(m.m32, mv.M43, epsilon);
            Assert.AreEqual(m.m33, mv.M44, epsilon);
        }
        private Single[] GetVals(int p)
        {
            Random r = new Random((Int32)DateTime.UtcNow.Ticks);

            List<Single> vals = new List<Single>(p);
            for (var i = 0; i < p; i++)
            {
                vals.Add((Single)(r.NextDouble()));
            }
            return vals.ToArray();
        }
        private void CreateMat(out mat4 m, out Matrix4 mv)
        {
            float[] vals = GetVals(16);
            m = mat4.create(vals);
            mv = new Matrix4(
                vals[0],
                vals[1],
                vals[2],
                vals[3],
                vals[4],
                vals[5],
                vals[6],
                vals[7],
                vals[8],
                vals[9],
                vals[10],
                vals[11],
                vals[12],
                vals[13],
                vals[14],
                vals[15]
                );
        }
        private Single epsilon = 0.001f;
        private Single DEG2RAD = (Single)(Math.PI / 180);
    }
}
