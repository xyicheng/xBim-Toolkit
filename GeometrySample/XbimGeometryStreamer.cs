using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.IO;
using Xbim.ModelGeometry.Converter;

namespace GeometrySample
{
    public enum Commands : byte
    {
        Error = 0x1,
        GetInstances = 0x2
    }

    public class XbimGeometryStreamer
    {
        
        public byte[] GetInstances(string modelName)
        {
            using (XbimModel source = new XbimModel())
            {
                try
                {
                    source.Open(modelName);
                    Xbim3DModelContext m3d = new Xbim3DModelContext(source);
                    int modelId = 0;
                    UInt32 instanceCount = 0;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (BinaryWriter bw = new BinaryWriter(ms))
                        {
                            bw.Write((byte)Commands.GetInstances);
                            bw.Write(0U);
                            foreach (var si in m3d.ShapeInstances())
                            {
                                bw.Write(modelId);
                                bw.Write(si.InstanceLabel);
                                bw.Write(si.IfcProductLabel);
                                bw.Write(si.IfcTypeId);
                                bw.Write(si.StyleLabel);
                                bw.Write(si.ShapeGeometryLabel);
                                bw.Write(si.Transformation.ToArray(false));
                                XbimShapeGeometry sg = m3d.ShapeGeometry(si);
                                bw.Write(sg.Cost);
                                bw.Write(sg.ReferenceCount);
                                bw.Write(sg.BoundingBox.ToFloatArray());
                                instanceCount++;
                            }
                            bw.Seek(1, SeekOrigin.Begin);
                            bw.Write(instanceCount);
                            byte[] b = ms.ToArray();
                            return b;
                        }
                       
                    }
                   
                }
                catch (Exception )
                {
                    return new byte[1] { (byte)Commands.Error};
                    
                }

            }

        }
    }
}
