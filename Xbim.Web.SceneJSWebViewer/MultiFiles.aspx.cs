using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;

namespace Xbim.SceneJSWebViewer
{
    public partial class MultiFiles : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            LiteralControl litcon01 = new LiteralControl();
            litcon01.Text += "<h1>nbl_WC_Close-Coupled</h1>";
            model01_title.Controls.Add(litcon01);

            LiteralControl litcon02 = new LiteralControl();
            litcon02.Text += "<h1>nbl_WashFountain</h1>";
            model02_title.Controls.Add(litcon02);

            LiteralControl litcon03 = new LiteralControl();
            litcon03.Text += "<h1>nbl_WashBasin_Pedestal</h1>";
            model03_title.Controls.Add(litcon03);

            LiteralControl litcon04 = new LiteralControl();
            litcon04.Text += "<h1>nbl_Sink_Belfast</h1>";
            model04_title.Controls.Add(litcon04);

            LiteralControl litcon05 = new LiteralControl();
            litcon05.Text += "<h1>nbl_Shower_Rctngl</h1>";
            model05_title.Controls.Add(litcon05);

            LiteralControl litcon06 = new LiteralControl();
            litcon06.Text += "<h1>nbl_SanitaryAccessory_Hand-Drier</h1>";
            model06_title.Controls.Add(litcon06);
        }
    }
}