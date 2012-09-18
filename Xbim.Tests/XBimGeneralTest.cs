using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Xbim.XbimExtensions;
using Xbim.IO;
using Xbim.Ifc.Extensions;
using Xbim.XbimExtensions.Parser;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions.Transactions.Extensions;
using System.Diagnostics;
namespace Xbim.Tests
{
    [TestClass]
    public class XBimGeneralTest
    {
        [TestMethod]
        public void Test_Instances()
        {
            Module ifcModule = typeof(IfcActor).Module;
            IEnumerable<Type> types =
                ifcModule.GetTypes().Where(
                    t =>
                    typeof(IPersistIfc).IsAssignableFrom(t) && t != typeof(IPersistIfc) && !t.IsEnum && t.IsAbstract &&
                    t.IsPublic && !typeof(ExpressHeaderType).IsAssignableFrom(t));
            foreach (Type ifctype in types)
            {
                //Console.WriteLine(ifctype.Name);
                Trace.WriteLine(ifctype.Name);
                //Debug.WriteLine(ifctype.Name);
            }
        }
    }
}
