using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xbim.Xplorer
{
    class View
    {
        public Int64 offset { get { return stream.Position; } }
        public MemoryStream stream = null;
        byte[] buffer = new byte[8];
        public View(MemoryStream Stream)
        {
            stream = Stream;
        }
        public View(byte[] Buffer)
        {
            stream = new MemoryStream(Buffer);
        }
        public Single getFloat32()
        {
            stream.Read(buffer, 0, 4);
            Single ret = BitConverter.ToSingle(buffer, 0);

            return ret;
        }
        public Double getFloat64()
        {
            stream.Read(buffer, 0, 8);
            Double ret = BitConverter.ToDouble(buffer, 0);
            return ret;
        }
        public Int32 getInt32()
        {
            stream.Read(buffer, 0, 4);
            Int32 ret = BitConverter.ToInt32(buffer, 0);
            return ret;
        }
        public UInt32 getUint32()
        {
            stream.Read(buffer, 0, 4);
            UInt32 ret = BitConverter.ToUInt32(buffer, 0);
            return ret;
        }
        public ushort getUint16()
        {
            stream.Read(buffer, 0, 2);
            UInt16 ret = BitConverter.ToUInt16(buffer, 0);
            return ret;
        }
        public Byte getUint8()
        {
            return getByte();
        }
        public Byte getByte()
        {
            stream.Read(buffer, 0, 1);
            Byte ret = buffer[0];
            return ret;
        }
    }
}