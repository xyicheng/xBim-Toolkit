using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;
using Xbim.XbimExtensions.SelectTypes;

namespace Xbim.Tests
{
    [DeploymentItem(TestSourceFileIfc, Root)]
    [TestClass]
    public class XbimInstanceCollectionTests
    {
        const string Root = "TestSourceFiles";
        const string Temp = "Temp";
        const string TestSourceFileIfc = Root + "/Ref.ifc";

        [ClassInitialize]
        public static void LoadModel(TestContext context)
        {
            Directory.CreateDirectory(Path.Combine(Root, Temp));    
        }

        [TestMethod]
        public void AllowsSearchingExpressSelectGenerics()
        {
            IModel m = OpenIfcModel();
            int iC = CountMaterialSelect(m);
            int iExpressSelectCount = -1;
            try
            {
                var v = m.Instances.OfType<IfcMaterialSelect>(true);
                iExpressSelectCount = v.Count();
            }
            finally
            {
                m.Close();
            }
            Assert.AreEqual(iExpressSelectCount, iC);
        }

        [TestMethod]
        public void AllowsSearchingExpressSelectViaString()
        {
            IModel m = OpenIfcModel();
            int iC = CountMaterialSelect(m);
            int iExpressSelectCount = -1;
            try
            {
                var v = m.Instances.OfType("IfcMaterialSelect", true);
                iExpressSelectCount = v.Count();                
            }
            finally
            {
                m.Close();
            }
            Assert.AreEqual(iExpressSelectCount, iC);
        }

        private int CountMaterialSelect(IModel mbox)
        {
            int tot = 0;
            tot += mbox.Instances.OfType<IfcMaterial>().Count();
            tot += mbox.Instances.OfType<IfcMaterialList>().Count();
            tot += mbox.Instances.OfType<IfcMaterialLayer>().Count();
            tot += mbox.Instances.OfType<IfcMaterialLayerSet>().Count();
            tot += mbox.Instances.OfType<IfcMaterialLayerSetUsage>().Count();
            return tot;
        }

        public IModel  OpenIfcModel()
        {
            IModel model = new XbimModel();
            string xbimName = Path.ChangeExtension(TestSourceFileIfc, "xbim");
            model.CreateFrom(TestSourceFileIfc, xbimName);
            model.Close();
            model = new XbimModel();
            model.Open(xbimName);
            return model;
        }
    }
}
