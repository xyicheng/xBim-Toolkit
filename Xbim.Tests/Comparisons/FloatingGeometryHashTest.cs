using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xbim.Tests.Comparisons
{
    [TestClass]
    public class FloatingGeometryHashTests
    {
        [TestMethod]
        public void FloatingGeometryHashTest()
        {
            var d = 0.000015;
            var res = Math.Floor(Math.Log10(Math.Abs(d)));
        }
    }
}
