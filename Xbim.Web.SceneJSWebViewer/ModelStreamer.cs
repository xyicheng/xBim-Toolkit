using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Web.Script.Serialization;
using System.Diagnostics;
using Xbim.Common.Logging;

namespace Xbim.SceneJSWebViewer
{
    public class ModelStreamer
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger();
        private static ConcurrentDictionary<String, String> usermodels = new ConcurrentDictionary<String, String>();


        public static void Disconnect(string connectionId)
        {
            String modelid = String.Empty;
            bool success = usermodels.TryRemove(connectionId, out modelid);
            if (success && modelid != String.Empty)
            {
                Int32 count = 0;
                foreach (String key in usermodels.Keys) //go through users and work out how many are using this model
                {
                    String modelName;
                    if (usermodels.TryGetValue(key, out modelName))
                    {
                        if (modelid == modelName)
                        {
                            count++;
                        }
                    }
                }
                if (count == 0) //if no one is using the file then close it.
                {
                    CloseModel(modelid);
                }
            }
        }

        internal static IModelStream GetModelStream(String modelId)
        {
            return XBimModelStream.GetModelStream(modelId.Replace("'", string.Empty));
        }

        private  static IModelStream GetModelStream(dynamic connection)
        {
            try
            {
                String modelid = connection.ModelID;
                return GetModelStream(modelid.Replace("'", string.Empty));
            }
            catch (Exception ex) { Logger.Error(ex.ToString()); }

            return GetModelStream("test");
        }

        internal static void CloseModel(String modelId)
        {
            modelId = modelId.Replace("'", String.Empty);
            XBimModelStream.CloseModel(modelId);
        }

        internal static byte[] SendGeometryData(string connectionId, string modelId, String[] ids)
        {
            IModelStream modelstream = GetModelStream(modelId);
            GeometryData[] gdata = new GeometryData[ids.Length];
            Int32 size = 6;
            Int32 NoOfNull = 0;
            for (int i = 0; i < gdata.Length; i++)
            {
                GeometryData gd = modelstream.GetGeometryData(ids[i]);
                gdata[i] = gd;
                if (gd != null)
                {
                    size += 11
                        + gdata[i].data.Length
                        + (16 * 8);//add in the transform matrix length
                }
                else
                {
                    NoOfNull++;
                }
            }


            byte[] data = new byte[size];

            //setup a new byte list for return, and add the command/endian
            data[0] = ((byte)Command.GeometryData); //Command
            data[1] = BitConverter.IsLittleEndian ? (byte)0x01 : (byte)0x00; //Endian Flag

            Int32 offset = 2;

            //send count of items
            BitConverter.GetBytes(Convert.ToUInt16(gdata.Length - NoOfNull)).CopyTo(data, offset);
            offset += 2;

            BitConverter.GetBytes(Convert.ToUInt16(NoOfNull)).CopyTo(data, offset);
            offset += 2;

            //foreach item
            for (int i = 0; i < gdata.Length; i++)
            {
                if (gdata[i] != null)
                {
                    BitConverter.GetBytes(gdata[i].ID).CopyTo(data, offset);
                    offset += 4;

                    //view transform matrix
                    if (gdata[i].MatrixTransform.Length == 16) //if we have a well formed matrix
                    {
                        foreach (Double md in gdata[i].MatrixTransform)
                        {
                            BitConverter.GetBytes(md).CopyTo(data, offset);
                            offset += 8;
                        }
                    }
                    else { offset += 16 * 8; }

                    //add data length
                    BitConverter.GetBytes(Convert.ToUInt32(gdata[i].data.Length)).CopyTo(data, offset);
                    offset += 4;

                    BitConverter.GetBytes(gdata[i].NumChildren).CopyTo(data, offset);
                    offset += 2;

                    BitConverter.GetBytes(gdata[i].HasData).CopyTo(data, offset);
                    offset += 1;

                    //add data
                    gdata[i].data.CopyTo(data, offset);
                    offset += gdata[i].data.Length;
                }
            }
            return data;
        }

