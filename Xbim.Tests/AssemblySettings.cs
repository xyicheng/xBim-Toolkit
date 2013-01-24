using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;

namespace Xbim.Tests
{
    [TestClass]
    public class AssemblySettings
    {
        [AssemblyInitialize()]
        static public void AssemblyInit(TestContext context)
        {
            // this method runs before start of every test
            XbimModel.Initialize();
        }

        [AssemblyCleanup()]
        static public void AssemblyCleanup()
        {
            // this method runs after finishes every test
            XbimModel.Terminate();
        }
    }
}
