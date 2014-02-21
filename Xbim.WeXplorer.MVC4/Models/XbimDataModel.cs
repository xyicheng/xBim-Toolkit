using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Xbim.IO;
using Xbim.XbimExtensions;

namespace Xbim.WeXplorer.MVC4.Models
{
    public class XbimDataModel : IDisposable
    {
        protected XbimModel model;
        string _name;
        string _xbimName;
       

        public void Dispose()
        {
            if (model != null)
            {
                model.Close();
                model = null;
            }

        }

        public string Name
        {
            get { return _name; }
        }

        public string XbimName
        {
            get { return _xbimName; }
        }

        /// <summary>
        /// Create a new data model
        /// </summary>
        /// <param name="model">A model that is Open at least read only</param>
        public XbimDataModel(string xbimFileName)
        {
            
            if (System.IO.File.Exists(xbimFileName))
            {
                model = new XbimModel();
                model.Open(xbimFileName);
                _name = System.IO.Path.GetFileNameWithoutExtension(xbimFileName);
                _xbimName = xbimFileName;
            }
            
        }


        public String Summary()
        {
            var header = model.Header;
            dynamic summary = new
            { 
                Model = _name, 
                Error = string.Empty,
                Name = header.Name,
                Created = header.TimeStamp,
                Schema = header.SchemaVersion,
                View = header.ModelViewDefinition,
                CreatedBy = header.CreatingApplication,
                InstanceCount = model.Instances.Count,
                GeometryVersion = model.GeometrySupportLevel,
                GeometryCount = model.GeometriesCount  
            };
            return JsonConvert.SerializeObject(summary); 
        }



    }
}