        //Query IFC model for property information
        internal static byte[] SendQueryData(string connectionId, string modelId, String id, String query)
        {
            IModelStream modelstream = GetModelStream(modelId);
            String bdata = modelstream.QueryData(id, query);
            String buffer = "{\"id\":\"" + id + "\",\"data\":\"" + bdata + "\"}";

            //setup a new byte list for return, and add the command/endian
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            List<byte> data = new List<byte>();
            data.Add((byte)Command.QueryData); //Command
            data.Add(BitConverter.IsLittleEndian ? (byte)0x01 : (byte)0x00); //Endian Flag

            data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(buffer.Length)));
            data.AddRange(encoding.GetBytes(buffer));

            return data.ToArray();
        }

        internal static byte[] SendData(string connectionId, string modelId)
        {
            IModelStream modelstream = GetModelStream(modelId);
            //setup a new byte list for return, and add the command/endian
            List<byte> data = new List<byte>();
            data.Add((byte)Command.Data); //Command
            data.Add(BitConverter.IsLittleEndian ? (byte)0x01 : (byte)0x00); //Endian Flag

            return data.ToArray();
        }

        internal static byte[] SendSharedGeometry(string connectionId, string modelId)
        {
            IModelStream modelstream = GetModelStream(modelId);
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

            //setup a new byte list for return, and add the command/endian
            List<byte> data = new List<byte>();
            data.Add((byte)Command.SharedGeometry); //Command
            data.Add(BitConverter.IsLittleEndian ? (byte)0x01 : (byte)0x00); //Endian Flag

            List<GeometryLabel> temp = modelstream.GetGeometryHeaders();
            data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(temp.Count)));
            Int32 totalcount = 0;
            foreach (GeometryLabel g in temp)
            {
                totalcount += g.Geometries.Count;
            }
            data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(totalcount)));
            foreach (GeometryLabel g in temp)
            {
                data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(g.Type.Length)));
                data.AddRange(encoding.GetBytes(g.Type));
                data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(g.Material.Length)));
                data.AddRange(encoding.GetBytes(g.Material));

                //send the layer ordering (for alpha blending) of this type
                data.AddRange(BitConverter.GetBytes(g.LayerPriority));

                data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(g.Geometries.Count)));
                foreach (String s in g.Geometries)
                {
                    data.AddRange(BitConverter.GetBytes(Convert.ToInt32(s)));
                }
            }

            return data.ToArray();
        }

        internal static byte[] SendTypes(string connectionId, string modelId)
        {
            IModelStream modelstream = GetModelStream(modelId);
            //dummy string array of types
            List<String> types = modelstream.GetTypes();

            //setup a new byte list for return, and add the command/endian
            List<byte> data = new List<byte>();
            data.Add((byte)Command.Types); //Command
            data.Add(BitConverter.IsLittleEndian ? (byte)0x01 : (byte)0x00); //Endian Flag

            //set how many strings we are sending
            data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(types.Count)));
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            foreach (String s in types)
            {
                //for each string send string length then the string
                data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(s.Length)));
                data.AddRange(encoding.GetBytes(s));
            }

            return data.ToArray();
        }

        internal static byte[] SendModelView(string connectionId, string modelId)
        {
            IModelStream modelstream = GetModelStream(modelId);

            List<byte> data = new List<byte>();
            data.Add((byte)Command.ModelView); //Command
            data.Add(BitConverter.IsLittleEndian ? (byte)0x01 : (byte)0x00); //Endian Flag

            Camera cam = modelstream.GetCamera();

            //Check the cam values
            if (cam.minX == Double.MinValue || cam.minY == Double.MinValue || cam.minZ == Double.MinValue || cam.maxX == Double.MaxValue || cam.maxY == Double.MaxValue || cam.maxZ == Double.MaxValue)
            {
                //if we hit here we haven't got a valid box model for the camera
                Logger.Warn("Failed to get a valid Bounding Box for the model: "+modelId);
                return new byte[]{};
            }

            data.AddRange(BitConverter.GetBytes(cam.minX));
            data.AddRange(BitConverter.GetBytes(cam.minY));
            data.AddRange(BitConverter.GetBytes(cam.minZ));
            data.AddRange(BitConverter.GetBytes(cam.maxX));
            data.AddRange(BitConverter.GetBytes(cam.maxY));
            data.AddRange(BitConverter.GetBytes(cam.maxZ));

            return data.ToArray();
        }

        internal static byte[] SendSharedMaterials(string connectionId, string modelId)
        {
            IModelStream modelstream = GetModelStream(modelId);
            List<Material> mats = modelstream.GetMaterials();

            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

            //setup a new byte list for return, and add the command/endian
            List<byte> data = new List<byte>();
            data.Add((byte)Command.SharedMaterials); //Command
            data.Add(BitConverter.IsLittleEndian ? (byte)0x01 : (byte)0x00); //Endian Flag

            //send num of materials
            data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(mats.Count)));

            //send each material
            foreach (Material m in mats)
            {
                //send name length
                data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(m.Name.Length)));
                //send name
                data.AddRange(encoding.GetBytes(m.Name));
                //send mat details - R, G, B, Alpha, Emit
                data.AddRange(BitConverter.GetBytes(m.Red));
                data.AddRange(BitConverter.GetBytes(m.Green));
                data.AddRange(BitConverter.GetBytes(m.Blue));
                data.AddRange(BitConverter.GetBytes(m.Alpha));
                data.AddRange(BitConverter.GetBytes(m.Emit));
            }
            return data.ToArray();
        }
    }
}