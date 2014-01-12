using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;

namespace Xbim.ModelGeometry.Converter
{
    public class AssemblyResolver
    {
#if DEBUG
        internal const string geomModuleName = "Xbim.ModelGeometry.OCCd";
#else
        internal const string geomModuleName = "Xbim.ModelGeometry.OCC";
#endif

        public static void HandleUnresolvedAssemblies()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            
            currentDomain.AssemblyResolve += currentDomain_AssemblyResolve;
        }

        private static Assembly currentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (string.Compare(args.Name, 0, geomModuleName, 0, geomModuleName.Length, true) == 0)
            {
                return GetModelGeometryAssembly();
            }
            else
                return null;

        }
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern void SetDllDirectory(string lpPathName);

        /// <summary>
        /// Sets the base path for native dlls, this should be the directory that contains the x86 and x64 directories
        /// The correct native dll directory will be added to the path
        /// </summary>
        /// <param name="interopPath"></param>
        public static void SetDllPath(string interopPath)
        {
            string path;
            if (IntPtr.Size == 8) // or for .NET4 use Environment.Is64BitProcess
            {
                path = Path.Combine(interopPath, "x64");
            }
            else
            {
                path = Path.Combine(interopPath, "x86");
            }
            SetDllDirectory(path);
        }
        
        public static Assembly GetModelGeometryAssembly(string basePath = null)
        {
            
            //check if the assembly is loaded
            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly asm in asms)
            {
                AssemblyName asmName = asm.GetName();
                if (string.Compare(asmName.Name, geomModuleName, true) == 0)
                    return asm;

            };
            string path;
            if (!string.IsNullOrWhiteSpace(basePath)) //if we have a base path use it else look at the executable
                path = basePath;
            else
                path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (!string.IsNullOrWhiteSpace(path))
                path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (IntPtr.Size == 8) // or for .NET4 use Environment.Is64BitProcess
            {
                path = Path.Combine(path, "x64");
            }
            else
            {
                path = Path.Combine(path, "x86");
            }
            path = Path.Combine(path, geomModuleName + ".dll");
     
            if (File.Exists(path))
            {
                Assembly assembly = Assembly.LoadFile(path);
                return assembly;
            }
            else
            {
                throw new Exception(string.Format("Could not load the Geometry engine at {0}",path));  
            }
            
        }
    }
}
