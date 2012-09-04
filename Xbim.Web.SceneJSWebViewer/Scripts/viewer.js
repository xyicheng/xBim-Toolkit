
var connection = new WebSocket("ws://localhost:81/");

$(window).load(function () {
    $(document).ready(function () {

        connection.binaryType = "arraybuffer";
        connection.onopen = function () {
        };
        connection.onmessage = function (e) {
            var view;
            if (e.data instanceof ArrayBuffer) { //if we have it in binary, then skip straight to the jDataView
                view = new jDataView(e.data, 0, e.data.length, e.data[1] == 1)
            } else { //otherwise we have to base64 decode it first
                var byteString = b64_to_utf8(e.data);

                var ab = new ArrayBuffer(byteString.length);
                var ia = new Uint8Array(ab);
                for (var i = 0; i < byteString.length; i++) {
                    ia[i] = byteString.charCodeAt(i);
                }
                delete byteString;

                //create a view of the buffer, allowing us to read bits, floats, strings etc from the array
                view = new jDataView(ab, 0, ab.length, ia[1] == 1);
            }
            var command = view.getInt8();
            view.getInt8(); //endian flag. already have it, so just move pointer by uint8 size

            //handle our data received
            ModelDataReceived(command, view);
        };
        connection.onclose = function () {
            alert("closed");
        };

        //setup bottom tab
        $("#help").accordion({
            autoHeight: false,
            navigation: true
        });

        //setup nav tree
        $("#navtree").dynatree({
            checkbox: true,
            selectMode: 3,
            onSelect: function (select, node) {
                var scenenode = SceneJS.scene("Scene").findNode(node.data.key + "_name");
                if (!scenenode) scenenode = SceneJS.scene("Scene").findNode(node.data.key);
                if (scenenode) ChangeVisibility(scenenode, select);
            },
            onClick: function (node, event) {
                if (node.getEventTargetType(event) == 'title') {
                    ClickPickItem({ "name": node.data.key });
                }
            }
        });

        $("#navtree a").live('hover', function (e) { HoverPickItem({ "name": this.text }); });

        SceneJS.createScene({ "id": "Scene", "canvasId": "scenejsCanvas", "contextAttr": { "antialias": true} });
        SceneJS.scene("Scene").add("node", basescene);

        camera = new Camera(SceneJS.scene("Scene").findNode("zoom"));
        camera.SetAspectRatio($("#scenejsCanvas").width(), $("#scenejsCanvas").height());

        InitScene();
    });
});
function toggleantialias() {
    var scene = SceneJS.scene("Scene");
    var attr = scene.get("contextAttr");
    scene.set("contextAttr", { "antialias": !attr.antialias });
}
$(window).resize(function () {

    if (camera)
        camera.SetAspectRatio($("#scenejsCanvas").width(), $("#scenejsCanvas").height());

    setupDivBoundsWhenLeavingViewport("#types", 0, 0);
    setupDivBoundsWhenLeavingViewport("#properties", 0, $("#scenejsCanvas").width() - $("#properties").width());

    //fix for picking problems on resize
    SceneJS.scene("Scene").resetPick($(window).width(), $(window).height());
});

function ZoomExtents() {
    ResetModelView();
}
function setupDivBoundsWhenLeavingViewport(div, top, left) {
    $(div + " span:below-the-fold").each(function () {
        $(div).css('top', top).css('left', left);
    });
    $(div + " span:above-the-top").each(function () {
        $(div).css('top', top).css('left', left);
    });
    $(div + " span:left-of-screen").each(function () {
        $(div).css('top', top).css('left', left);
    });
    $(div + " span:right-of-screen").each(function () {
        $(div).css('top', top).css('left', left);
    });
}

