var streamScene, streamNodeAt;
var batchamount = 20; //the number of geometry pieces to request in each go
var currentBatchAmount = 0;
var LoadCount = 0;
var LoadTotal = 0;
var failedpieces = 0;
var ToLoad = new Array();
var pauseDownload = false; //setting this to true will pause teh geometry streaming after it has finished the current batch. use pauseresume() to toggle

//enums for the different command codes
CommandCode = {
    ModelView: 0,
    SharedMaterials: 1,
    Types: 2,
    GeometryHeaders: 3,
    GeometryData: 4,
    Metadata: 5,
    QueryData: 6
}

//////////////////////////////////////////////////////////
function StartLoadingDynamicModel(Scene, StartAtNode, Model) {
    ModelID = Model;
    
    //reset progress bar
    $("#loadedbar").progressbar({ value: 0 });

    //display the loading screen until we have all the headers
    $("#loadingScreen").height(innerHeight);
    $("#loadingScreen").width(innerWidth);
    $("#loadingScreen").show();

    //save our scene locations, then send the first message to server (get view)
    streamScene = Scene;
    streamNodeAt = StartAtNode;

    //send command to request camera info
    $("#debuginfo").append('<p>Requesting Camera Info</p>');
    connection.send(JSON.stringify({ "command": CommandCode.ModelView, "ModelID": ModelID }));
}
function ModelDataReceived(command, view) {
    //select the right function based on the incoming command type
    switch (command) {
        case CommandCode.ModelView:
            SetupModelView(view);
            if (!Loaded) {
                Loaded = true;
                $("#debuginfo").append('<p>Requesting Shared Materials</p>');
                connection.send(JSON.stringify({ "command": CommandCode.SharedMaterials, "ModelID": ModelID}));
            }
            break;
        case CommandCode.SharedMaterials:
            SetupSharedMaterials(view);
            break;
        case CommandCode.Types:
            SetupTypes(view);
            break;
        case CommandCode.GeometryHeaders:
            SetupGeometryHeaders(view);
            $("#loadingScreen").hide(); //we have our headers, so hide the loading screen
            break;
        case CommandCode.GeometryData:
            SetupGeometryData(view);
            GrabNextGeoPiece();
            break;
        case CommandCode.Metadata:
            SetupData(view);
            break;
        case CommandCode.QueryData:
            ShowData(view);
            break;
        default:
            alert('unknown command sent: ' + command);
            break;
    }
}

function ShowData(view) {

    var len = view.getUint16();
    var message = view.getString(len);

    var obj = jQuery.parseJSON(message);
    if (obj) {
        var id = obj.id;

        $("#debuginfo").append('<p>Received Quick Hover data for ID: ' + id + ', caching it client side</p>');

        SceneJS.scene("Scene").findNode(id + "_name").set("data", obj.data);
        //Set properties data

        $("#quickProperties").html("<p>" + obj.data + "</p>");
    }
}

//Tells the server to send us the next piece(s) of geometry
function GrabNextGeoPiece() {
    //Work out if we have more to stream, and send the appropriate command
    if (LoadCount >= LoadTotal) //if we have finished loading geometry
    {
        connection.send(JSON.stringify({ "command": CommandCode.Metadata, "ModelID": ModelID}));
        if (failedpieces > 0) {
            //alert("Model finished loading. " + failedpieces + " products have no geometry to display");
        }
        //addClassification(ModelID);
    } else {
        if (!pauseDownload) { //if we haven't paused the download
            //Get the lesser of batch size or how many are remaining
            var amount = Math.min(batchamount, ToLoad.length);

            //Reset current batch amount to 0
            currentBatchAmount = 0;

            //Send out as many requests as we can based on batch size and amount remaining
            var message = '';
            for (var i = 0; i < amount; i++) {
                message += ToLoad.pop() + ',';
            }
            if (message != '') {
                $("#debuginfo").append('<p>Requesting geometry for IDs: ' + message + '</p>');
                connection.send(JSON.stringify({ "command": CommandCode.GeometryData, "ModelID": ModelID, "id": message}));
            } else {
                alert('failed to find next piece of geometry to load');
            }
        }
    }
}

