using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using glMatrix;
using System.Collections.Generic;
using OpenTK;

namespace MatrixLibraryTests
{
    [TestClass]
    public class vec3Tests
    {
        [TestMethod]
        public void Vec3Check()
        {
            float[] vals = GetVals(3);
            vec3 vec1 = vec3.create(vals);
            Assert.AreEqual(vec1.X, vals[0]);
            Assert.AreEqual(vec1.Y, vals[1]);
            Assert.AreEqual(vec1.Z, vals[2]);
        }
        [TestMethod]
        public void Vec3MatchesVector3DCheck()
        {
            vec3 v;
            Vector3 mv;
            CreateVec(out v, out mv);
            AreEqual(v, mv);
        }
        [TestMethod]
        public void VectorAdd()
        {
            vec3 v;
            Vector3 mv;
            CreateVec(out v, out mv);

            vec3 v2;
            Vector3 mv2;
            CreateVec(out v2, out mv2);

            AreEqual(vec3.add(v, v2), Vector3.Add(mv, mv2));
        }
        [TestMethod]
        public void VectorSubtract()
        {
            vec3 v;
            Vector3 mv;
            CreateVec(out v, out mv);

            vec3 v2;
            Vector3 mv2;
            CreateVec(out v2, out mv2);

            AreEqual(vec3.subtract(v, v2), Vector3.Subtract(mv, mv2));
        }
        [TestMethod]
        public void VectorMultiply()
        {
            vec3 v;
            Vector3 mv;
            CreateVec(out v, out mv);

            vec3 v2;
            Vector3 mv2;
            CreateVec(out v2, out mv2);

            AreEqual(vec3.multiply(v, v2), Vector3.Multiply(mv, mv2));
        }
        private void AreEqual(vec3 v, Vector3 mv)
        {
            Assert.AreEqual(v.X, mv.X, epsilon);
            Assert.AreEqual(v.Y, mv.Y, epsilon);
            Assert.AreEqual(v.Z, mv.Z, epsilon);
        }
        private void CreateVec(out vec3 v, out Vector3 mv)
        {
            float[] vals = GetVals(3);
            v = vec3.create(vals);
            mv = new Vector3(vals[0], vals[1], vals[2]);
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
        private Single epsilon = 0.001f;
    }
}
