using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;
using Xbim.Ifc2x3.ActorResource;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Tests
{
    [TestClass]
    [DeploymentItem(ModelA, Root)]
    [DeploymentItem(ModelB, Root)]
    [DeploymentItem(ModelC, Root)]
    public class Federation_Tests
    {
        private const string Root = "TestSourceFiles";
        private const string ModelA = Root + @"\BIM Logo-ExclaimationBody.xBIM";
        private const string ModelB = Root + @"\BIM Logo-LetterB.xBIM";
        private const string ModelC = Root + @"\BIM Logo-LetterM.xBIM";

        /// <summary>
        /// Creates anew Federation with three models A,B and C
        /// </summary>
        [TestMethod]
        public void CreateFederation()
        {
            using (XbimModel fedModel = XbimModel.CreateTemporaryModel())
            {
                fedModel.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                using (var txn = fedModel.BeginTransaction())
                {
                    fedModel.IfcProject.Name = "Federation Project Name";
                    txn.Commit();
                }
                //now add federated models
                fedModel.AddModelReference(ModelA, "The Architects Name", IfcRole.Architect);
                fedModel.AddModelReference(ModelB, "The Owners Name", IfcRole.BuildingOwner);
                fedModel.AddModelReference(ModelC, "The Cost Consultants Name", IfcRole.UserDefined, "Cost Consultant");
                fedModel.SaveAs("Federated Model", XbimStorageType.IFC);
            } //close and automatically delete the temporary database
            //Now open the Ifc file and see what we have
            using (XbimModel fed = new XbimModel())
            {
                fed.CreateFrom("Federated Model.ifc", "Federated Model.xBIMF"); //use xBIMF to help us distinguish
                fed.Open("Federated Model.xBIMF", XbimExtensions.XbimDBAccess.Read);

                //check the various ways of access objects give consistent results.
                long localInstances = fed.InstancesLocal.Count;
                long totalInstances = fed.Instances.Count;
                long refInstancesCount = 0;
                foreach (var refModel in fed.ReferencedModels)
                {
                    refInstancesCount += refModel.Model.Instances.Count;
                }

                Assert.IsTrue(totalInstances == refInstancesCount + localInstances);

                long enumeratingInstancesCount = 0;
                foreach (IPersistIfcEntity item in fed.Instances)
                {
                    enumeratingInstancesCount++;
                }
                Assert.IsTrue(totalInstances == enumeratingInstancesCount);

                long fedProjectCount = fed.Instances.OfType<IfcProject>().Count();
                long localProjectCount = fed.InstancesLocal.OfType<IfcProject>().Count();
                Assert.IsTrue(fedProjectCount == 4);
                Assert.IsTrue(localProjectCount == 1);
            }
        }
    }
}
