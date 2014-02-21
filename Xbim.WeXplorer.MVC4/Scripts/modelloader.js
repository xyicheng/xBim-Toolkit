define(['jquery', 'eventmanager', 'b64utilities', 'Geometry/GeometryMesher', 'Geometry/gl-matrix.1.3.7', 'Geometry/jdataview'], function ($, eventmanager, b64, GeometryMesher, glmatrix, jDataView, undefined) {
    var ModelLoader = function ()
    {
        this.GeoLayer = [];
        this.GeoQueue = [];
    };
    ModelLoader.prototype.StartLoading = function () {
        this.GetModelBounds();
    };
    ModelLoader.prototype.GetModelBounds = function () {
        var self = this;
        $.ajax({
            url: "/XbimModel/GeometryContext?name=" + ModelID + "&ext=xbim"
        })
      .done(function (data) {
          data = JSON.parse(data);
          for (var i = 0; i < data.transforms.length; i++)
          {
              data.transforms[i].transform = b64.base64DecToArr(data.transforms[i].transform).buffer;
          }
          eventmanager.FireEvent("ModelBounds", data);
          self.GetMaterials();
      });
    }
    ModelLoader.prototype.GetMaterials = function () {
        var self = this;
        $.ajax({
            url: "/XbimModel/Materials/" + ModelID
        })
      .done(function (data) {
          data = JSON.parse(data);
          eventmanager.FireEvent("Materials", data);
          self.GetManifest();
      });
    }
    ModelLoader.prototype.GetManifest = function () {
        var self = this;
        $.ajax({
            url: "/XbimModel/Manifest/" + ModelID
        })
      .done(function (data) {
          data = JSON.parse(data);

          for (var i = 0; i < data.TypeList.length; i++)
          {
              var LayerID = data.TypeNames[i];
              for (var j = 0; j < data.TypeList[i].length; j++)
              {
                  var geomid = data.TypeList[i][j];
                  self.GeoLayer[geomid] = LayerID;
                  self.GeoQueue.push(geomid);
              }
          }

          eventmanager.FireEvent("Manifest", data);
          self.GeomLoad();
      });
    }
    ModelLoader.prototype.GeomLoad = function () {
        var self = this;
        if (self.GeoQueue.length > 0) {

            var geomid = [];
            for (var i = 0; i < 20 && self.GeoQueue.length > 0; i++) {
                geomid.push(self.GeoQueue.pop());
            }

            $.ajax({
                url: "/XbimModel/Geometry/" + ModelID + "/" + geomid.toString()
            }).done(function (data) {
                self.GeomLoad();

                data = JSON.parse(data);
                for (var i = 0; i < data.length; i++) {
                    var geoid = data[i].id;
                    data[i].geo = b64.base64DecToArr(data[i].geo).buffer;
                    data[i].matrix = b64.base64DecToArr(data[i].matrix).buffer;
                    var layerid = self.GeoLayer[geoid];

                    
                    var geom = new GeometryMesher();
                    var br = new jDataView(data[i].geo, 0, data[i].geo.length, true);
        //var modeltransform = mat4.create(this.Transforms[modelIDToUse])
        //var geotransform = mat4.create(new Float32Array(transform));
        //var totaltransform = mat4.create();
       // mat4.multiply(modeltransform, geotransform, totaltransform);
        var obj = geom.Mesh(br, mat4.create(new Float32Array(data[i].matrix)));

                    /**/

                    eventmanager.FireEvent("Geometry", { id: geoid, layerid: layerid, data: data[i], geometry: obj });
                }
            });
        } else {
            eventmanager.FireEvent("FinishedLoading");
        }
    }

    return new ModelLoader();
});