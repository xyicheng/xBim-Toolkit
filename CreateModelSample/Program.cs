using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.XbimExtensions;
using Xbim.Common.Logging;
using Xbim.Common.Exceptions;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions.Interfaces;

namespace CreateModelSample
{
    class Program
    {
        /// <summary>
        /// Optionally create a stric logger for error reporting, see app.config for settings
        /// </summary>
        private static readonly ILogger Logger = LoggerFactory.GetLogger();

        //sample to show how to create a model from scratch
        
        static void Main(string[] args)
        { 
            Logger.Debug("Creating Model...");
            XbimModel model = CreateModel("NewModel");
         
            if (model != null)
            {
                using (XbimReadWriteTransaction txn = model.BeginTransaction()) //start a readwrite transaction
                {
                    try
                    {
                        IfcProject project = model.Instances.New<IfcProject>();     //Project Created

                        txn.Commit(); //commit the changes if nothing went wrong
                       
                    }
                    catch (Exception e)
                    {
                        Logger.DebugFormat("Error {0}\nRolling back changes.....", e.Message);
                    }
                }
                model.SaveAs("NewModel.ifc", XbimStorageType.IFC); //export as an Ifc File
                model.Close(); //close the model and release all resources and handles
            }
          
            Logger.Debug("Model Created Ended...");
            Console.ReadLine();
           // CreateProject
        }

        private static XbimModel CreateModel(string name)
        {
            try
            {
                return XbimModel.CreateModel(name, XbimDBAccess.ReadWrite); //create and open a model for readwrite
            }
            catch (Exception e )
            {
                Logger.Error("Failed to create Database", e);
                return null;
            }
        }

       
    }
}
