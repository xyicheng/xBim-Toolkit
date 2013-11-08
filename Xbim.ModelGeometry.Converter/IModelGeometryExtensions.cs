using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.Common.Logging;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.ModelGeometry.Converter
{
    public static class IModelGeometryExtensions
    {
       

        internal readonly static ILogger Logger = LoggerFactory.GetLogger();
        public static IXbimGeometryEngine GeometryEngine(this IModel iModel)
        {
            
            if (iModel.GeometryManager == null)
            {
                //Create the geometry engine by reflection to allow dynamic loading of different binary platforms (32, 64 etc)
                Assembly assembly = AssemblyResolver.GetModelGeometryAssembly();
                if (assembly == null)
                {
                    if (Logger != null)
                    {
#if DEBUG
                    Logger.Error("Failed to load Xbim.ModelGeometry.OCCd.dll Please ensure it is installed correctly");
#else
                        Logger.Error("Failed to load Xbim.ModelGeometry.OCC.dll Please ensure it is installed correctly");
#endif
                    }

                }
                else if(iModel is XbimModel) //only works with XbimModels
                {
                    IXbimGeometryEngine engine = (IXbimGeometryEngine)assembly.CreateInstance("Xbim.ModelGeometry.XbimGeometryEngine");
                    engine.Init((XbimModel)iModel);
                    if (engine == null)
                    {
                        if (Logger != null)
                        {
#if DEBUG
                            Logger.Error("Failed to create Xbim Geometry engine. Please ensure Xbim.ModelGeometry.OCCd.dll is installed correctly");
#else
                            Logger.Error("Failed to create Xbim Geometry engine. Please ensure Xbim.ModelGeometry.OCC.dll is installed correctly");
#endif
                        }

                    }
                    else
                        iModel.GeometryManager = engine;
                }

            }
            return (IXbimGeometryEngine)iModel.GeometryManager;
        }
    }
}
