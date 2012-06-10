using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;

namespace Xbim.SceneJSWebViewer
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                //get any xbim files in the /model directory of this web app, and display them as options for the user to load
                String path = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + "models";
                DirectoryInfo di = new DirectoryInfo(path);
                FileInfo[] fis = di.GetFiles("*.xbim");

                LiteralControl litcon = new LiteralControl();
                litcon.Text += "<ul>";
                //Setup the links to our models
                foreach (FileInfo fi in fis)
                {
                    //fi.Name;
                    litcon.Text += "<li><a href=\"#\" OnClick=DynamicLoad('"+fi.Name+"');>"+fi.Name+"</a></li>";
                }
                litcon.Text+= "</ul>";
                this.menu.Controls.Add(litcon);
            }
        }
    }
}
