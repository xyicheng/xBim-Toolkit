using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Xbim.IO;
using Xbim.Script;
using Newtonsoft.Json;

namespace Xbim.WeXplorer.MVC4.Controllers
{
    public class BQLScriptController : XbimController
    {
        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            //suspend model which is held by parser
            //if (parser != null)
                
            base.OnActionExecuted(filterContext);
        }

        //XbimQueryParser
        private XbimQueryParser parser {
            get { 
            return Session["actualParser"] as XbimQueryParser;
            }

            set { Session["actualParser"] = value; }
        }

        [HttpPost]
        public JsonResult Execute(string model, string script)
        {
            if (parser == null)
                InitParser(model);
            
            var dbName = Path.GetFileNameWithoutExtension(parser.Model.DatabaseName);
            if (dbName != model)
                InitParser(model);

            var fileOutput = "";
            parser.Output = new StringWriter();
            parser.OnFileReportCreated += delegate(object sender, FileReportCreatedEventArgs e)
            {
                fileOutput = e.FilePath;
            };
            parser.Parse(script);
            parser.OnFileReportCreated -= delegate(object sender, FileReportCreatedEventArgs e)
            {
                fileOutput = e.FilePath;
            };

            //create dynamic result
            var result = new
            {
                Errors = parser.Errors,
                File = fileOutput,
                Message = parser.Output.ToString(),
                LatestResults = GetLatestResults(),
            };
       
            return CreateJsonResult(result, JsonRequestBehavior.DenyGet);
        }

        private void InitParser(string name)
        {
            parser = new XbimQueryParser(GetModel(name));
        }

        private IEnumerable<uint> GetLatestResults()
        {
            foreach (var item in parser.Results.LastEntities)
                yield return item.EntityLabel;
        }
    }
}
