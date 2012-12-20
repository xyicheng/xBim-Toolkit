//Adds the types to the Scene at a specified Node
function AddTypes(Scene, NodeAt, Types) {
    for (var i = 0; i < Types.length; i++) {
        AddType(Scene, NodeAt, Types[i]);
    }
}
function AddType(Scene, NodeAt, Type) {
    if (!Scene.findNode(Type)) {
        NodeAt.add("node", CreateType(Type));
    }
    currentNode = $("#navtree").dynatree("getRoot");
    currentNode.addChild({ "title": Type, "tooltip": "Type", "key": Type, "select": true });
}

//Adds an item to the shared library
function AddItemToLibrary(Scene, Item) {
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
function LoadGeometryHeaders(Scene, Type, Material, IDs) {
    var typenode = Scene.findNode(Type);
    if (typenode == null) {
        alert("type not found.");
    }
    for (var i = 0; i < IDs.length; i++) {
        var node = CreateGeometryHeader(IDs[i], IDs[i]);
        var vMat = typenode.node(Type + "Mat");
        if (vMat == null) {
            
        }
        if (Material != null) {
            vMat.add("node",
                {
                    "type": "material",
                    "coreId": Material,
                    "nodes": [node]
                }
            );
        } else {
                vMat.add("node", node);
        }
        currentNode = $("#navtree").dynatree("getTree").getNodeByKey(Type);
        currentNode.addChild({ "title": IDs[i], "tooltip": Type, "key": IDs[i], "select": true });
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
        alert("positions empty");
        return;
    }
    if (normals == null || normals.length == 0) {
        alert("normals empty");
        return;
    }
    if (indices == null || indices.length == 0) {
        alert("indices empty");
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
//Creates a JSON type for an IFC/xBim Type
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