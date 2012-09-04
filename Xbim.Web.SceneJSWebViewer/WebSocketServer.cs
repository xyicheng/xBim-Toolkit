using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using Xbim.Common.Logging;
using System.Collections.Concurrent;
using Alchemy.Classes;
using Alchemy.Handlers.WebSocket;

namespace Xbim.SceneJSWebViewer
{
    public class XBimWebSocketServer
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger();
        private static ConcurrentDictionary<String, String> usermodels = new ConcurrentDictionary<String, String>();

        internal void OnConnected(Alchemy.Classes.UserContext context)
        {
        }

        internal void OnConnect(Alchemy.Classes.UserContext context)
        {
        }

        internal void OnDisconnect(Alchemy.Classes.UserContext context)
        {
            String connectionId = context.ClientAddress.ToString();
            ModelStreamer.Disconnect(connectionId);
        }

        internal void OnSend(Alchemy.Classes.UserContext context)
        {
        }

        internal void OnReceive(Alchemy.Classes.UserContext context)
        {
            String modelId = String.Empty;
            String data = context.DataFrame.ToString();
            String connectionId = context.ClientAddress.ToString();
            try
            {
                //Attempt to deserialise the string as a dynamic JSON object
                var serializer = new JavaScriptSerializer();
                serializer.RegisterConverters(new[] { new DynamicJsonConverter() });
                dynamic obj = serializer.Deserialize(data, typeof(object));

                modelId = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + "models\\" + obj.ModelID.ToString().Split(new char[] { '.' })[0];
                if (modelId == String.Empty)
                {
                    return;
                }
                usermodels.AddOrUpdate(connectionId, (k) => modelId, (k, v) => modelId);

                switch ((Command)obj.command)
                {
                    case Command.ModelView: //Setup model stream and send Model View
                        context.Send(System.Convert.ToBase64String(ModelStreamer.SendModelView(connectionId, modelId)));
                        return;
                    case Command.SharedMaterials: //Setup Shared Materials
                        context.Send(System.Convert.ToBase64String(ModelStreamer.SendSharedMaterials(connectionId, modelId)));
                        return;
                    case Command.Types: //Setup Types
                        context.Send(System.Convert.ToBase64String(ModelStreamer.SendTypes(connectionId, modelId)));
                        return;
                    case Command.SharedGeometry: //Setup Shared Geometry
                        context.Send(System.Convert.ToBase64String(ModelStreamer.SendSharedGeometry(connectionId, modelId)));
                        return;
                    case Command.GeometryData: // Actual vertex locations
                        String temp = obj.id;
                        String[] temps = temp.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        context.Send(System.Convert.ToBase64String(ModelStreamer.SendGeometryData(connectionId, modelId, temps)));
                        return;
                    case Command.Data: //Setup Data
                        context.Send(System.Convert.ToBase64String(ModelStreamer.SendData(connectionId, modelId)));
                        return;
                    case Command.QueryData:
                        context.Send(System.Convert.ToBase64String(ModelStreamer.SendQueryData(connectionId, modelId, obj.id.ToString(), obj.query)));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Debug("Failed To convert or send", ex);
            }
            return;
        }
    }
}