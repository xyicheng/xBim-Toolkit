using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using Xbim.Common.Geometry;
using Xbim.WebXplorer.xbim;

namespace Xbim.WebXplorer.Models
{
    public class XbimSceneModel: IDisposable
    {
        private XbimModelHandler _model = null;
        public XbimSceneModel(XbimModelHandler Model)
        {
            _model = Model;
            _model.Init();
        }
        public String GetModelBounds()
        {
            List<Object> transforms = new List<object>();
            for (var i = 0; i < _model._ModelTransform.Count(); i++)
            {
                var m = _model._ModelTransform[i] * _model._modelTranslation;
                transforms.Add(new { 
                    model = i,
                    transform = m.ToArray(false)
                });
            }
            object data = new { 
                minx = _model._ModelBounds.X,
                miny = _model._ModelBounds.Y,
                minz = _model._ModelBounds.Z,
                maxx = _model._ModelBounds.X + _model._ModelBounds.SizeX,
                maxy = _model._ModelBounds.Y + _model._ModelBounds.SizeY,
                maxz = _model._ModelBounds.Z + _model._ModelBounds.SizeZ,
                transforms = transforms
            };
            return JsonConvert.SerializeObject(data);
        }
        public String GetManifest()
        {
            dynamic types = new
            {
                TypeCount = _model.xLayers.Count,
                TypeNames = _model.xLayers.Keys,
                TypeList = _model.xLayers.Values
            };
            return JsonConvert.SerializeObject(types);
        }
        public String GetMaterials()
        {
            dynamic mats = new
            {
                MaterialCount = _model.xMaterials.Keys.Count,
                Materials = _model.xMaterials.Values
            };
            return JsonConvert.SerializeObject(mats);
        }
        public String GetGeometry(String Ids)
        {
            var data = _model.GetGeometry(Ids);
            List<Object> retval = new List<Object>();
            foreach (var i in data)
            {
                retval.Add(new { 
                    id = i.GeometryLabel,
                    prod = i.IfcProductLabel,
                    geo = i.ShapeData,
                    matrix = XbimMatrix3D.FromArray(i.DataArray2)
                });
            }

            return JsonConvert.SerializeObject(retval);
        }

        public void Dispose()
        {
            if (_model != null) _model.Dispose();
        }
    }
}