//Adds the types to the Scene and UI tree at a specified Node
function AddTypes(Scene, NodeAt, TypesArray) {
    var rootUiTreeNode = $("#navtree").dynatree("getRoot");
    for (var i = 0; i < TypesArray.length; i++) {
        var Type = TypesArray[i];
        // add to the UI tree interface
        rootUiTreeNode.addChild({ "title": Type, "tooltip": "Type", "key": Type, "select": true });

        // add to the 3d scene
        if (!Scene.findNode(Type)) {
            NodeAt.add("node", CreateType(Type));
        }
    }
}
//Creates a JSON type for the SceneJS tree
function CreateType(Name) {
    return {
        "type": "layer",
        "id": Name + "_layer",
        "nodes": [
        {
            "type": "tag",
            "tag": Name,
            "id": Name,
            "nodes": [
                {
                    "type": "material",
                    "id": Name + "Mat",
                    "coreId": Name + "Material"
    }
            ]
}
        ]
    }
}

//Adds an item to the shared library
function AddItemToSceneLibrary(Scene, Item) {
    var lib = Scene.findNode("library");
    if (!lib) {
        Scene.add("node",
            {
                "type": "library",
                "id": "library"
            }
        );
        lib = Scene.findNode("library");
    }
    lib.add("node", Item);
}

//Creates a Material
function CreateMaterial(id, R, G, B, A, Emit, Specular, Shine) {
    if (!A) A = 1.0;
    if (!Emit) Emit = 0.0;
    if (!Specular) Specular = 0.9;
    if (!Shine) Shine = 6.0;

    return {
        "type": "material",
        "coreId": id,
        "baseColor": { "r": R, "g": G, "b": B },
        "alpha": A,
        "emit": Emit,
        "specular": Specular,
        "shine": Shine
    }
}
function UpdateLayers(Scene, Type, Priority) {
    var node = Scene.findNode(Type + "_layer");
    if (node) node.set("priority", Priority);
}

// adds elements to the UI tree and the scene
//
function LoadGeometryHeaders(Scene, Type, Material, IDs, styleNumber) {
    var typenode = Scene.findNode(Type);
    if (typenode == null) {
        alert("type not found.");
    }
    // loops through the nodes to add
    for (var i = 0; i < IDs.length; i++) {
        var uniqueID = IDs[i]; 
        var node = CreateGeometryHeader(uniqueID);
        var vMat = typenode.node(Type + "Mat");
        if (Material != null) {
            vMat.add("node", {
                    "type": "material",
                    "coreId": Material,
                    "nodes": [node]
                    });
        } else {
                vMat.add("node", node);
        }
        
        // preparing UI
        var rootUiTreeNode = $("#navtree").dynatree("getTree").getNodeByKey(Type);
        rootUiTreeNode.addChild({ "title": uniqueID, "tooltip": Type, "key": uniqueID, "select": true });
    }
}
function CreateGeometryHeader(ID) {
    return {
        "type": "shaderParams",
        "params": { "picked": false },
        "nodes": [
            {
                "type": "name",
                "id": ID + "_name",
                "name": ID,
                "nodes": [
                    {
                        "type": "flags",
                        "id": ID + "_flags",
                        "flags": { "enabled": false, "transparent": true, "backfaces": true }
                    }
                ]
            }
        ]
    }
}
function CreateGeometryData(Scene, ID, MeshType, positions, normals, indices, matrix) {
    if (ID == null) {
        try { console.log("ID empty"); } catch (e) { }
        return;
    }
    if (positions == null || positions.length == 0) {
        // alert("positions empty");
        return;
    }
    if (normals == null || normals.length == 0) {
        // alert("normals empty");
        return;
    }
    if (indices == null || indices.length == 0) {
        // alert("indices empty");
        return;
    }

    var mesh = "triangles";
    switch (MeshType) {
        case (4):
            mesh = "triangles";
            break;
        case (5):
            mesh = "triangle-strip";
            break;
        case (6):
            mesh = "triangle-fan";
            break;
    }

    //get the geometry node parent (flags node)
    var geoNodeFlags = Scene.findNode(ID + "_flags");

    if (matrix == null) {
        geoNodeFlags.add("node",
        {
            "type": "geometry",
            "id": ID + "_geo",
            "primitive": mesh,
            "positions": positions,
            "normals": normals,
            "indices": indices
        });

    } else {
        geoNodeFlags.add("node", {
            "type": "matrix",
            "id": ID + "_matrix",
            "elements": matrix,
            "nodes": [
                {
                    "type": "geometry",
                    "id": ID + "_geo",
                    "primitive": mesh,
                    "positions": positions,
                    "normals": normals,
                    "indices": indices
                }
            ]
        });
    }

    //enable the rendering of the node
    var flags = geoNodeFlags.get("flags");
    flags.enabled = true;
    geoNodeFlags.set("flags", flags);
}