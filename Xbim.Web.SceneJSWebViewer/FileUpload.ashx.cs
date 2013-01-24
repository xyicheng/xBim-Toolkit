using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Diagnostics;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.IO;
using Xbim.ModelGeometry;
using Xbim.XbimExtensions;

namespace Xbim.SceneJSWebViewer
{
    /// <summary>
    /// Summary description for FileUpload
    /// </summary>
    public class FileUpload : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            if (context.Request.Files.Count > 0)
            {
                string path = context.Server.MapPath("~/models");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                var file = context.Request.Files[0];
                string fileName;

                // we need random filename for temporary upload
                fileName = Guid.NewGuid().ToString() + ".ifc";
                //if (HttpContext.Current.Request.Browser.Browser.ToUpper() == "IE")
                //{
                //    string[] files = file.FileName.Split(new char[] { '\\' });
                //    fileName = files[files.Length - 1];
                //}
                //else
                //{
                //    fileName = file.FileName;
                //}

                if (!string.IsNullOrEmpty(fileName))
                {
                    string strFileName = fileName;
                    fileName = Path.Combine(path, fileName);
                    file.SaveAs(fileName);

                    

                    try
                    {

                        //string xbimFileName = Path.ChangeExtension(fileName, ".xbim");
                        //XbimModel model = new XbimModel();
                        //model.CreateFrom(fileName, xbimFileName, null);
                        //model.Open(xbimFileName, XbimDBAccess.ReadWrite);
                        //XbimScene.ConvertGeometry(model.Instances.OfType<IfcProduct>().Where(t => !(t is IfcFeatureElement)), null, false);
                        //model.Close();
                       
                        string msg = "{";
                        msg += string.Format("error:'{0}',\n", string.Empty);
                        msg += string.Format("modelid:'{0}'\n", "Munkerud.xbim");
                        ;
                        msg += "}";
                        context.Response.Write(msg);
                        
                    }
                    catch (Exception ex)
                    {
                        string msg = "{";
                        msg += string.Format("error:'{0}',\n", ex.Message);
                        msg += string.Format("modelid:'{0}'\n", Path.ChangeExtension(strFileName, "xbim"));
                        msg += "}";
                        context.Response.Write(msg);
                    }
                }
                
            }
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}
