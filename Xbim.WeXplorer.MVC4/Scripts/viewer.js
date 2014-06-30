requirejs.config({
    paths: {
        jquery: "jquery-2.1.1",
        bootstrap: "bootstrap",
        ace: "editor/ace"
    },
    shim: {
        bootstrap: {
            deps: ["jquery"]
        },
        scenejs: {
            deps: ["jquery"],
            exports: "SceneJS"
        },
        "viewer-ui-init": {
            deps: ["ace"]
        },
        ace: {
            exports: "ace"
        }
    }
});

define(['jquery', 'bootstrap', 'scenejs', 'modelloader', 'eventmanager', 'observables', 'viewer-ui-init'], function ($, bootstrap, scenejs, ModelLoader, eventmanager, undefined, observ, uinit) {

    // Point SceneJS to the bundled plugins
    SceneJS.setConfigs({
        pluginPath: "/Scripts/plugins"
    });

    // Create scene
    // Create scene
    var scene = SceneJS.createScene({

        id: "XbimModelScene",

        // Link to our canvas element
        canvasId: "modelcanvas",

        nodes: [
            //// Mouse-orbited camera, defined by
            //// plugin in http://scenejs.org/api/latest/plugins/node/cameras/orbit.js
            //{
            //    type: "cameras/orbit",
            //    yaw: 30,
            //    pitch: -25,
            //    zoomSensitivity: 3.0,
            //    zoom: 50,
            //    id: "camera",
            //    nodes: [
            //        {
            //            type: "matrix", //rotate x by 90degrees so the SceneJS Cameras operate on the correct up plane
            //            elements: [1, 0, 0, 0,
            //                       0, 0, -1, 0,
            //                       0, 1, 0, 0,
            //                       0, 0, 0, 1],
            //            nodes: [
            //                {
            //                    type: "flags", id: "flags", flags: { transparent: false, picking: true, backfaces: true }
            //                }
            //            ]
            //        }
            //    ]
            //}
        ]
    });
    var canvas = scene.getCanvas();

    eventmanager.RegisterCallback("CreateSceneOutline", function (sceneOutline) {
        scene.getNode("XbimModelScene", function (XbimModelScene) {
            XbimModelScene.addNode(sceneOutline);
        });

        var scene = SceneJS.createScene(sceneOutline);
        var canvas = scene.getCanvas();
    });

    eventmanager.RegisterCallback("ModelBounds", function (ModelBounds) {
        //console.log(ModelBounds);

        var transform = ModelBounds;
        scene.getNode("flags", function (flags) {
            flags.addNode({
                type: "matrix",
                id: "modeltransform",
                elements: transform,
                nodes: []
            });
        });
    });
    eventmanager.RegisterCallback("Materials", function (Materials) {
        //console.log(Materials);

        scene.getNode("modeltransform", function (modeltransform) {
            for (var key in Materials) {
                var mat = Materials[key];
                var flags = modeltransform.addNode({
                    type: "flags",
                    flags: { transparent: mat.Style[0].Alpha < 0.99999 }
                });
                //note at the moment we only handle a single style for a material (Style[0])
                flags.addNode({
                    type: "material",
                    id: mat.Id,
                    color: { r: mat.Style[0].Red, g: mat.Style[0].Green, b: mat.Style[0].Blue },
                    alpha: mat.Style[0].Alpha,
                    nodes: []
                });
            }
        });
    });

    eventmanager.RegisterCallback("Geometry", function (Geometry) {
        try {
            scene.getNode(Geometry.layerid, function (MaterialNode) {
                MaterialNode.addNode({
                    type: "name",
                    id: Geometry.data.prod + "_" + Geometry.id + "_" + Geometry.mapid + "_name",
                    nodes: [{
                        type: "geometry",
                        id: Geometry.data.prod + "_" + Geometry.id + "_" + Geometry.mapid,
                        data: { product: Geometry.data.prod },
                        primitive: "triangles",
                        positions: Geometry.Positions,
                        normals: Geometry.Normals,
                        indices: Geometry.Indices
                    }]
                });
            });
        } catch (exception) { console.log(exception); }
    });

    eventmanager.RegisterCallback("Box", function (geom) {
        try {
            scene.getNode(geom.Style, function (MaterialNode) {
                MaterialNode.addNode({
                    type: "matrix",
                    elements: geom.Trans,
                    id: geom.Prod + "_" + geom.Id,
                    nodes: [{
                        type: "translate",
                        x: geom.Box.X + geom.Box.SizeX / 2, y: geom.Box.Y + geom.Box.SizeY / 2, z: geom.Box.Z + geom.Box.SizeZ / 2,
                        nodes: [{
                            type: "prims/box",
   
                            data: { product: geom.Prod },
                            xSize: geom.Box.SizeX / 2,
                            ySize: geom.Box.SizeY / 2,
                            zSize: geom.Box.SizeZ / 2,
                        }]
                    }]
                });
            });
        } catch (exception) { console.log(exception); }
    });

    //register mouse handlers for picking
    canvas.addEventListener('mousedown', mouseDown, true);
    canvas.addEventListener('mouseup', mouseUp, true);
    canvas.addEventListener('touchstart', touchStart, true);
    canvas.addEventListener('touchend', touchEnd, true);

    var downtime;
    function mouseDown(event) {
        lastX = event.clientX;
        lastY = event.clientY;
        dragging = true;
        downtime = new Date();
    }

    function touchStart(event) {
        lastX = event.targetTouches[0].clientX;
        lastY = event.targetTouches[0].clientY;
        dragging = true;
        downtime = new Date();
    }

    function mouseUp(event) {
        var time = new Date() - downtime;
        if (dragging && time < 250) { //assume a click event if we take less than a 1/4 second between mousedown and up
            scene.pick(event.clientX, event.clientY);
        }
        dragging = false;
    }

    function touchEnd() {
        var time = new Date() - downtime;
        if (dragging && time < 250) { //assume a click event if we take less than a 1/4 second between touchstart and up
            scene.pick(event.targetTouches[0].clientX, event.targetTouches[0].clientY);
        }
        dragging = false;
        
    }
    scene.on("pick",
            function (hit) {
                var ids = hit.nodeId.split("_");
                console.log("picked geometry id: " + ids[1] + " for product id: " + ids[0]);
                //var geometryid = hit.nodeId.replace("_name","");
                //alert("picked geometry id: " + ids[1] + " for product id: " + ids[0]);

                var pId = ids[0];
                var proxy = new XbimProxy.Properties();
                var properties = proxy.GetProperties(ModelID, pId, function (result) {
                    var pSets = result;
                    var collection = new ObservableCollection('selected-properties');
                    for (var i in pSets) {
                        var prop = new Observable(pSets[i].Label);
                        prop.fillFromObject(pSets[i]);
                        collection.add(prop);
                    }
                });
            });
    ModelLoader.StartLoading();
});