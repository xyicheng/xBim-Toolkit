using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Xbim.WeXplorer.MVC4.Controllers
{
    public class FileDownloadController : Controller
    {
        [HttpGet]
        public void DownloadOnce(string id)
        {
            var path = GetPath(id);
            if (System.IO.File.Exists(path))
            {
                Response.ContentType = "application/octet-stream";
                Response.AppendHeader("Content-Disposition", "attachment; filename=xxx");
                Response.WriteFile(path);
                System.IO.File.Delete(path);
                Response.End();
            }
        }

        private string GetPath(string id)
        {
            throw new NotImplementedException();
        }

    }
}
