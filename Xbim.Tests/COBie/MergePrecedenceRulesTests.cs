using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;
using Xbim.Ifc2x3.SharedBldgServiceElements;
using Xbim.COBie.Serialisers.XbimSerialiser;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.BuildingControlsDomain;
using Xbim.COBie;
using Xbim.Ifc2x3.HVACDomain;
using Xbim.Ifc2x3.ElectricalDomain;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.SharedFacilitiesElements;
using Xbim.Ifc2x3.PlumbingFireProtectionDomain;

namespace Xbim.Tests.COBie
{
    [TestClass]
    public class MergePrecedenceRulesTests
    {
        [TestMethod]
        public void DistributionControlElement_IfcFlowController_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcDistributionControlElement ifcDistributionControlElement;
                using (var txn = model.BeginTransaction())
                {
                    IfcFlowController ifcFlowController = model.Instances.New<IfcFlowController>();
                    IfcRelFlowControlElements ifcRelFlowControlElements = model.Instances.New<IfcRelFlowControlElements>();
                    ifcDistributionControlElement = model.Instances.New<IfcDistributionControlElement>();

                    //set up relationship between IfcFlowController and IfcDistributionControlElement
                    ifcRelFlowControlElements.RelatingFlowElement = ifcFlowController;
                    ifcRelFlowControlElements.RelatedControlElements.Add(ifcDistributionControlElement);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                fileRoles = COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing;//should pass with Electrical or Plumbing
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Plumbing; //should pass with Plumbing
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));
            }
        }

        [TestMethod]
        public void DistributionControlElement_ifcActuatorType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcDistributionControlElement ifcDistributionControlElement;
                using (var txn = model.BeginTransaction())
                {
                    ifcDistributionControlElement = model.Instances.New<IfcDistributionControlElement>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcActuatorType ifcActuatorType = model.Instances.New<IfcActuatorType>();
                    ifcActuatorType.PredefinedType = IfcActuatorTypeEnum.ELECTRICACTUATOR;

                    //set up relationship between IfcActuatorType and IfcDistributionControlElement
                    ifcRelDefinesByType.RelatingType = ifcActuatorType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcDistributionControlElement);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                fileRoles = COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing;//should pass with Electrical or Plumbing
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Plumbing; //should pass with Plumbing
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));
                
            }
        }

        [TestMethod]
        public void DistributionControlElement_ifcAlarmType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcDistributionControlElement ifcDistributionControlElement;
                using (var txn = model.BeginTransaction())
                {
                    ifcDistributionControlElement = model.Instances.New<IfcDistributionControlElement>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcAlarmType ifcAlarmType = model.Instances.New<IfcAlarmType>();
                    ifcAlarmType.PredefinedType = IfcAlarmTypeEnum.SIREN;

                    //set up relationship between IfcAlarmType and IfcDistributionControlElement
                    ifcRelDefinesByType.RelatingType = ifcAlarmType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcDistributionControlElement);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural | COBieMergeRoles.Plumbing;//should fail with Architectural or Plumbing
                Assert.IsFalse(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.FireProtection;//should pass with Mechanical, Electrical or fireProtection
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

            }
        }

        [TestMethod]
        public void DistributionControlElement_ifcControllerType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcDistributionControlElement ifcDistributionControlElement;
                using (var txn = model.BeginTransaction())
                {
                    ifcDistributionControlElement = model.Instances.New<IfcDistributionControlElement>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcControllerType ifcControllerType = model.Instances.New<IfcControllerType>();
                    ifcControllerType.PredefinedType = IfcControllerTypeEnum.TWOPOSITION;

                    //set up relationship between IfcControllerType and IfcDistributionControlElement
                    ifcRelDefinesByType.RelatingType = ifcControllerType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcDistributionControlElement);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural ;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical, Electrical, Plumbing or FireProtection
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

            }
        }

        [TestMethod]
        public void DistributionControlElement_ifcFlowInstrumentType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcDistributionControlElement ifcDistributionControlElement;
                using (var txn = model.BeginTransaction())
                {
                    ifcDistributionControlElement = model.Instances.New<IfcDistributionControlElement>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcFlowInstrumentType ifcFlowInstrumentType = model.Instances.New<IfcFlowInstrumentType>();
                    ifcFlowInstrumentType.PredefinedType = IfcFlowInstrumentTypeEnum.FREQUENCYMETER;

                    //set up relationship between IfcControllerType and IfcDistributionControlElement
                    ifcRelDefinesByType.RelatingType = ifcFlowInstrumentType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcDistributionControlElement);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

            }
        }

        [TestMethod]
        public void DistributionControlElement_ifcFlowInstrumentType_Test2()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcDistributionControlElement ifcDistributionControlElement;
                using (var txn = model.BeginTransaction())
                {
                    ifcDistributionControlElement = model.Instances.New<IfcDistributionControlElement>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcFlowInstrumentType ifcFlowInstrumentType = model.Instances.New<IfcFlowInstrumentType>();
                    ifcFlowInstrumentType.PredefinedType = IfcFlowInstrumentTypeEnum.PRESSUREGAUGE;

                    //set up relationship between IfcControllerType and IfcDistributionControlElement
                    ifcRelDefinesByType.RelatingType = ifcFlowInstrumentType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcDistributionControlElement);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

            }
        }

        [TestMethod]
        public void DistributionControlElement_ifcSensorType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcDistributionControlElement ifcDistributionControlElement;
                IfcSensorType ifcSensorType;
                using (var txn = model.BeginTransaction())
                {
                    ifcDistributionControlElement = model.Instances.New<IfcDistributionControlElement>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    ifcSensorType = model.Instances.New<IfcSensorType>();
                    ifcSensorType.PredefinedType = IfcSensorTypeEnum.FIRESENSOR;

                    //set up relationship between IfcSensorType and IfcDistributionControlElement
                    ifcRelDefinesByType.RelatingType = ifcSensorType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcDistributionControlElement);
                    txn.Commit();
                }


                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with FireProtection
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.FireProtection; //should pass with FireProtection
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));
                using (var txn = model.BeginTransaction())
                {
                    ifcSensorType.PredefinedType = IfcSensorTypeEnum.FLOWSENSOR;
                    txn.Commit();
                }
                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                using (var txn = model.BeginTransaction())
                {
                    ifcSensorType.PredefinedType = IfcSensorTypeEnum.GASSENSOR;
                    txn.Commit();
                }
                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                using (var txn = model.BeginTransaction())
                {
                    ifcSensorType.PredefinedType = IfcSensorTypeEnum.HEATSENSOR;
                    txn.Commit();
                }
                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.FireProtection; //should pass with FireProtection
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                using (var txn = model.BeginTransaction())
                {
                    ifcSensorType.PredefinedType = IfcSensorTypeEnum.HUMIDITYSENSOR;
                    txn.Commit();
                }
                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                using (var txn = model.BeginTransaction())
                {
                    ifcSensorType.PredefinedType = IfcSensorTypeEnum.LIGHTSENSOR;
                    txn.Commit();
                }
                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));
                
                using (var txn = model.BeginTransaction())
                {
                    ifcSensorType.PredefinedType = IfcSensorTypeEnum.MOISTURESENSOR;
                    txn.Commit();
                }
                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                using (var txn = model.BeginTransaction())
                {
                    ifcSensorType.PredefinedType = IfcSensorTypeEnum.MOVEMENTSENSOR;
                    txn.Commit();
                }
                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                using (var txn = model.BeginTransaction())
                {
                    ifcSensorType.PredefinedType = IfcSensorTypeEnum.PRESSURESENSOR;
                    txn.Commit();
                }
                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));
                
                using (var txn = model.BeginTransaction())
                {
                    ifcSensorType.PredefinedType = IfcSensorTypeEnum.SMOKESENSOR;
                    txn.Commit();
                }
                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                using (var txn = model.BeginTransaction())
                {
                    ifcSensorType.PredefinedType = IfcSensorTypeEnum.SOUNDSENSOR;
                    txn.Commit();
                }
                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));

                using (var txn = model.BeginTransaction())
                {
                    ifcSensorType.PredefinedType = IfcSensorTypeEnum.TEMPERATURESENSOR;
                    txn.Commit();
                }
                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionControlElement, fileRoles));
            }
        }

        [TestMethod]
        public void FlowController_ifcDamperType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowController ifcFlowController;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowController = model.Instances.New<IfcFlowController>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcDamperType ifcDamperType = model.Instances.New<IfcDamperType>();
                    ifcDamperType.PredefinedType = IfcDamperTypeEnum.BLASTDAMPER;

                    //set up relationship between IfcDamperType and IfcFlowController
                    ifcRelDefinesByType.RelatingType = ifcDamperType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowController);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowController, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowController, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowController, fileRoles));

            }
        }

        [TestMethod]
        public void FlowController_ifcElectricDistributionPoint_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcElectricDistributionPoint ifcElectricDistributionPoint;
                using (var txn = model.BeginTransaction())
                {
                    ifcElectricDistributionPoint = model.Instances.New<IfcElectricDistributionPoint>();
                    ifcElectricDistributionPoint.DistributionPointFunction = IfcElectricDistributionPointFunctionEnum.USERDEFINED;
                    ifcElectricDistributionPoint.UserDefinedFunction = "Circuit";
                    
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcElectricDistributionPoint, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcElectricDistributionPoint, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcElectricDistributionPoint, fileRoles));

            }
        }

        [TestMethod]
        public void FlowController_ifcElectricTimeControlType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowController ifcFlowController;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowController = model.Instances.New<IfcFlowController>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcElectricTimeControlType ifcElectricTimeControlType = model.Instances.New<IfcElectricTimeControlType>();
                    ifcElectricTimeControlType.PredefinedType = IfcElectricTimeControlTypeEnum.TIMEDELAY;

                    //set up relationship between IfcElectricTimeControlType and IfcFlowController
                    ifcRelDefinesByType.RelatingType = ifcElectricTimeControlType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowController);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowController, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowController, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowController, fileRoles));

            }
        }

        [TestMethod]
        public void FlowController_ifcProtectiveDeviceType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowController ifcFlowController;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowController = model.Instances.New<IfcFlowController>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcProtectiveDeviceType ifcProtectiveDeviceType = model.Instances.New<IfcProtectiveDeviceType>();
                    ifcProtectiveDeviceType.PredefinedType = IfcProtectiveDeviceTypeEnum.CIRCUITBREAKER;

                    //set up relationship between IfcProtectiveDeviceType and IfcFlowController
                    ifcRelDefinesByType.RelatingType = ifcProtectiveDeviceType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowController);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowController, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowController, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowController, fileRoles));

            }
        }

        [TestMethod]
        public void FlowController_ifcSwitchingDeviceType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowController ifcFlowController;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowController = model.Instances.New<IfcFlowController>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcSwitchingDeviceType ifcSwitchingDeviceType = model.Instances.New<IfcSwitchingDeviceType>();
                    ifcSwitchingDeviceType.PredefinedType = IfcSwitchingDeviceTypeEnum.EMERGENCYSTOP;

                    //set up relationship between IfcSwitchingDeviceType and IfcFlowController
                    ifcRelDefinesByType.RelatingType = ifcSwitchingDeviceType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowController);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowController, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowController, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowController, fileRoles));

            }
        }

        [TestMethod]
        public void FlowController_ifcValveType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowController ifcFlowController;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowController = model.Instances.New<IfcFlowController>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcValveType ifcValveType = model.Instances.New<IfcValveType>();
                    ifcValveType.PredefinedType = IfcValveTypeEnum.ANTIVACUUM;

                    //set up relationship between IfcSwitchingDeviceType and IfcFlowController
                    ifcRelDefinesByType.RelatingType = ifcValveType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowController);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowController, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Plumbing
                Assert.IsTrue(MergeTool.Merge(ifcFlowController, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Plumbing; //should pass with Plumbing
                Assert.IsTrue(MergeTool.Merge(ifcFlowController, fileRoles));

            }
        }

        [TestMethod]
        public void FlowTerminal_ifcAirTerminalType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowTerminal ifcFlowTerminal;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowTerminal = model.Instances.New<IfcFlowTerminal>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcAirTerminalType ifcAirTerminalType = model.Instances.New<IfcAirTerminalType>();
                    ifcAirTerminalType.PredefinedType = IfcAirTerminalTypeEnum.REGISTER;

                    //set up relationship between IfcAirTerminalType and IfcFlowTerminal
                    ifcRelDefinesByType.RelatingType = ifcAirTerminalType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowTerminal);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

            }
        }

        [TestMethod]
        public void FlowTerminal_ifcElectricApplianceType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowTerminal ifcFlowTerminal;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowTerminal = model.Instances.New<IfcFlowTerminal>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcElectricApplianceType ifcElectricApplianceType = model.Instances.New<IfcElectricApplianceType>();
                    ifcElectricApplianceType.PredefinedType = IfcElectricApplianceTypeEnum.FRIDGE_FREEZER;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcElectricApplianceType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowTerminal);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

            }
        }

        [TestMethod]
        public void FlowTerminal_ifcAirTerminalBoxType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowTerminal ifcFlowTerminal;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowTerminal = model.Instances.New<IfcFlowTerminal>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcAirTerminalBoxType ifcAirTerminalBoxType = model.Instances.New<IfcAirTerminalBoxType>();
                    ifcAirTerminalBoxType.PredefinedType = IfcAirTerminalBoxTypeEnum.VARIABLEFLOWPRESSUREDEPENDANT;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcAirTerminalBoxType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowTerminal);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

            }
        }
        [TestMethod]
        public void FlowTerminal_ifcLampType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowTerminal ifcFlowTerminal;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowTerminal = model.Instances.New<IfcFlowTerminal>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcLampType ifcLampType = model.Instances.New<IfcLampType>();
                    ifcLampType.PredefinedType = IfcLampTypeEnum.FLUORESCENT;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcLampType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowTerminal);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

            }
        }

        [TestMethod]
        public void FlowTerminal_ifcLightFixtureType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowTerminal ifcFlowTerminal;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowTerminal = model.Instances.New<IfcFlowTerminal>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcLightFixtureType ifcLightFixtureType = model.Instances.New<IfcLightFixtureType>();
                    ifcLightFixtureType.PredefinedType = IfcLightFixtureTypeEnum.DIRECTIONSOURCE;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcLightFixtureType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowTerminal);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

            }
        }

        [TestMethod]
        public void FlowTerminal_ifcOutletType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowTerminal ifcFlowTerminal;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowTerminal = model.Instances.New<IfcFlowTerminal>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcOutletType ifcOutletType = model.Instances.New<IfcOutletType>();
                    ifcOutletType.PredefinedType = IfcOutletTypeEnum.POWEROUTLET;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcOutletType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowTerminal);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

            }
        }

        [TestMethod]
        public void FlowTerminal_ifcSanitaryTerminalType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowTerminal ifcFlowTerminal;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowTerminal = model.Instances.New<IfcFlowTerminal>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcSanitaryTerminalType ifcSanitaryTerminalType = model.Instances.New<IfcSanitaryTerminalType>();
                    ifcSanitaryTerminalType.PredefinedType = IfcSanitaryTerminalTypeEnum.BIDET;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcSanitaryTerminalType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowTerminal);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Plumbing; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

            }
        }

        [TestMethod]
        public void FlowTerminal_ifcStackTerminalType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowTerminal ifcFlowTerminal;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowTerminal = model.Instances.New<IfcFlowTerminal>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcStackTerminalType ifcStackTerminalType = model.Instances.New<IfcStackTerminalType>();
                    ifcStackTerminalType.PredefinedType = IfcStackTerminalTypeEnum.COWL;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcStackTerminalType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowTerminal);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

            }
        }

        [TestMethod]
        public void FlowTerminal_ifcWasteTerminalType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowTerminal ifcFlowTerminal;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowTerminal = model.Instances.New<IfcFlowTerminal>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcWasteTerminalType ifcWasteTerminalType = model.Instances.New<IfcWasteTerminalType>();
                    ifcWasteTerminalType.PredefinedType = IfcWasteTerminalTypeEnum.FLOORWASTE;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcWasteTerminalType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowTerminal);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTerminal, fileRoles));

            }
        }

        [TestMethod]
        public void EnergyConversionDevice_ifcAirToAirHeatRecoveryType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcEnergyConversionDevice ifcEnergyConversionDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcEnergyConversionDevice = model.Instances.New<IfcEnergyConversionDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcAirToAirHeatRecoveryType ifcAirToAirHeatRecoveryType = model.Instances.New<IfcAirToAirHeatRecoveryType>();
                    ifcAirToAirHeatRecoveryType.PredefinedType = IfcAirToAirHeatRecoveryTypeEnum.HEATPIPE;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcAirToAirHeatRecoveryType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcEnergyConversionDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

            }
        }

        [TestMethod]
        public void EnergyConversionDevice_ifcBoilerType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcEnergyConversionDevice ifcEnergyConversionDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcEnergyConversionDevice = model.Instances.New<IfcEnergyConversionDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcBoilerType ifcBoilerType = model.Instances.New<IfcBoilerType>();
                    ifcBoilerType.PredefinedType = IfcBoilerTypeEnum.STEAM;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcBoilerType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcEnergyConversionDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

            }
        }

        [TestMethod]
        public void EnergyConversionDevice_ifcChillerType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcEnergyConversionDevice ifcEnergyConversionDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcEnergyConversionDevice = model.Instances.New<IfcEnergyConversionDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcChillerType ifcChillerType = model.Instances.New<IfcChillerType>();
                    ifcChillerType.PredefinedType = IfcChillerTypeEnum.HEATRECOVERY;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcChillerType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcEnergyConversionDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

            }
        }

        [TestMethod]
        public void EnergyConversionDevice_ifcCoilType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcEnergyConversionDevice ifcEnergyConversionDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcEnergyConversionDevice = model.Instances.New<IfcEnergyConversionDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcCoilType ifcCoilType = model.Instances.New<IfcCoilType>();
                    ifcCoilType.PredefinedType = IfcCoilTypeEnum.GASHEATINGCOIL;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcCoilType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcEnergyConversionDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

            }
        }

        [TestMethod]
        public void EnergyConversionDevice_ifcCondenserType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcEnergyConversionDevice ifcEnergyConversionDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcEnergyConversionDevice = model.Instances.New<IfcEnergyConversionDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcCondenserType ifcCondenserType = model.Instances.New<IfcCondenserType>();
                    ifcCondenserType.PredefinedType = IfcCondenserTypeEnum.WATERCOOLEDBRAZEDPLATE;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcCondenserType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcEnergyConversionDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

            }
        }

        [TestMethod]
        public void EnergyConversionDevice_ifcElectricGeneratorType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcEnergyConversionDevice ifcEnergyConversionDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcEnergyConversionDevice = model.Instances.New<IfcEnergyConversionDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcElectricGeneratorType ifcElectricGeneratorType = model.Instances.New<IfcElectricGeneratorType>();
                    ifcElectricGeneratorType.PredefinedType = IfcElectricGeneratorTypeEnum.NOTDEFINED;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcElectricGeneratorType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcEnergyConversionDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

            }
        }

        [TestMethod]
        public void EnergyConversionDevice_ifcElectricMotorType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcEnergyConversionDevice ifcEnergyConversionDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcEnergyConversionDevice = model.Instances.New<IfcEnergyConversionDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcElectricMotorType ifcElectricMotorType = model.Instances.New<IfcElectricMotorType>();
                    ifcElectricMotorType.PredefinedType = IfcElectricMotorTypeEnum.INDUCTION;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcElectricMotorType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcEnergyConversionDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

            }
        }

        [TestMethod]
        public void EnergyConversionDevice_ifcEnergyConversionDeviceType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcEnergyConversionDevice ifcEnergyConversionDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcEnergyConversionDevice = model.Instances.New<IfcEnergyConversionDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcBoilerType ifcBoilerType = model.Instances.New<IfcBoilerType>();
                    ifcBoilerType.PredefinedType = IfcBoilerTypeEnum.USERDEFINED;
                    ifcBoilerType.ElementType = "Furnaces";

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcBoilerType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcEnergyConversionDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

            }
        }

        [TestMethod]
        public void EnergyConversionDevice_ifcEvaporativeCoolerType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcEnergyConversionDevice ifcEnergyConversionDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcEnergyConversionDevice = model.Instances.New<IfcEnergyConversionDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcEvaporativeCoolerType ifcEvaporativeCoolerType = model.Instances.New<IfcEvaporativeCoolerType>();
                    ifcEvaporativeCoolerType.PredefinedType = IfcEvaporativeCoolerTypeEnum.DIRECTEVAPORATIVESLINGERSPACKAGEDAIRCOOLER;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcEvaporativeCoolerType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcEnergyConversionDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

            }
        }

        [TestMethod]
        public void EnergyConversionDevice_ifcEvaporatorType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcEnergyConversionDevice ifcEnergyConversionDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcEnergyConversionDevice = model.Instances.New<IfcEnergyConversionDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcEvaporatorType ifcEvaporatorType = model.Instances.New<IfcEvaporatorType>();
                    ifcEvaporatorType.PredefinedType = IfcEvaporatorTypeEnum.FLOODEDSHELLANDTUBE;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcEvaporatorType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcEnergyConversionDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

            }
        }

        [TestMethod]
        public void EnergyConversionDevice_ifcMotorConnectionType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcEnergyConversionDevice ifcEnergyConversionDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcEnergyConversionDevice = model.Instances.New<IfcEnergyConversionDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcMotorConnectionType ifcMotorConnectionType = model.Instances.New<IfcMotorConnectionType>();
                    ifcMotorConnectionType.PredefinedType = IfcMotorConnectionTypeEnum.DIRECTDRIVE;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcMotorConnectionType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcEnergyConversionDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

            }
        }

        [TestMethod]
        public void EnergyConversionDevice_ifcTransformerType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcEnergyConversionDevice ifcEnergyConversionDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcEnergyConversionDevice = model.Instances.New<IfcEnergyConversionDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcTransformerType ifcTransformerType = model.Instances.New<IfcTransformerType>();
                    ifcTransformerType.PredefinedType = IfcTransformerTypeEnum.FREQUENCY;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcTransformerType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcEnergyConversionDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

            }
        }

        [TestMethod]
        public void EnergyConversionDevice_ifcUnitaryEquipmentType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcEnergyConversionDevice ifcEnergyConversionDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcEnergyConversionDevice = model.Instances.New<IfcEnergyConversionDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcUnitaryEquipmentType ifcUnitaryEquipmentType = model.Instances.New<IfcUnitaryEquipmentType>();
                    ifcUnitaryEquipmentType.PredefinedType = IfcUnitaryEquipmentTypeEnum.AIRHANDLER;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcUnitaryEquipmentType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcEnergyConversionDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

            }
        }

        [TestMethod]
        public void EnergyConversionDevice_ifcHeatExchangerType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcEnergyConversionDevice ifcEnergyConversionDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcEnergyConversionDevice = model.Instances.New<IfcEnergyConversionDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcHeatExchangerType ifcHeatExchangerType = model.Instances.New<IfcHeatExchangerType>();
                    ifcHeatExchangerType.PredefinedType = IfcHeatExchangerTypeEnum.PLATE;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcHeatExchangerType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcEnergyConversionDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

            }
        }

        [TestMethod]
        public void EnergyConversionDevice_ifcCoolingTowerType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcEnergyConversionDevice ifcEnergyConversionDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcEnergyConversionDevice = model.Instances.New<IfcEnergyConversionDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcCoolingTowerType ifcCoolingTowerType = model.Instances.New<IfcCoolingTowerType>();
                    ifcCoolingTowerType.PredefinedType = IfcCoolingTowerTypeEnum.NATURALDRAFT;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcCoolingTowerType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcEnergyConversionDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcEnergyConversionDevice, fileRoles));

            }
        }

        [TestMethod]
        public void FlowMovingDevice_ifcCompressorType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowMovingDevice ifcFlowMovingDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowMovingDevice = model.Instances.New<IfcFlowMovingDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcCompressorType ifcCompressorType = model.Instances.New<IfcCompressorType>();
                    ifcCompressorType.PredefinedType = IfcCompressorTypeEnum.DYNAMIC;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcCompressorType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowMovingDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowMovingDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowMovingDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowMovingDevice, fileRoles));

            }
        }

        [TestMethod]
        public void FlowMovingDevice_ifcFanType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowMovingDevice ifcFlowMovingDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowMovingDevice = model.Instances.New<IfcFlowMovingDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcFanType ifcFanType = model.Instances.New<IfcFanType>();
                    ifcFanType.PredefinedType = IfcFanTypeEnum.CENTRIFUGALRADIAL;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcFanType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowMovingDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowMovingDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowMovingDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowMovingDevice, fileRoles));

            }
        }

        [TestMethod]
        public void FlowMovingDevice_ifcPumpType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowMovingDevice ifcFlowMovingDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowMovingDevice = model.Instances.New<IfcFlowMovingDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcPumpType ifcPumpType = model.Instances.New<IfcPumpType>();
                    ifcPumpType.PredefinedType = IfcPumpTypeEnum.ENDSUCTION;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcPumpType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowMovingDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowMovingDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical, Electrical, FireProtection
                Assert.IsTrue(MergeTool.Merge(ifcFlowMovingDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.FireProtection; //should pass with FireProtection
                Assert.IsTrue(MergeTool.Merge(ifcFlowMovingDevice, fileRoles));

            }
        }

        [TestMethod]
        public void Covering_ifcCoveringType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcCovering ifcCovering;
                using (var txn = model.BeginTransaction())
                {
                    ifcCovering = model.Instances.New<IfcCovering>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcCoveringType ifcCoveringType = model.Instances.New<IfcCoveringType>();
                    ifcCoveringType.PredefinedType = IfcCoveringTypeEnum.CLADDING;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcCoveringType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcCovering);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Mechanical;//should fail with Mechanical
                Assert.IsFalse(MergeTool.Merge(ifcCovering, fileRoles));

                fileRoles = COBieMergeRoles.Architectural | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Architectural
                Assert.IsTrue(MergeTool.Merge(ifcCovering, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Architectural; //should pass with Architectural
                Assert.IsTrue(MergeTool.Merge(ifcCovering, fileRoles));

            }
        }

        [TestMethod]
        public void Door_ifcDoorStyle_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcDoor ifcDoor;
                using (var txn = model.BeginTransaction())
                {
                    ifcDoor = model.Instances.New<IfcDoor>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcDoorStyle ifcDoorStyle = model.Instances.New<IfcDoorStyle>();
                    ifcDoorStyle.OperationType = IfcDoorStyleOperationEnum.DOUBLE_DOOR_FOLDING;
                    ifcDoorStyle.ConstructionType = IfcDoorStyleConstructionEnum.ALUMINIUM_WOOD;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcDoorStyle;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcDoor);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Mechanical;//should fail with Mechanical
                Assert.IsFalse(MergeTool.Merge(ifcDoor, fileRoles));

                fileRoles = COBieMergeRoles.Architectural | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Architectural
                Assert.IsTrue(MergeTool.Merge(ifcDoor, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Architectural; //should pass with Architectural
                Assert.IsTrue(MergeTool.Merge(ifcDoor, fileRoles));

            }
        }

        [TestMethod]
        public void FlowStorageDevice_ifcElectricFlowStorageDeviceType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowStorageDevice ifcFlowStorageDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowStorageDevice = model.Instances.New<IfcFlowStorageDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcElectricFlowStorageDeviceType ifcElectricFlowStorageDeviceType = model.Instances.New<IfcElectricFlowStorageDeviceType>();
                    ifcElectricFlowStorageDeviceType.PredefinedType = IfcElectricFlowStorageDeviceTypeEnum.CAPACITORBANK;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcElectricFlowStorageDeviceType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowStorageDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Mechanical;//should fail with Mechanical
                Assert.IsFalse(MergeTool.Merge(ifcFlowStorageDevice, fileRoles));

                fileRoles = COBieMergeRoles.Architectural | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Architectural
                Assert.IsTrue(MergeTool.Merge(ifcFlowStorageDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Architectural
                Assert.IsTrue(MergeTool.Merge(ifcFlowStorageDevice, fileRoles));

            }
        }

        [TestMethod]
        public void FlowTreatmentDevice_ifcFlowTreatmentDevice_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowTreatmentDevice ifcFlowTreatmentDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowTreatmentDevice = model.Instances.New<IfcFlowTreatmentDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcFilterType ifcFilterType = model.Instances.New<IfcFilterType>();
                    ifcFilterType.PredefinedType = IfcFilterTypeEnum.ODORFILTER;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcFilterType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowTreatmentDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowTreatmentDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTreatmentDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTreatmentDevice, fileRoles));

            }
        }
        [TestMethod]
        public void FlowTreatmentDevice_ifcFireSuppressionTerminalType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowTreatmentDevice ifcFlowTreatmentDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowTreatmentDevice = model.Instances.New<IfcFlowTreatmentDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcFireSuppressionTerminalType ifcFireSuppressionTerminalType = model.Instances.New<IfcFireSuppressionTerminalType>();
                    ifcFireSuppressionTerminalType.PredefinedType = IfcFireSuppressionTerminalTypeEnum.FIREHYDRANT;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcFireSuppressionTerminalType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowTreatmentDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowTreatmentDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.FireProtection | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with FireProtection
                Assert.IsTrue(MergeTool.Merge(ifcFlowTreatmentDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.FireProtection; //should pass with FireProtection
                Assert.IsTrue(MergeTool.Merge(ifcFlowTreatmentDevice, fileRoles));

            }
        }

        [TestMethod]
        public void FlowTreatmentDevice_ifcHumidifierType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowTreatmentDevice ifcFlowTreatmentDevice;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowTreatmentDevice = model.Instances.New<IfcFlowTreatmentDevice>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcHumidifierType ifcHumidifierType = model.Instances.New<IfcHumidifierType>();
                    ifcHumidifierType.PredefinedType = IfcHumidifierTypeEnum.ADIABATICPAN;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcHumidifierType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowTreatmentDevice);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowTreatmentDevice, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.FireProtection | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTreatmentDevice, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcFlowTreatmentDevice, fileRoles));

            }
        }

        [TestMethod]
        public void FurnishingElement_ifcFurnitureType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFurnishingElement ifcFurnishingElement;
                using (var txn = model.BeginTransaction())
                {
                    ifcFurnishingElement = model.Instances.New<IfcFurnishingElement>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcFurnitureType ifcFurnitureType = model.Instances.New<IfcFurnitureType>();
                    ifcFurnitureType.AssemblyPlace = IfcAssemblyPlaceEnum.FACTORY;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcFurnitureType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFurnishingElement);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Mechanical;//should fail with Mechanical
                Assert.IsFalse(MergeTool.Merge(ifcFurnishingElement, fileRoles));

                fileRoles = COBieMergeRoles.Architectural | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Architectural
                Assert.IsTrue(MergeTool.Merge(ifcFurnishingElement, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Architectural; //should pass with Architectural
                Assert.IsTrue(MergeTool.Merge(ifcFurnishingElement, fileRoles));

            }
        }
        [TestMethod]
        public void FurnishingElement_ifcSystemFurnitureElementType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFurnishingElement ifcFurnishingElement;
                using (var txn = model.BeginTransaction())
                {
                    ifcFurnishingElement = model.Instances.New<IfcFurnishingElement>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcSystemFurnitureElementType ifcSystemFurnitureElementType = model.Instances.New<IfcSystemFurnitureElementType>();
                    
                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcSystemFurnitureElementType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFurnishingElement);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Mechanical;//should fail with Mechanical
                Assert.IsFalse(MergeTool.Merge(ifcFurnishingElement, fileRoles));

                fileRoles = COBieMergeRoles.Architectural | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Architectural
                Assert.IsTrue(MergeTool.Merge(ifcFurnishingElement, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Architectural; //should pass with Architectural
                Assert.IsTrue(MergeTool.Merge(ifcFurnishingElement, fileRoles));

            }
        }

        [TestMethod]
        public void FlowFitting_ifcJunctionBoxType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcFlowFitting ifcFlowFitting;
                using (var txn = model.BeginTransaction())
                {
                    ifcFlowFitting = model.Instances.New<IfcFlowFitting>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcJunctionBoxType ifcJunctionBoxType = model.Instances.New<IfcJunctionBoxType>();
                    ifcJunctionBoxType.PredefinedType = IfcJunctionBoxTypeEnum.USERDEFINED;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcJunctionBoxType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcFlowFitting);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcFlowFitting, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowFitting, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcFlowFitting, fileRoles));

            }
        }

        [TestMethod]
        public void Roof_ifcRoof_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcRoof ifcRoof;
                using (var txn = model.BeginTransaction())
                {
                    ifcRoof = model.Instances.New<IfcRoof>();
                    ifcRoof.ShapeType = IfcRoofTypeEnum.FLAT_ROOF;
                    
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Mechanical;//should fail with Mechanical
                Assert.IsFalse(MergeTool.Merge(ifcRoof, fileRoles));

                fileRoles = COBieMergeRoles.Architectural | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Architectural
                Assert.IsTrue(MergeTool.Merge(ifcRoof, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Architectural; //should pass with Architectural
                Assert.IsTrue(MergeTool.Merge(ifcRoof, fileRoles));

            }
        }

        [TestMethod]
        public void DistributionFlowElement_ifcTubeBundleType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcDistributionFlowElement ifcDistributionFlowElement;
                using (var txn = model.BeginTransaction())
                {
                    ifcDistributionFlowElement = model.Instances.New<IfcDistributionFlowElement>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcTubeBundleType ifcTubeBundleType = model.Instances.New<IfcTubeBundleType>();
                    ifcTubeBundleType.PredefinedType = IfcTubeBundleTypeEnum.FINNED;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcTubeBundleType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcDistributionFlowElement);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcDistributionFlowElement, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionFlowElement, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with Electrical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionFlowElement, fileRoles));

            }
        }

        [TestMethod]
        public void DistributionFlowElement_ifcTankType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcDistributionFlowElement ifcDistributionFlowElement;
                using (var txn = model.BeginTransaction())
                {
                    ifcDistributionFlowElement = model.Instances.New<IfcDistributionFlowElement>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcTankType ifcTankType = model.Instances.New<IfcTankType>();
                    ifcTankType.PredefinedType = IfcTankTypeEnum.PREFORMED;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcTankType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcDistributionFlowElement);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcDistributionFlowElement, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with .Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionFlowElement, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with .Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionFlowElement, fileRoles));

            }
        }

        [TestMethod]
        public void DistributionFlowElement_ifcFlowMeterType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcDistributionFlowElement ifcDistributionFlowElement;
                using (var txn = model.BeginTransaction())
                {
                    ifcDistributionFlowElement = model.Instances.New<IfcDistributionFlowElement>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcFlowMeterType ifcFlowMeterType = model.Instances.New<IfcFlowMeterType>();
                    ifcFlowMeterType.PredefinedType = IfcFlowMeterTypeEnum.FLOWMETER;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcFlowMeterType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcDistributionFlowElement);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcDistributionFlowElement, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with .Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionFlowElement, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with .Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionFlowElement, fileRoles));

            }
        }

        [TestMethod]
        public void DistributionFlowElement_ifcFlowMeterType_Test2()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcDistributionFlowElement ifcDistributionFlowElement;
                using (var txn = model.BeginTransaction())
                {
                    ifcDistributionFlowElement = model.Instances.New<IfcDistributionFlowElement>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcFlowMeterType ifcFlowMeterType = model.Instances.New<IfcFlowMeterType>();
                    ifcFlowMeterType.PredefinedType = IfcFlowMeterTypeEnum.GASMETER;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcFlowMeterType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcDistributionFlowElement);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcDistributionFlowElement, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with .Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionFlowElement, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Mechanical; //should pass with .Mechanical
                Assert.IsTrue(MergeTool.Merge(ifcDistributionFlowElement, fileRoles));

            }
        }


        [TestMethod]
        public void Window_ifcWindow_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcWindow ifcWindow;
                using (var txn = model.BeginTransaction())
                {
                    ifcWindow = model.Instances.New<IfcWindow>();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Mechanical;//should fail with Mechanical
                Assert.IsFalse(MergeTool.Merge(ifcWindow, fileRoles));

                fileRoles = COBieMergeRoles.Architectural | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with Architectural
                Assert.IsTrue(MergeTool.Merge(ifcWindow, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Architectural; //should pass with Architectural
                Assert.IsTrue(MergeTool.Merge(ifcWindow, fileRoles));

            }
        }

        [TestMethod]
        public void TransportElement_ifcTransportElementType_Test()
        {
            using (XbimModel model = XbimModel.CreateTemporaryModel())
            {
                model.Initialise("Federation Creating Author", "Federation Creating Organisation", "This Application", "This Developer", "v1.1");
                IfcTransportElement ifcTransportElement;
                using (var txn = model.BeginTransaction())
                {
                    ifcTransportElement = model.Instances.New<IfcTransportElement>();

                    IfcRelDefinesByType ifcRelDefinesByType = model.Instances.New<IfcRelDefinesByType>();
                    IfcTransportElementType ifcTransportElementType = model.Instances.New<IfcTransportElementType>();
                    ifcTransportElementType.PredefinedType = IfcTransportElementTypeEnum.ELEVATOR;

                    //set up relationship 
                    ifcRelDefinesByType.RelatingType = ifcTransportElementType;
                    ifcRelDefinesByType.RelatedObjects.Add(ifcTransportElement);
                    txn.Commit();
                }

                FilterValuesOnMerge MergeTool = new FilterValuesOnMerge();

                COBieMergeRoles fileRoles = COBieMergeRoles.Architectural;//should fail with Architectural
                Assert.IsFalse(MergeTool.Merge(ifcTransportElement, fileRoles));

                fileRoles = COBieMergeRoles.Mechanical | COBieMergeRoles.Electrical | COBieMergeRoles.Plumbing | COBieMergeRoles.FireProtection;//should pass with .Electrical
                Assert.IsTrue(MergeTool.Merge(ifcTransportElement, fileRoles));

                fileRoles = COBieMergeRoles.Unknown | COBieMergeRoles.Architectural | COBieMergeRoles.Electrical; //should pass with .Electrical
                Assert.IsTrue(MergeTool.Merge(ifcTransportElement, fileRoles));

            }
        }
    }
}
