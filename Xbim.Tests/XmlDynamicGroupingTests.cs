using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;
using Xbim.IO.DynamicGrouping;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Tests
{
    [TestClass]
    // [DeploymentItem(TestSourceFileIfc, Root)]
    [DeploymentItem(TestXbimFile, ModelFolder)]
    [DeploymentItem(XmlNRM_ClassFile, XmlFolder)]
    [DeploymentItem(XmlNRM_RulesFile, XmlFolder)]
    public class XmlDynamicGroupingTests
    {
        const string ModelFolder = "TestSourceFiles";
        const string TestXbimFile = ModelFolder + "/Duplex.xbim";

        const string XmlFolder = "DynamicGrouping";
        const string XmlNRM_ClassFile = XmlFolder + "/NRM clssification.xml";
        const string XmlNRM_RulesFile = XmlFolder + "/NRM2IFC.xml";

        [TestMethod]
        public void GroupNRM()
        {
            Debug.WriteLine(Directory.GetCurrentDirectory());

            XbimModel model = new XbimModel();
            model.Open(TestXbimFile, XbimExtensions.XbimDBAccess.ReadWrite);
            
            GroupsFromXml g = new GroupsFromXml(model);
            g.CreateGroups(XmlNRM_ClassFile);
            
            GroupingByXml grouping = new GroupingByXml(model);
            grouping.GroupElements(XmlNRM_RulesFile);
  
            model.Close();
        }
    }
}
