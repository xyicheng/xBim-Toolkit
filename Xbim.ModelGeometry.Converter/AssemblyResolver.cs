using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace Xbim.ModelGeometry.Converter
{
    public class AssemblyResolver
    {
        public static void HandleUnresovledAssemblies()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            
            currentDomain.AssemblyResolve += currentDomain_AssemblyResolve;
        }

        private static Assembly currentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            const string geomModuleName = "Xbim.ModelGeometry.OCC";
            if (string.Compare(args.Name,0,geomModuleName,0,geomModuleName.Length,true)==0)
            {
                var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);


                if (IntPtr.Size == 8) // or for .NET4 use Environment.Is64BitProcess
                {
                    path = Path.Combine(path, "x64");
                }
                else
                {
                    path = Path.Combine(path, "x86");
                }

                path = Path.Combine(path, geomModuleName + ".dll");

                Assembly assembly = Assembly.LoadFrom(path);
                return assembly;
            }

            return null;
        }
    }
}