//this toggles pausing/resuming of model geometry download (after the current batch completes).
function pauseresume() {
    pauseDownload = !pauseDownload;
    if (!pauseDownload) {
        GrabNextGeoPiece();
    }
}

var InitialModelView;
function ResetModelView() {
    $("#debuginfo").append('<p>(Re)Setting Model View</p>');
    //reset viewing variables
    RotationX = 0;
    RotationY = -75;
    RotationZ = -40;
    moveXAmount = 0;
    moveYAmount = 0;
    moveZAmount = 0;
    flyUp = 0;
    zoomScale = 1;

    //get the largest plane, so we can work out the z distance
    var eyez = Math.max(InitialModelView.maxX - InitialModelView.minX, Math.max(InitialModelView.maxY - InitialModelView.minY, InitialModelView.maxZ - InitialModelView.minZ));

    //setup a sensible modifier for how much we move each tick
    moveMod = eyez / 650;

    var fovy = camera.GetFOV();
    var yTan = Math.tan((fovy / 2) * DEG2RAD);

    var aspect = $("#scenejsCanvas").width() / $("#scenejsCanvas").height();

    camera.SetOptics({ "type": "perspective", "fovy": fovy, "aspect": aspect, "near": (eyez / 1000), "far": (eyez * 100) });

    var setZoom = (1 / yTan) * (eyez / 2) * 1.4;

    var centerX = InitialModelView.minX + ((InitialModelView.maxX - InitialModelView.minX) / 2);
    var centerY = InitialModelView.minY + ((InitialModelView.maxY - InitialModelView.minY) / 2);
    var centerZ = InitialModelView.minZ + ((InitialModelView.maxZ - InitialModelView.minZ) / 2);

    //set eye
    camera.SetPosition({ "x": 0, "y": 0, "z": setZoom }, { "x": 0, "y": 0, "z": setZoom - 1 });

    //set focal point as the centre of the model bounds
    SceneJS.scene("Scene").findNode("move").set("xyz", { "x": -centerX, "y": -centerY, "z": -centerZ });

    //set up plane
    newInput = true;
}
function SetupModelView(view) {

    /////////////////////////////
    var minX = view.getFloat64();
    var minY = view.getFloat64();
    var minZ = view.getFloat64();
    var maxX = view.getFloat64();
    var maxY = view.getFloat64();
    var maxZ = view.getFloat64();

    InitialModelView = { "minX": minX, "minY": minY, "minZ": minZ, "maxX": maxX, "maxY": maxY, "maxZ": maxZ };

    ResetModelView();
}
function SetupSharedMaterials(view) {
    var count = view.getUint16();
    $("#debuginfo").append('<p>Received ' + count + ' shared materials</p>');
    for (var i = 0; i < count; i++) {
        var length = view.getUint16();
        //                                 name                    R                  G                  B                  Alpha             Emit
        var material = CreateMaterial(view.getString(length), view.getFloat64(), view.getFloat64(), view.getFloat64(), view.getFloat64(), view.getFloat64());
        AddItemToLibrary(streamScene, material);
    }

    $("#debuginfo").append('<p>Requesting Type Info</p>');
    connection.send(JSON.stringify({ "command": CommandCode.Types, "ModelID": ModelID}));
}
function SetupTypes(view) {
    var count = view.getUint16();
    var type = new Array(count);

    for (var i = 0; i < count; i++) {
        var length = view.getUint16();
        type[i] = view.getString(length);
    }
    $("#debuginfo").append('<p>Received ' + count + ' types: ' + type.toString() + '</p>');

    AddTypes(streamScene, streamNodeAt, type);

    $("#debuginfo").append('<p>Requesting Geometry Headers</p>');
    connection.send(JSON.stringify({ "command": CommandCode.GeometryHeaders, "ModelID": ModelID}));
}
function SetupGeometryHeaders(view) {
    var d1 = new Date();

    var countStyles = view.getUint16(); //total geometry types
    var totalcount = view.getUint16(); //total geometry

    $("#debuginfo").append('<p>Received headers for ' + countStyles + ' geometry styles, with a total of ' + totalcount + ' pieces of geometry</p>');

    var addedCount = 0;
    ToLoad = new Array(totalcount);
    for (var iCurStyle = 0; iCurStyle < countStyles; iCurStyle++) {
        //type
        var typeLen = view.getUint16();
        var typeName = view.getString(typeLen);
        //material
        var matLen = view.getUint16();
        var matName = view.getString(matLen);

        var layer = view.getInt16(); // has to do with transparency
        UpdateLayers(streamScene, typeName, layer);

        //geometry details
        var geomcount = view.getUint16();
        var geometryLabels = new Array(geomcount);
        for (var j = 0; j < geomcount; j++) {
            var id = view.getInt32();
            geometryLabels[j] = id;
            ToLoad[addedCount] = id;
            addedCount++;
        }
        LoadGeometryHeaders(streamScene, typeName, matName, geometryLabels, iCurStyle);
    }

    //Setup total number to load and fire off the first load
    LoadTotal = ToLoad.length;

    GrabNextGeoPiece();
}
function updateProgressBar() {
    $("#loaded").text(('Loaded ' + LoadCount + '/' + LoadTotal + ' items'));
    $("#loadedbar").progressbar("option", "value", ((LoadCount / LoadTotal) * 100));
    if (LoadCount == LoadTotal) {
        $("#loadedWrapper").hide();
    }
}
function SetupGeometryData(view) {
    // bonghi updated version
    var loaded = LoadCount;
    var count = view.getUint16(); //how is the system trying to send 
    // failedpieces++; // when needed
    // LoadCount += brokenitems;
    // currentBatchAmount += count + brokenitems;

    // $("#debuginfo").append('<p>Received geometry for ' + count + ' items, with ' + brokenitems + ' items lacking geometry data (either serverside/processing failure, or no geometry available)</p>');

    // updateProgressBar();

    for (var c = 0; c < count; c++) {
        LoadCount++;
        // updateProgressBar();

        var ID = view.getUint32();
        $("#debuginfo").append('<p>Processing Geometry for ID: ' + ID + '</p>');
        if (ID == 0) {
            alert("debug-me-plz");
            continue;
        }

        var HasData = view.getInt8();
        if (HasData == 0) {
            failedpieces++;
        } else {
            // this is where the data comes from GetPNIGeometryData

            //get transform matrix
            var matrix = new Array(16);
            for (var l = 0; l < 16; l++) {
                var item = view.getFloat64();
                matrix[l] = item;
            }

            // get initial metrics
            var NumPosNormals = view.getUint32();
            var NumTriangles = view.getUint32();

            // initialise local arrays
            var positions = new Float64Array(NumPosNormals * 3);
            var normals = new Float64Array(NumPosNormals * 3);
            var indices = new Int32Array(NumTriangles * 3);

            for (var i = 0; i < NumPosNormals; i++) {
                positions[i * 3 + 0] = view.getFloat32();
                positions[i * 3 + 1] = view.getFloat32();
                positions[i * 3 + 2] = view.getFloat32();

                normals[i * 3 + 0] = view.getFloat32();
                normals[i * 3 + 1] = view.getFloat32();
                normals[i * 3 + 2] = view.getFloat32();
            }
            for (var i = 0; i < NumTriangles; i++) {
                indices[i * 3 + 0] = view.getUint32();
                indices[i * 3 + 1] = view.getUint32();
                indices[i * 3 + 2] = view.getUint32();
            }
            CreateGeometryData(streamScene, ID, 4, positions, normals, indices, matrix);
            //cleanup
            delete positions;
            delete normals;
            delete indices;
        }
    }
    //clean up
    delete view;
}
function SetupData(view) {
    newInput = true;
}

///////////// Helper functions for base64 encoding UTF8
function utf8_to_b64(str) {
    return window.btoa(str);
}

function b64_to_utf8(str) {
    return window.atob(str);
}