//Iterates over the node and all child nodes, setting the visibility flag
function ChangeVisibility(StartNode, Visibility) {
    StartNode.eachNode(function () {
        if (this.get("type") == "flags") {
            var flags = this.get("flags");
            flags.enabled = Visibility;
            this.set("flags", flags);
        }
    }, { andSelf: true, depthFirst: true });
}
//Setup our globals (yuck!) for various nodes
var Loaded = false;
var RotationX = 0;
var RotationY = 0;
var RotationZ = 0;
var moveXAmount = 0;
var moveYAmount = 0;
var moveZAmount = 0;
var flyUp = 0;
var zoomScale = 1;
var HubActive = false;
var IsController = false;
var lastUpdate = new Date().getTime();
var moveMod = 65;
var DEG2RAD = Math.PI / 180;
var camera;
var canvas;
var vertices = 0;var ModelID = null;


//setup control methods. for now, only orbit control
var orbitcontrol = new orbit();
var controlMethod = orbitcontrol;
var elapsed = 100;

//flag for whether we need to alter the transforms on render due to updated input from mouse/keys etc
var newInput = false;
var flyInput = false;

var lastX = 0;
var lastY = 0;

//Transparency flag
Transparent = false;

function ClickPickItem(item) {
    //try to get the actual node based on the picked item name
    var obj = SceneJS.scene("Scene").findNode(item.name + "_name");
    if (obj && obj.get("type") == 'name') {

        //Remove existing pick
        if (lastPicked) {
            var shadernode = lastPicked.parent();
            shadernode.set("params", { picked: false });

            $("#properties").hide();
        }

        //check if the last node is also this node (ie same item picked twice)
        if (lastPicked == obj) {

            lastPicked = null; //reset the last picked item as we have unpicked
            $("#properties").hide();

        } else {
            obj.parent().set("params", { picked: true, colorTransScale: [0.0, 0.0, 0.6] });
            lastPicked = obj;

            var message = { "command": CommandCode.QueryData, "id": item.name, "query": "this is a query", "ModelID": ModelID};
            connection.send(JSON.stringify(message));

            $("#properties").show();
        }
    }
}

var showQuickProperties = 1;

function HoverPickItem(item) {
    var obj = SceneJS.scene("Scene").findNode(item.name + "_name");

    if (obj && obj.get("type") == 'name') {

        var data = obj.get("data");

        var shadernode;
        if (lastHoveredItem && lastHoveredItem != obj && lastHoveredItem != lastPicked) {
            //if we aren't hovering on the same item, or a picked item
            shadernode = lastHoveredItem.parent();
            shadernode.set("params", { picked: false });
            lastHoveredItem = null;

            //Set flag to show menu on moving to different item
            showQuickProperties = 1;
            $("#quickProperties").hide();
        }

        if (lastPicked != obj) {  //if we aren't on a picked item
            shadernode = obj.parent();
            shadernode.set("params", { picked: true, colorTransScale: [0.0, 0.6, 0.6] });
            lastHoveredItem = obj;

            if (data) $("#quickProperties").html("<p>" + data + "</p>");
            else {
                var message = { "command": CommandCode.QueryData, "id": item.name, "query": "this is a query", "ModelID": ModelID };
                connection.send(JSON.stringify(message));
            }

            //hovering
            if (showQuickProperties == 1) {
                $("#quickProperties").css({ position: "absolute", top: lastY + 30, left: lastX + 30, opacity: 1 })
                $("#quickProperties").show()
                                     .delay(1500)
                                     .fadeIn("fast")
                                     .delay(5000)
                                     .fadeOut("fast", function () {
                                         $("#quickProperties").css('opacity', 0);
                                         $("#quickProperties").hide();
                                         showQuickProperties = 0;
                                     });
            }
        }
    } else {
        //hovered out of the model
        if (lastHoveredItem && lastHoveredItem != lastPicked) {
            shadernode = lastHoveredItem.parent();
            shadernode.set("params", { picked: false });
            lastHoveredItem = null;

            $("#quickProperties").hide();
        }
    }
}

