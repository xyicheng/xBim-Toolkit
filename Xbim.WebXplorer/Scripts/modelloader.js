define(['jquery', 'eventmanager', 'ModelProcessor'], function ($, eventmanager, ModelProcessor, undefined) {
    var ModelLoader = function ()
    {
        this.GeoLayer = [];
        this.GeoQueue = [];
        this.MapQueue = [];
        this.BATCHAMOUNT = 20;
        this.StartTime;
        this.EndTime;
    };
    ModelLoader.prototype.StartLoading = function () {
        this.StartTime = new Date();
        this.GetData("GeometrySupportLevel", this.HandleGeometrySupportLevel); 
    }
    ModelLoader.prototype.HandleGeometrySupportLevel = function (self, data) {

        //if we aren't looking at level 2, then quit out
        self.GetData("ModelContext", self.HandleModelContext);
    }
    ModelLoader.prototype.HandleModelContext = function (self, data) {
        ModelProcessor.HandleContext(data);
        self.GetData("LibraryStyles", self.HandleLibraryStyles);
    }
    ModelLoader.prototype.HandleLibraryStyles = function (self, data) {
        ModelProcessor.HandleLibraryStyles(data);
        self.GetData("LibraryShapes", self.HandleLibraryShapes);
    }
    ModelLoader.prototype.HandleLibraryShapes = function (self, data) {
        self.MapQueue = ModelProcessor.HandleLibraryShapes(data);
        self.GetData("ProductShapes", self.HandleProductShapes);
    }
    ModelLoader.prototype.HandleProductShapes = function (self, data) {
        self.GeoQueue = ModelProcessor.HandleProductShapes(data);
        self.HandleMapGeometry(self);
    }
    ModelLoader.prototype.HandleMapGeometry = function (self, data) {

        //process this batch
        if (data) { ModelProcessor.HandleMapGeometry(data); }

        //request next batch if we have still got stuff queued
        if (self.MapQueue.length > 0) {

            var geomid = [];
            for (var i = 0; i < self.BATCHAMOUNT && self.MapQueue.length > 0; i++) {
                geomid.push(self.MapQueue.pop());
            }

            self.GetData("Geometry", self.HandleMapGeometry, geomid.toString());
        //otherwise start on the product shapes
        } else {
            self.HandleGeometry(self);
        }        
    }
    ModelLoader.prototype.HandleGeometry = function (self, data) {
        //process this batch
        if (data) { ModelProcessor.HandleGeometry(data); }

        //request next batch if we have still got stuff queued
        if (self.GeoQueue.length > 0) {

            var geomid = [];
            for (var i = 0; i < self.BATCHAMOUNT && self.GeoQueue.length > 0; i++) {
                geomid.push(self.GeoQueue.pop());
            }

            self.GetData("Geometry", self.HandleGeometry, geomid.toString());
        } else {
            //done
            self.EndTime = new Date();
            console.log("finished loading in "+(self.EndTime-self.StartTime)+"ms");
        }
    }
    ModelLoader.prototype.GetData = function (Type, Callback, IDs) {
        var self = this;
            $.ajax({
                url: "/Xbim.WebXplorer/XbimModel/" + Type + "/" + ModelID + "/" + IDs
            })
          .done(function (data) {
              var obj = JSON.parse(data);
              Callback(self, obj);
          });
    }

    return new ModelLoader();
});