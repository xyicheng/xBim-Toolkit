using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Diagnostics;

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
                        Process p = new Process();
                        // Redirect the output stream of the child process.
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.FileName = context.Server.MapPath("~/ConvertGCApp/Xbim.SceneHelper.exe");
                        p.StartInfo.Arguments = fileName;
                        p.Start();
                        // Do not wait for the child process to exit before
                        // reading to the end of its redirected stream.
                        // p.WaitForExit();
                        // Read the output stream first and then wait.
                        string output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(60000);


                        //System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo();
                        //start.FileName = context.Server.MapPath("~/bin/testdll.exe"); 
                        //start.Arguments = fileName;
                        //start.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

                        string msg = "{";
                        msg += string.Format("error:'{0}',\n", string.Empty);
                        msg += string.Format("modelid:'{0}'\n", Path.ChangeExtension(strFileName, "xbim"));
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