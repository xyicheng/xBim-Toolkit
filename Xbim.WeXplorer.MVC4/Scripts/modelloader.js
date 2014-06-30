define(['jquery', 'eventmanager', 'ModelProcessor'], function ($, eventmanager, ModelProcessor, undefined) {
    var ModelLoader = function () {
        this.GeoLayer = [];
        this.GeoQueue = [];
        this.MapQueue = [];
        this.BATCHAMOUNT = 20;
        this.StartTime;
        this.EndTime;
    };
    ModelLoader.prototype.StartLoading = function () {
        this.StartTime = new Date();
        //this.GetData("GeometryVersion", this.HandleGeometrySupportLevel);
       // this.GetData("GeometryContext", this.HandleModelContext);
        this.GetData("GetSceneOutline", this.HandleSceneOutline);
    }

    ModelLoader.prototype.GetSceneOutline = function (self, data) {
        ModelProcessor.CreateSceneOutline(data);
    }

    //ModelLoader.prototype.HandleGeometrySupportLevel = function (self, data) {

    //    //if we aren't looking at level 2, then quit out
    //    self.GetData("GeometryContext", self.HandleModelContext);
    //}
    ModelLoader.prototype.HandleModelContext = function (self, data) {
        ModelProcessor.HandleContext(data);
        self.GetData("Styles", self.HandleLibraryStyles);
    }
    ModelLoader.prototype.HandleLibraryStyles = function (self, data) {
        ModelProcessor.HandleLibraryStyles(data);
        self.GetData("ShapeInstances", self.HandleShapeInstances);
       // self.GetData("ShapeLibrary", self.HandleLibraryShapes);
    }
    ModelLoader.prototype.HandleShapeInstances = function (self, data) {
        ModelProcessor.HandleShapeInstances(data);
    }
    //ModelLoader.prototype.HandleLibraryShapes = function (self, data) {
    //    self.MapQueue = ModelProcessor.HandleLibraryShapes(data);
    //    self.GetData("ProductShapes", self.HandleProductShapes);
    //}
    //ModelLoader.prototype.HandleProductShapes = function (self, data) {
    //    self.GeoQueue = ModelProcessor.HandleProductShapes(data);
    //    self.HandleMapGeometry(self);
    //}
    //ModelLoader.prototype.HandleMapGeometry = function (self, data) {

    //    //process this batch
    //    if (data) { ModelProcessor.HandleMapGeometry(data); }

    //    //request next batch if we have still got stuff queued

    //    if (self.MapQueue && self.MapQueue.length > 0)
    //    {
    //        var geomid = [];
    //        for (var i = 0; i < self.BATCHAMOUNT && self.MapQueue.length > 0; i++) {
    //            geomid.push(self.MapQueue.pop());
    //        }

    //        self.GetData("Meshes", self.HandleMapGeometry, geomid.toString());
    //        //otherwise start on the product shapes
    //    }
    //    else
    //    {
    //        self.HandleGeometry(self);
    //    }
    //}
    //ModelLoader.prototype.HandleGeometry = function (self, data) {
    //    //process this batch
    //    if (data) { ModelProcessor.HandleGeometry(data); }

    //    //request next batch if we have still got stuff queued
    //    if (self.GeoQueue.length > 0) {

    //        var geomid = [];
    //        for (var i = 0; i < self.BATCHAMOUNT && self.GeoQueue.length > 0; i++) {
    //            geomid.push(self.GeoQueue.pop());
    //        }

    //        self.GetData("Meshes", self.HandleGeometry, geomid.toString());
    //    } else {
    //        //done
    //        self.EndTime = new Date();
    //        console.log("finished loading in " + (self.EndTime - self.StartTime) + "ms");
    //    }
    //}
    ModelLoader.prototype.GetData = function (Type, Callback, IDs) {
        var self = this;
        $.ajax({
           // url: "/Xbim.WebXplorer/XbimModel/" + Type + "/" + ModelID + "/" + IDs
            url: "/XbimModel/" + Type + "/?name=" + ModelID + "&ext=xbim&Ids=" + IDs
        })
      .done(function (data) {
          var obj = JSON.parse(data);
          Callback(self, obj);
      });
    }

    return new ModelLoader();
});