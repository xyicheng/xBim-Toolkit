using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Xbim.Web.Viewer3D.ServerSide;
using System.IO;

namespace Xbim.Web.Xplorer
{
    public partial class Data3D : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //XbimRepo x = new XbimRepo("onewall.xbim");
            //string filePath = Path.Combine(Server.MapPath("~/SampleData"), "SimpleHouse.xbim");
            string filePath = Path.Combine(Server.MapPath("~/SampleData"), "OneWall.xbim");
            //string filePath = Path.Combine(Server.MapPath("~/SampleData"), "TwoBlocks.xbim");
            
            XbimRepo x = new XbimRepo(filePath);

            switch (Request["Data"])
            {
                case "TG":
                    // this is only the TransformGraph in XML 
                    //
                    x.WriteTg(Response);
                    break;
                case "MSH":
                    // this is only the TransformGraph in XML
                    //
                    string itemlables = Request.Form["EL"];
                    x.WriteBinaryMesh(Response, itemlables);
                    break;
                case "XMLMESH":
                    x.WriteXMLMesh(Response);
                    break;
                default:
                    Response.Write("heregoes");
                    Response.End();
                    break;
            }
        }
    }
}