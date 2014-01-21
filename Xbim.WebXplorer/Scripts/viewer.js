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
        pluginPath: "http://scenejs.org/api/latest/plugins"
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
                pitch: -30,
                zoom: 10,
                zoomSensitivity: 10.0,
                id: "camera",
                nodes: []
            }
        ]
    });
    eventmanager.RegisterCallback("ModelBounds", function (ModelBounds) {
        console.log(ModelBounds);

        var transform = new Float32Array(ModelBounds.transforms[0].transform);
        scene.getNode("camera", function (camera) {
            camera.addNode({
                type: "matrix",
                id: "modeltransform",
                elements: transform,
                nodes:[]
            });
        });
    });
    eventmanager.RegisterCallback("Materials", function (Materials) {
        console.log(Materials);
        
        scene.getNode("modeltransform", function (modeltransform) {
            for (var i = 0; i < Materials.Materials.length; i++)
            {
                var mat = Materials.Materials[i].Material;
                modeltransform.addNode({
                    type: "material",
                    id: mat.MaterialID,
                    color: { r: mat.Red, g: mat.Green, b: mat.Blue },
                    nodes:[]
                });
            }
        });
    });
    eventmanager.RegisterCallback("Manifest", function (Manifest) {
        console.log(Manifest);
    });
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
                    positions: Geometry.geometry.Positions,
                    normals: Geometry.geometry.Normals,
                    indices: Geometry.geometry.Indices
                }]
            });
        });
        } catch (exception) { }
    });

    ModelLoader.StartLoading();
});