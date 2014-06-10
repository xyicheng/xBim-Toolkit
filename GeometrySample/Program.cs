using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;
using Xbim.IO;
using Xbim.ModelGeometry.Converter;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions.Interfaces;

namespace GeometrySample
{
    class Program
    {
        static void Main(string[] args)
        {
            Params arguments = Params.ParseParams(args);
            if (arguments.IsValid)
            {
                try
                {

                    XbimGeometryStreamer gs = new XbimGeometryStreamer();
                    byte[] b = gs.GetInstances(arguments.SourceModelName);

                    //ReportProgressDelegate progDelegate = delegate(int percentProgress, object userState)
                    //{
                    //    Console.Write(string.Format("{0:D5}", percentProgress));
                    //    ResetCursor(Console.CursorTop);
                    //};
                    //using (XbimModel source = new XbimModel())
                    //{
                    //    Console.WriteLine(string.Format("Reading {0}", arguments.SourceModelName));
                    //    Xbim3DModelContext m3d=null;
                    //    if (arguments.SourceIsXbimFile)
                    //    {
                    //        source.Open(arguments.SourceModelName);
                    //        m3d = new Xbim3DModelContext(source);
                    //    }
                    //    else
                    //    {
                    //        source.CreateFrom(arguments.SourceModelName, null, progDelegate, true);
                    //        try
                    //        {
                    //            m3d = new Xbim3DModelContext(source);
                    //            m3d.CreateContext(true, progDelegate);
                    //        }
                    //        catch (Exception ce)
                    //        {
                    //            Console.WriteLine("Error compiling geometry\n" + ce.Message);
                    //        }

                    //    }
                    //    if (m3d == null) throw new Exception("Could not open or create the geometric context");

                    //    //get the translation to move the model to the canvas centre and scale to metres
                    //    XbimMatrix3D wcsTransform = GetModelTranslation(source, m3d);

                    //    //create a colour map for your default colours
                    //    XbimColourMap cMap = new XbimColourMap(StandardColourMaps.IfcProductTypeMap);
                    //    //get all the surface styles, if the object has no defined style use the default for its product type in the coulour map
                    //    IEnumerable<XbimTexture> styles = m3d.SurfaceStyles(cMap);
                    //    //go over each shape geometry
                    //    foreach (var shapeGeom in m3d.ShapeGeometries())
                    //    {
                    //        Console.WriteLine(string.Format("#{0:D5} = Record ID = {1}, Instances = {2}, Geometry Size = {3}", shapeGeom.IfcShapeLabel,shapeGeom.ShapeLabel, shapeGeom.ReferenceCount, shapeGeom.Cost));
                    //        foreach (var shapeInst in m3d.ShapeInstancesOf(shapeGeom))
                    //        {
                    //             Console.WriteLine(string.Format("  Inst = {0}, Type = {1}, Style = {2}, ", shapeInst.InstanceLabel,IfcMetaData.GetType(shapeInst.IfcTypeId).Name, shapeInst.StyleLabel));
                    //        }
                    //        Console.WriteLine(string.Format("   Box = {0}", shapeGeom.BoundingBox.ToString()));
                    //        Console.WriteLine(string.Format("  Geom = {0}", shapeGeom.ShapeData));
                    //    }
                    //}
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadLine(); //wait for use to kill
        }

        private static XbimMatrix3D GetModelTranslation(XbimModel source, Xbim3DModelContext m3d)
        {
            
            XbimRegion largest = m3d.GetLargestRegion();
            XbimPoint3D c = new XbimPoint3D(0, 0, 0);
            XbimRect3D bb = XbimRect3D.Empty;
            if (largest != null)
                bb = new XbimRect3D(largest.Centre, largest.Centre);

            foreach (var refModel in source.ReferencedModels)
            {

                XbimRegion r;

                Xbim3DModelContext refContext = new Xbim3DModelContext(refModel.Model);
                r = refContext.GetLargestRegion();

                if (r != null)
                {
                    if (bb.IsEmpty)
                        bb = new XbimRect3D(r.Centre, r.Centre);
                    else
                        bb.Union(r.Centre);
                }
            }
            XbimPoint3D p = bb.Centroid();
            XbimVector3D modelTranslation = new XbimVector3D(-p.X, -p.Y, -p.Z);
            //get the world coordinate transform and scale
            double metre = source.ModelFactors.OneMetre;
            XbimMatrix3D wcsTransform = XbimMatrix3D.CreateTranslation(modelTranslation) * XbimMatrix3D.CreateScale((float)(1 / metre));
            return wcsTransform;
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
