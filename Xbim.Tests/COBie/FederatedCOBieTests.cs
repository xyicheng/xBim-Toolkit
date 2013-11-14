using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.COBie;
using Xbim.COBie.Serialisers;
using Xbim.IO;

namespace Xbim.Tests.COBie
{
    [DeploymentItem(xbimFile, Root)]
    [DeploymentItem(cobieFile, Root)]
    [TestClass]
    public class FederatedCOBieTests
    {
        private const string Root = "TestSourceFiles";

        private const string xbimFile = Root + @"\Duplex MEP.xbim";
        private const string cobieFile = Root + @"\Duplex MEP.xcobie";

        [TestMethod]
        public void TestRoles()
        {
            COBieBinaryDeserialiser deserialiser = new COBieBinaryDeserialiser(cobieFile);
            COBieWorkbook workbook = deserialiser.Deserialise();
            using (XbimModel model = new XbimModel())
            {
                model.Open(xbimFile);
                workbook.ValidateRoles(model, COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing);
            }

            Assert.IsNotNull(workbook);
        }
    }
}
