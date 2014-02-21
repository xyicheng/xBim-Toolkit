requirejs.config({
    paths: {
        "jquery": "jquery-2.0.2",
        "bootstrap": "bootstrap"
    },
    shim: {
        "bootstrap": {
            deps: ["jquery"]
        },
        "scenejd": {
            deps: ["jquery"],
            exports:"SceneJS"
        }
    }
});

define(['jquery', 'bootstrap', 'scenejs', 'modelloader', 'eventmanager'], function ($, bootstrap, scenejs, ModelLoader, eventmanager, undefined) {

    // Point SceneJS to the bundled plugins
    SceneJS.setConfigs({
        pluginPath: "/Xbim.WebXplorer/Scripts/plugins"
    });

    // Create scene
    var scene = SceneJS.createScene({

        id: "XbimModelScene",

        // Link to our canvas element
        canvasId: "modelcanvas",

        nodes: [

            // Mouse-orbited camera, defined by
            // plugin in http://scenejs.org/api/latest/plugins/node/cameras/orbit.js
            {
                type: "cameras/orbit",
                yaw: 30,
                pitch: 35,
                zoomSensitivity: 3.0,
                zoom: 50,
                id: "camera",
                nodes: [
                    { type: "flags", id: "flags", flags: { transparent: true, picking: true } }
                ]
            }
        ]
    });
    var canvas = scene.getCanvas();
    eventmanager.RegisterCallback("ModelBounds", function (ModelBounds) {
        //console.log(ModelBounds);

        var transform = ModelBounds;
        scene.getNode("flags", function (flags) {
            flags.addNode({
                type: "matrix",
                id: "modeltransform",
                elements: transform,
                nodes:[]
            });
        });
    });
    eventmanager.RegisterCallback("Materials", function (Materials) {
        //console.log(Materials);
        
        scene.getNode("modeltransform", function (modeltransform) {
            for (var key in Materials)
            {
                var mat = Materials[key];
                modeltransform.addNode({
                    type: "material",
                    id: mat.MaterialID,
                    color: { r: mat.Red, g: mat.Green, b: mat.Blue },
                    alpha: mat.Alpha,
                    nodes:[]
                });
            }
        });
    });
    //eventmanager.RegisterCallback("Manifest", function (Manifest) {
    //    console.log(Manifest);
    //});
    eventmanager.RegisterCallback("Geometry", function (Geometry) {
        try {
            scene.getNode(Geometry.layerid, function (MaterialNode) {
                MaterialNode.addNode({
                    type: "name",
                    id:Geometry.id+"_name",
                    nodes: [{
                        type: "geometry",
                        id: Geometry.id,
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

    //register mouse handlers for picking
    canvas.addEventListener('mousedown', mouseDown, true);
    canvas.addEventListener('mouseup', mouseUp, true);
    canvas.addEventListener('touchstart', touchStart, true);
    canvas.addEventListener('touchend', touchEnd, true);
    function mouseDown(event) {
        lastX = event.clientX;
        lastY = event.clientY;
        dragging = true;
    }

    function touchStart(event) {
        lastX = event.targetTouches[0].clientX;
        lastY = event.targetTouches[0].clientY;
        dragging = true;
    }

    function mouseUp(event) {
        if (dragging) {
            scene.pick(event.clientX, event.clientY);
        }
        dragging = false;
    }

    function touchEnd() {
        if (dragging) {
            scene.pick(event.targetTouches[0].clientX, event.targetTouches[0].clientY);
        }
        dragging = false;
    }
    scene.on("pick",
            function (hit) {
                var geometryid = hit.nodeId.replace("_name","");
                alert("picked geometry id: " + geometryid);
            });
    ModelLoader.StartLoading();
});