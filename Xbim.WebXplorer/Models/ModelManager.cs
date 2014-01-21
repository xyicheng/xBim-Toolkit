using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using Xbim.WebXplorer.xbim;
using System.IO;

namespace Xbim.WebXplorer.Models
{
    /// <summary>
    /// This class provides a way of getting a model, and keeping it alive for KEEPALIVETIME (ms) from the last request
    /// </summary>
    public static class ModelManager
    {
        private const Int32 KEEPALIVETIME = 30000;
        private const String ModelPath = "c:\\";
        private const String ModelExt = ".xbim";
        private static Dictionary<String, XbimSceneModel> Models = new Dictionary<String, XbimSceneModel>();
        private static Dictionary<String, Timer> Timers = new Dictionary<String, Timer>();

        public static XbimSceneModel GetModel(String ModelName)
        {
            XbimSceneModel retval = null;
            try { retval = Models[ModelName]; } catch(Exception){}

            if (retval == null)
            {
                var model = new XbimModelHandler(ModelPath + ModelName + ModelExt);
                model.Init();

                retval = new XbimSceneModel(model);

                Models[ModelName] = retval;
                Timers[ModelName] = new Timer(TimerHit, ModelName, KEEPALIVETIME, 0);
            }
            else {
                Timers[ModelName].Change(KEEPALIVETIME, 0);
            }

            return retval;
        }
        public static void TimerHit(object state)
        {
            string ModelName = state as String;
            XbimSceneModel Model = Models[ModelName];

            if (Model != null)
            {
                Model.Dispose();
            }

            Models.Remove(ModelName);
            Timers.Remove(ModelName);
        }

        internal static IEnumerable<String> GetModelList()
        {
            DirectoryInfo d = new DirectoryInfo(ModelPath);
            var files = d.EnumerateFiles("*" + ModelExt);
            List<String> retvals = new List<string>();
            foreach (var f in files)
            {
                retvals.Add(f.Name.Replace(ModelExt, ""));
            }
            return retvals;
        }
    }
}