using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Xbim.WeXplorer.MVC4.Controllers
{
    public class ProxyController : Controller
    {
        public JavaScriptResult js()
        {
            //use reflection to get functions available 
            var types = GetType().Assembly.GetTypes().Where(t => typeof(Controller).IsAssignableFrom(t));
            var ctrlScripts = "//This is automatically generated Javascript proxy \n var XbimProxy = function() { };";
            foreach (var type in types)
            {
                var objectName = type.Name.Replace("Controller", "");
                var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var methodsScript = "";
                foreach (var mInfo in methods)
                {
                    if (mInfo.ReturnType != typeof(JsonResult))
                        continue;

                    var functionString = mInfo.Name + ": function(";
                    var parameters = mInfo.GetParameters();
                    string paramString = "";
                    string queryString = "?";
                    string dataString = "{";
                    foreach (var param in parameters)
                    {
                        paramString += param.Name + ", ";
                        queryString += String.Format("{0}='+{0}+'&", param.Name);
                        dataString += String.Format("{0}:{0},", param.Name);
                    }
                    paramString = paramString.TrimEnd(',', ' ');
                    queryString = queryString.TrimEnd('&');
                    dataString = dataString.TrimEnd(',') + "}";

                    if (mInfo.GetCustomAttributes(typeof(HttpPostAttribute), true).Length == 0)
                    {
                        functionString += paramString + @", onSuccess) {
$.getJSON(
'/" + objectName + "/" + mInfo.Name + queryString + @"',
null,
function(result){
var object = JSON.parse(result);
if(onSuccess) 
    onSuccess(object);
}
)
},";
                    }
                    else
                    {
                        functionString += paramString + @", onSuccess) {
$.post(
'/" + objectName + "/" + mInfo.Name + @"',
"+ dataString +@",
function(result){
var object = JSON.parse(result);
if(onSuccess) 
    onSuccess(object);
}
, 'json')
},";                
                    }
                    
                    methodsScript += functionString;
                }

                methodsScript = methodsScript.TrimEnd(',');

                if (methodsScript != "")
                    ctrlScripts += @"
XbimProxy." + objectName + @" = function(){};
XbimProxy." + objectName + @".prototype = {
" + methodsScript + @"
}
";
            }

            var result = new JavaScriptResult();
            result.Script = ctrlScripts;
            return result;
        }

    }
}
