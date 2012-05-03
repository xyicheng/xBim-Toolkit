using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Xml.Linq;

namespace Xbim.Web.Viewer3D
{
    /// <summary>
    /// Summary description for ServerViewer3DControl
    /// </summary>
    public class ServerViewer3DControl : ScriptControl
    {
        public enum InitMode
        {
            None = 0,
            InitO3dEngine = 1,
            GetTransformGraph = 2,
            GetWholeMesh = 4,
        }

        public InitMode InitialisationMode { get; set; }

        public string ServiceUrl { get; set; }
        public ServerViewer3DControl()
        {
            //
            // TODO: Add constructor logic here
            //
            ServiceUrl = "/Data3D.aspx";
            this.InitialisationMode = InitMode.GetWholeMesh;
        }
        protected override IEnumerable<ScriptDescriptor>
                GetScriptDescriptors()
        {
            ScriptControlDescriptor descriptor = new ScriptControlDescriptor("Xbim.Web.Viewer3D.ClientViewer3DControl", this.ClientID);
            yield return descriptor;
        }

        // Generate the script reference
        protected override IEnumerable<ScriptReference>
                GetScriptReferences()
        {
            yield return new ScriptReference("Xbim.Web.Viewer3D.ClientViewer3DControl.js", this.GetType().Assembly.FullName);
            yield return new ScriptReference("Xbim.Web.Viewer3D.ClientViewerTG.js", this.GetType().Assembly.FullName);
            yield return new ScriptReference("Xbim.Web.Viewer3D.XbimBinaryParser.js", this.GetType().Assembly.FullName);
            
            yield return new ScriptReference("Xbim.Web.Viewer3D.Viewer.viewer.js", this.GetType().Assembly.FullName);
            yield return new ScriptReference("Xbim.Web.Viewer3D.Viewer.viewer-mesh.js", this.GetType().Assembly.FullName);
            yield return new ScriptReference("Xbim.Web.Viewer3D.Viewer.orbittool.js", this.GetType().Assembly.FullName);
            yield return new ScriptReference("Xbim.Web.Viewer3D.Viewer.pantool.js", this.GetType().Assembly.FullName);
            yield return new ScriptReference("Xbim.Web.Viewer3D.Viewer.zoomtool.js", this.GetType().Assembly.FullName);
            yield return new ScriptReference("Xbim.Web.Viewer3D.Viewer.picktool.js", this.GetType().Assembly.FullName);
        }

        protected override void Render(HtmlTextWriter writer)
        {
            writer.WriteLine("<div id='o3d' style='width:100%; height: 100%;'></div>");
            writer.WriteLine("<script>");
            writer.WriteLine("var _Viewer = new XbimClientViewer3DControl('o3d','" + this.ServiceUrl + "' );");
            if (((int)InitialisationMode) >= (int)InitMode.GetTransformGraph)
                writer.WriteLine("_Viewer.addAsyncQueue('downloadTG');");
            if (((int)InitialisationMode) >= (int)InitMode.GetWholeMesh)
                writer.WriteLine("_Viewer.addAsyncQueue('downloadWholeModel');");
            if (((int)InitialisationMode) >= (int)InitMode.InitO3dEngine)
                writer.WriteLine("_Viewer.initO3D();");
            writer.WriteLine("</script>");
        }
    }
}