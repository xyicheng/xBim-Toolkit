using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.ModelGeometry.Converter;
using Xbim.Presentation;
using Xbim.XbimExtensions.Interfaces;

namespace XbimThumbnail
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Params arguments = Params.ParseParams(args);
            if (arguments.IsValid)
            {
                try
                {
                    ReportProgressDelegate progDelegate = delegate(int percentProgress, object userState)
                    {
                        Console.Write(string.Format("{0:D5}", percentProgress));
                        ResetCursor(Console.CursorTop);
                    };
                    using (XbimModel model = new XbimModel())
                    {
                        Console.WriteLine(string.Format("Reading {0}", arguments.SourceModelName));
                        if (arguments.SourceIsXbimFile)
                            model.Open(arguments.SourceModelName);
                        else
                            model.CreateFrom(arguments.SourceModelName, null, progDelegate, true);
                        Console.WriteLine();
                        Console.WriteLine("Compiling Geometry");
                        Xbim3DModelContext m3d = new Xbim3DModelContext(model);
                        try
                        {
                            m3d.CreateContext(true, progDelegate);
                        }
                        catch (Exception ce)
                        {
                            Console.WriteLine("Error compiling geometry, " + ce.Message);
                        }
                        Console.WriteLine();
                        Console.WriteLine("Creating Thumbnail");
                        DrawingControl3D.CreateThumbnail(model, arguments.TargetThumbnailName, arguments.Width, arguments.Height, model.IfcProject.Name);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            
        }

        private static void ResetCursor(int top)
        {
            try
            {
                // Can't reset outside of buffer, and should ignore when in quiet mode
                if (top >= Console.BufferHeight)
                    return;
                Console.SetCursorPosition(0, top);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