//last picked node in the scene graph
var lastPicked = null;
var lastHoveredItem;
var LastFrame = new Date().getTime();
var CurrentFrame;
var SceneStarter = {
    "fps": 25,
    "requestAnimationFrame": false,
    "idleFunc": function (timestamp) {
        CurrentFrame = new Date().getTime(); //note: this call allocates memory during render :( but is unavoidable for now if we want a timer
        elapsed = CurrentFrame - LastFrame;
        LastFrame = CurrentFrame;

        //get keyboard updates
        if (controlMethod.keyboard)
            controlMethod.keyboard(elapsed);

        camera.CalculateRotation();

        if (newInput) { //if we have changed things using mouse input

            if (flyUp < 0) {
                moveYAmount += moveMod; //flying down
            } else if (flyUp > 0) {
                moveYAmount -= moveMod; //flying up
            } else { //only mark as no input if we aren't flying up
                newInput = false;
            }

            SceneJS.scene("Scene").findNode("rotX").set("angle", RotationY);
            SceneJS.scene("Scene").findNode("rotZ").set("angle", RotationZ);

            SceneJS.scene("Scene").findNode("plus").set("x", moveXAmount);
            SceneJS.scene("Scene").findNode("plus").set("y", moveYAmount);
            SceneJS.scene("Scene").findNode("plus").set("z", moveZAmount);

            SceneJS.scene("Scene").findNode("scale").set("x", zoomScale);
            SceneJS.scene("Scene").findNode("scale").set("y", zoomScale);
            SceneJS.scene("Scene").findNode("scale").set("z", zoomScale);
        }
    }
};
function changeControlMethod(newmethod) {
    controlMethod = newmethod;
    controlMethod.init();
}
function InitScene() {
    canvas = document.getElementById("scenejsCanvas");

    orbitcontrol.fullscreenelement = canvas;

    //Enable scene graph compilation to speed things up
    SceneJS.setDebugConfigs({
        compilation: {
            enabled: true
        }
    });

    //mouse movement variables
    var dragging = false;
    var leftDown = false;
    var rightDown = false;
    var mouseDownTime = 0;

    //setup the rendering function
    SceneJS.scene("Scene").start(SceneStarter);

    //Handler for mouse down
    function mouseDown(event) {
        dragging = true;

        var mouseLeft = (event.pageX - canvas.offsetLeft);
        var mouseTop = (event.pageY - canvas.offsetTop);

        lastX = mouseLeft;
        lastY = mouseTop;

        switch (event.which) {
            case 1:
                leftDown = true;
                break;
            case 3:
                rightDown = true;
                break;
            default:
                break;
        }

        mouseDownTime = new Date().getTime();

        if (controlMethod.mousedown)
            controlMethod.mousedown(event);
    }

    //Handler for mouse up (if mouse was clicked quickly it's a pick, otherwise we are stopping a drag)
    function mouseUp(event) {

        dragging = false;

        var mouseLeft = (event.pageX - canvas.offsetLeft);
        var mouseTop = (event.pageY - canvas.offsetTop);
        lastX = mouseLeft;
        lastY = mouseTop;

        //check whether the mouseup occurred within 1/4 sec of mousedown - indicates a click rather than a drag
        var now = new Date();
        if (now.getTime() - mouseDownTime < 250) {

            //if its just a left mouse click, then its a pick
            if (leftDown && !rightDown) {
                var item = SceneJS.scene("Scene").pick(mouseLeft, mouseTop);

                if (item) {  //check if we found an item
                    ClickPickItem(item);
                }
            }
        }

        //if we weren't picking then we were dragging
        switch (event.which) {
            case 1:
                leftDown = false;
                break;
            case 3:
                rightDown = false;
                break;
            default:
                break;
        }

        if (controlMethod.mouseup)
            controlMethod.mouseup(event);
    }

    function orbitMouse(event) {
        orbit.mousemove(event);
    }

    function flyMouse(event) {
        walk.mousemove(event);
    }
    //On a mouse drag, we re-render the scene, passing in incremented angles in each time.
    function mouseMove(event) {
        var mouseLeft = (event.pageX - canvas.offsetLeft);
        var mouseTop = (event.pageY - canvas.offsetTop);
        lastX = mouseLeft;
        lastY = mouseTop;

        if (dragging | document.pointerLockEnabled) {
            //hide quick properties
            $("#quickProperties").hide();

            //Make menus transparent while dragging
            $("#types").css({ opacity: 0.5 });
            $("#properties").css({ opacity: 0.5 });

            if (controlMethod.mousemove)
                controlMethod.mousemove(event, leftDown, rightDown, dragging);

            if (rightDown) { //stop right click context menu from popping up
                if (event.stopPropagation)
                    event.stopPropagation();

                event.cancelBubble = true;
            }
        } else {
            //not dragging revert opacity of menus
            $("#types").css({ opacity: 1 });
            $("#properties").css({ opacity: 1 });

            if (event.currentTarget.attributes.id.value != "properties" && event.currentTarget.attributes.id.value != "types") {
                //if we aren't dragging, then update the hover picker with the id
                var item = null;
                if (mouseLeft > 0 && mouseTop > 0) {
                    item = SceneJS.scene("Scene").pick(mouseLeft, mouseTop);
                }
            }

            if (item && !controlMethod.mouselook) {
                //prevent hoverpick when mouselooking
                HoverPickItem(item);
            }
            else {
                $("#quickProperties").hide();
            }
        }

    }
    function mouseWheel(event) {
        if (controlMethod.mousewheel)
            controlMethod.mousewheel(event);
    }

    function keyDown(event) {
        if (event.keyCode == 46) { //delete key pressed
            var key = new String();
            key = lastHoveredItem.get("id");
            key = key.replace("_name", "");
            $("#navtree").dynatree("getTree").selectKey(key, false);
        }

        if (controlMethod.keydown)
            controlMethod.keydown(event);
    }
    function keyUp(event) {
        if (controlMethod.keyup)
            controlMethod.keyup(event);
    }

    /*Add events to menus's so model rotation doesnt stop when
    mouse is over them whilst rotating fullscreenelement*/
    $("#types").mousemove(function (e) { mouseMove(e); });
    $("#properties").mousemove(function (e) { mouseMove(e); });
    $("#types").bind("mouseup", mouseUp)
    $("#properties").bind("mouseup", mouseUp)

    //Add event listeners
    canvas.addEventListener('mousedown', mouseDown, true);
    canvas.addEventListener("mousemove", mouseMove, true);
    canvas.addEventListener('mouseup', mouseUp, true);
    canvas.oncontextmenu = function () { return false; };
    $("*").keydown(function (e) { keyDown(e); });
    $("*").keyup(function (e) { keyUp(e); });
    if (window.addEventListener) {
        canvas.addEventListener('DOMMouseScroll', mouseWheel, false);
        canvas.addEventListener('mousewheel', mouseWheel, false);
    }
    else {
        canvas.onmousewheel = mouseWheel;
    }
}
//all the tags in the scene
var tags = [];

function DynamicLoad(modelid) {
    ModelID = modelid;  
    if (SceneJS.scene("Scene").findNode("materialNode") == null) {
        var sce = SceneJS.scene("Scene");
        var node = SceneJS.scene("Scene").findNode("offset");

        $("#types").show();
        $("#modelmenu").hide();

        StartLoadingDynamicModel(sce, node, ModelID);
    }
}
function filterchange() {
    var checked = $(":checked");
    var tags = "(";
    if (checked.length > 0) {
        tags += checked[0].value;
        for (var i = 1; i < checked.length; i++) {
            tags += "|" + checked[i].value;
        }
    } else {
        tags += "none";
    }
    tags += ")";

    SceneJS.scene("Scene").set("tagMask", tags);
}
