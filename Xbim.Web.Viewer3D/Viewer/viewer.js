/*
* viewer.js
* 
* Copyright 2009, Google Inc.
* All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are
* met:
*
*     * Redistributions of source code must retain the above copyright
* notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above
* copyright notice, this list of conditions and the following disclaimer
* in the documentation and/or other materials provided with the
* distribution.
*     * Neither the name of Google Inc. nor the names of its
* contributors may be used to endorse or promote products derived from
* this software without specific prior written permission.
*
* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
* "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
* LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
* A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
* OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
* SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
* LIMITED TO,§ PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
* DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
* THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
* (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
* OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/


/**
 * This file contains definitions for the common functions used by all the home
 * configurator pages.
 */
o3djs.base.o3d = o3d;
o3djs.require('o3djs.webgl');
o3djs.require('o3djs.util');
o3djs.require('o3djs.arcball');
o3djs.require('o3djs.dump');
o3djs.require('o3djs.rendergraph');
o3djs.require('o3djs.shape');
o3djs.require('o3djs.effect');
o3djs.require('o3djs.material');
o3djs.require('o3djs.pack');
o3djs.require('o3djs.picking');
o3djs.require('o3djs.scene');
o3djs.require('o3djs.primitives');

var g_root;
var g_o3d;
var g_math;
var g_client;
var g_pack = null;
var g_mainPack;
var g_viewInfo;
var g_pickManager;
var g_lightPosParam;
var g_currentTool = null;
var g_mainmeshRoot = null;
var g_placedModesRoot = null;

// An array of tool objects that will get populated when our base model loads.
var g_tools = [];

var TOOL_PICKER = 0;
var TOOL_ORBIT = 1;
var TOOL_PAN = 2;
var TOOL_ZOOM = 3;

var g_urlToInsert;
var g_o3dElement = null;
var g_meshFile;

function selectInViewer(nodelabel) {
    var selected_ids = [];
    var textNodeLabel = "" + nodelabel;
    selected_ids.push(textNodeLabel);
    Picker().SelectInViewer(selected_ids);
}

/**
 * Retrieve the absolute position of an element on the screen.
 */
function getAbsolutePosition(element) {
  var r = { x: element.offset().left, y: element.offset().top };
  return r;
}

/**
 * Retrieve the coordinates of the given event relative to the center
 * of the widget.
 *
 * @param event
 *  A mouse-related DOM event.
 * @param reference
 *  A DOM element whose position we want to transform the mouse coordinates to.
 * @return
 *    An object containing keys 'x' and 'y'.
 */
function getRelativeCoordinates(event, reference) {
 
    // Use absolute coordinates
    var pos = getAbsolutePosition(reference);
    x = event.pageX - pos.x;
    y = event.pageY - pos.y;

  return { x: x, y: y };
}


// The target camera has its z and y flipped because that's the way Scott
// Lininger thinks.
function TargetCamera() {
  this.eye = {
      rotZ: -Math.PI / 3,
      rotH: Math.PI / 3,
      distanceFromTarget: 30 };
  this.target = { x: 0, y: 0, z: 0 };
  this.nearPlane = 0.001;
  this.farPlane = 50000;
}

TargetCamera.prototype.update = function() {
  var target = [this.target.x, this.target.y, this.target.z];

  this.eye.x = this.target.x + Math.cos(this.eye.rotZ) *
      this.eye.distanceFromTarget * Math.sin(this.eye.rotH);
  this.eye.y = this.target.y + Math.sin(this.eye.rotZ) *
      this.eye.distanceFromTarget * Math.sin(this.eye.rotH);
  this.eye.z = this.target.z + Math.cos(this.eye.rotH) *
      this.eye.distanceFromTarget;

  var eye = [this.eye.x, this.eye.y, this.eye.z];
  var up = [0, 0, 1];
  g_viewInfo.drawContext.view = g_math.matrix4.lookAt(eye, target, up);
  g_lightPosParam.value = eye;
  updateClient();

};

var g_camera = new TargetCamera();

function peg(value, lower, upper) {
  if (value < lower) {
    return lower;
  } else if (value > upper) {
    return upper;
  } else {
    return value;
  }
}

///**
// * Keyboard constants.
// */
var BACKSPACE = 8;
var TAB = 9;
var ENTER = 13;
var SHIFT = 16;
var CTRL = 17;
var ALT = 18;
var ESCAPE = 27;
var PAGEUP = 33;
var PAGEDOWN = 34;
var END = 35;
var HOME = 36;
var LEFT = 37;
var UP = 38;
var RIGHT = 39;
var DOWN = 40;
var DELETE = 46;
var SPACE = 32;

///**
// * Create some global key capturing. Keys that are pressed will be stored in
// * this global array.
// */
g_keyIsDown = [];

document.onkeydown = function(e) {
  var keycode;
  if (window.event) {
    keycode = window.event.keyCode;
  } else if (e) {
    keycode = e.which;
  }
  g_keyIsDown[keycode] = true;
  if (g_currentTool != null) {
    g_currentTool.handleKeyDown(keycode);
  }
};

document.onkeyup = function(e) {
  var keycode;
  if (window.event) {
    keycode = window.event.keyCode;
  } else if (e) {
    keycode = e.which;
  }
  g_keyIsDown[keycode] = false;
  if (g_currentTool != null) {
    g_currentTool.handleKeyUp(keycode);
  }
};

//document.onmouseup = function(e) {
//  if (g_currentTool != null) {
//    g_currentTool.handleMouseUp(e);
//  } else {
//    cancelInsertDrag();
//  }
//};

// NOTE: mouseDown, mouseMove and mouseUp are mouse event handlers for events
// taking place inside the o3d area.  They typically pass the events down
// to the currently selected tool (e.g. Orbit, Move, etc).  Tool and item
// selection mouse events are registered seperately on their respective DOM
// elements.

// This function handles the mousedown events that happen inside the o3d
// area.  If a tool is currently selected (e.g. Orbit, Move, etc.) the event
// is forwarded over to it.  If the middle mouse button is pressed then we
// temporarily switch over to the orbit tool to emulate the SketchUp behavior.
function mouseDown(e) {
  // If the middle mouse button is used, then switch into the orbit tool,
  // Sketchup-style.
  if (e.button == g_o3d.Event.BUTTON_MIDDLE) {
    g_lastTool = g_currentTool;
    SetTool(TOOL_ORBIT);
  }
  if (g_currentTool == null) {
      SetTool(TOOL_ORBIT);
  }
  if (g_currentTool != null) {
    g_currentTool.handleMouseDown(e);
  }
}

// This function handles mouse move events inside the o3d area.  It simply
// forwards them down to the currently selected tool.
function mouseMove(e) {
  if (g_currentTool != null) {
    g_currentTool.handleMouseMove(e);
  }
}

// This function handles mouse up events that take place in the o3d area.
// If the middle mouse button is lifted then we switch out of the temporary
// orbit tool mode.
function mouseUp(e) {
  // If the middle mouse button was used, then switch out of the orbit tool
  // and reset to their last tool.
  if (e.button == g_o3d.Event.BUTTON_MIDDLE) {
      //highlightTool(g_lastTool);
      g_currentTool = g_lastTool;
  }
  if (g_currentTool != null) {
    g_currentTool.handleMouseUp(e);
  }
}

// This function handles mouse scroll wheel events, to zoom in and out of the 
// view
function scrollMe(e) {
  e = e ? e : window.event;
  var raw = e.detail ? e.detail : -e.wheelDelta;
  if (raw < 0) {
    g_camera.eye.distanceFromTarget *= 11 / 12;

  } else {
    g_camera.eye.distanceFromTarget *= (1 + 1 / 12);
  }
  g_camera.update();
}

function zoom(amnt) {
    g_camera.eye.distanceFromTarget *= amnt;
    g_camera.update();
}


function SetTool(toolNumber) {
    g_currentTool = g_tools[toolNumber];
    g_currentTool.reset();
}


function ZoomExtents() {
    zoomContent(o3djs.util.getBoundingBoxOfTree(g_client.root));
    g_camera.update();
}

function DataTest() {
    // DoServerStuff2('/View3D/DataBinary');
    // alert(selected_ids);
    var bindataid = lastPickedNode.replace("#Node", "")
    // alert(bindataid);
    var barefilename = g_meshFile.replace("/View3D/Mesh?fileName=", "");
    var pageToLoad = '/View3D/BinaryMesh?filename=' + barefilename + '&CommaSepIds=' + bindataid;
    // alert(pageToLoad);
    ParseBinaryModel(pageToLoad);
}


function loadFile(context, path) {
    function callback(pack, start_move_tool_root, exception) {
        if (exception) {
            alert('Could not load: ' + path + '\n' + exception);
        } else {
            // Generate draw elements and setup material draw lists.
            o3djs.pack.preparePack(g_pack, g_viewInfo);

            // Manually connect all the materials' lightWorldPos params to the context
            var materials = g_pack.getObjectsByClassName('o3d.Material');
            for (var m = 0; m < materials.length; ++m) {
                var material = materials[m];
                var param = material.getParam('lightWorldPos');
                if (param) {
                    param.bind(g_lightPosParam);
                }
            }

            //debugDump();
        }

        g_camera.update();
    } // end callback

    g_pack = g_client.createPack();
    g_pack.name = 'load pack';

    new_object_root = null;
    if (g_mainmeshRoot == null) {
        // Assign as the floorplan
        g_mainmeshRoot = g_pack.createObject('o3d.Transform');
        g_mainmeshRoot.name = 'mainmesh';
        g_mainmeshRoot.parent = g_client.root;

        // Put the object we're loading on the floorplan.
        new_object_root = g_mainmeshRoot;

        g_pickManager = o3djs.picking.createPickManager(g_mainmeshRoot);

        // Create our set of tools that can be activated.
        // Note: Other tools exist for handlind 2ndary placed model, but are not used
        g_tools = [
          new PickTool(g_viewInfo.drawContext, g_mainmeshRoot),
          new OrbitTool(g_camera),
          new PanTool(g_camera),
          new ZoomTool(g_camera)
        ]

        // load main model
        if (path != null) {
            loadMesh(g_pack, path, new_object_root, callback);
        }

        SetTool(TOOL_ORBIT);
    } else {
        // Create a new transform for the loaded file
        new_object_root = g_pack.createObject('o3d.Transform');
        new_object_root.name = 'loaded object';
        new_object_root.parent = g_placedModelsRoot;

        if (path != null) {
            o3djs.scene.loadScene(g_client, g_pack, new_object_root, path, callback);
        }
    }


    return new_object_root;
}

function setClientSize() {
    // Create a perspective projection matrix
    if (g_viewInfo)
        g_viewInfo.drawContext.projection = g_math.matrix4.perspective(
            3.14 * 45 / 180, g_client.width / g_client.height, g_camera.nearPlane, g_camera.farPlane);

    updateClient();
}

function resizeClient() {
    setClientSize();
}

function updateClient() {
    // If we are in RENDERMODE_ON_DEMAND mode then set the render mode again
    // which will cause the client re-render the display.
    if (g_client.renderMode == g_o3d.Client.RENDERMODE_ON_DEMAND) {
        g_client.render();
    }
}

function init(meshFile) {
    g_meshFile = meshFile;
    o3djs.webgl.makeClients(initStep2);
}

function initStep2(clientElements) {
  g_o3dElement = clientElements[0];


  g_o3d = g_o3dElement.o3d;
  g_math = o3djs.math;
  g_client = g_o3dElement.client;

  g_client.renderMode = o3d.Client.RENDERMODE_ON_DEMAND;

  g_mainPack = g_client.createPack();
  g_mainPack.name = 'simple viewer pack';

  // Create the render graph for a view.
  g_viewInfo = o3djs.rendergraph.createBasicView(
      g_mainPack,
      g_client.root,
      g_client.renderGraphRoot,
      [1, 1, 1, 1]);


  var root = g_client.root;

  var target = [0, 0, 0];
  var eye = [0, 0, 5];
  var up = [0, 1, 0];
  g_viewInfo.drawContext.view = g_math.matrix4.lookAt(eye, target, up);

  setClientSize();

  var paramObject = g_mainPack.createObject('ParamObject');
  // Set the light at the same position as the camera to create a headlight
  // that illuminates the object straight on.
  g_lightPosParam = paramObject.createParam('lightWorldPos', 'ParamFloat3');
  g_lightPosParam.value = eye;

  loadModel(g_meshFile);

  o3djs.event.addEventListener(g_o3dElement, 'mousedown', mouseDown);
  o3djs.event.addEventListener(g_o3dElement, 'mousemove', mouseMove);
  o3djs.event.addEventListener(g_o3dElement, 'mouseup', mouseUp);
  g_o3dElement.addEventListener('mouseover', dragOver, false);
  // for Firefox
  g_o3dElement.addEventListener('DOMMouseScroll', scrollMe, false);
  // for Safari
  g_o3dElement.onmousewheel = scrollMe;

  // Register a mouse-move event listener to the entire window so that we can
  // catch the click-and-drag events that originate from the list of items
  // and end up in the o3d element.
  document.addEventListener('mousemove', mouseMove, false);
}

/**
* Removes any callbacks so they don't get called after the page has unloaded.
*/
function uninit() {
    if (g_client) {
        g_client.cleanup();
    }
}

function dragOver(e) {
  if (g_urlToInsert != null) {
    loadModel(g_urlToInsert);
  }
  g_urlToInsert = null;
}

function loadModel(opt_url) {
  var url;
  if (opt_url != null) {
    url = opt_url
  } else if ($('url')) {
    url = $('url').value;
  }
  g_root = loadFile(g_viewInfo.drawContext, url);
}

function startInsertDrag(url) {
  // If no absolute web path was passed, assume it's a local file
  // coming from the assets directory.
  if (url.indexOf('http') != 0) {
    var path = window.location.href;
    var index = path.lastIndexOf('/');
    g_urlToInsert = path.substring(0, index + 1) + g_assetPath + url;
  } else {
    g_urlToInsert = url;
  }
}

function cancelInsertDrag() {
  g_urlToInsert = null;
}

/// Xbim /////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////

var selLastShape;
var selLastMaterial;
var selSelectedMaterial;
var selTransparentMaterial;
var defaultMaterial;
var offWhiteMaterial;
var darkBlueSlateMaterial;
var lightGreyMaterial;
var windowMaterial;
var darkRedMaterial;
var darkGreyMaterial;
var darkBlueMaterial;
var greenMaterial;

var xmlhttp;
var nodes = 0;

var g_renderMode;

RenderModes = {
    SOLID: 0,
    XRAY: 1,
    WIRE: 2,
    POINT: 3
};

function SetRenderMode(renderMode) {
    g_renderMode = renderMode;
    if (renderMode == RenderModes.WIRE) {
        g_viewInfo.performanceState.getStateParam('FillMode').value = g_o3d.State.WIREFRAME;
        g_viewInfo.zOrderedState.getStateParam('FillMode').value = g_o3d.State.SOLID;

    } else if (renderMode == RenderModes.POINT) {
        g_viewInfo.performanceState.getStateParam('FillMode').value = g_o3d.State.POINT;
        g_viewInfo.zOrderedState.getStateParam('FillMode').value = g_o3d.State.POINT;
    } else {
        g_viewInfo.performanceState.getStateParam('FillMode').value = g_o3d.State.SOLID;
        g_viewInfo.zOrderedState.getStateParam('FillMode').value = g_o3d.State.SOLID;
    }

    setAllDefaultMaterial();
    updateClient();
}

function setStatus(message) {
    $(g_o3dElement).trigger("statusUpdated", [message]);
}

function prepareMaterials(pack) {
	selSelectedMaterial = CreateMaterial([1, 0.54, 0, 1], pack, g_viewInfo);
	selTransparentMaterial = CreateMaterial([0.9, 0.9, 0.9, 0.2], pack, g_viewInfo); 
	defaultMaterial = CreateMaterial([0.2, 0.4, 0.9, 1], pack, g_viewInfo); 

	offWhiteMaterial = CreateMaterial([0.98, 0.92, 0.74, 1], pack, g_viewInfo);
	darkBlueSlateMaterial = CreateMaterial([0.28, 0.24, 0.55, 1], pack, g_viewInfo);
	lightGreyMaterial = CreateMaterial([0.47, 0.53, 0.60, 1], pack, g_viewInfo);
	darkGreyMaterial = CreateMaterial([0.34, 0.34, 0.34, 1], pack, g_viewInfo);
	darkRedMaterial = CreateMaterial([0.97, 0.19, 0, 1], pack, g_viewInfo);
	darkBlueMaterial = CreateMaterial([0.0, 0.0, 0.55, 1], pack, g_viewInfo);
	greenMaterial = CreateMaterial([0.0, 0.9, 0.1, 1], pack, g_viewInfo);
	windowMaterial = CreateMaterial([0.68, 0.85, 0.90, 0.4], pack, g_viewInfo);
	windowMaterial.name = "window";

	defaultMaterial.name = "default"; 
}

function loadMesh(pack, path, transformRoot, callback) {
    var exception = null;
    try {
        prepareMaterials(pack);
        getXML(path);
    }
    catch (ex) {
        exception = ex;
    }
    callback(pack, transformRoot, exception);
}

function CreateMaterial(rgba, pack, viewInfo) {
    var isTransparent = (rgba[3] < 1);
    // Ambient colour - simplist halfing of colour... So unlit faces aren't completely black.
    var ambient = [(rgba[0] / 2), (rgba[1] / 2), (rgba[2] / 2), (rgba[3])];

    var material = pack.createObject('Material');
    material.drawList = isTransparent ? viewInfo.zOrderedDrawList :
                                        viewInfo.performanceDrawList;

    material.createParam('diffuse', 'ParamFloat4').value = rgba;

    // Assume Transparent materials are shiney/specular. Everything else isn't
    if (isTransparent) {
        material.createParam('emissive', 'ParamFloat4').value = [0, 0, 0, 1];
        material.createParam('ambient', 'ParamFloat4').value = ambient; 
        material.createParam('specular', 'ParamFloat4').value = [1, 1, 1, 1];
        material.createParam('shininess', 'ParamFloat').value = 100;
        material.createParam('specularFactor', 'ParamFloat').value = 1;
        material.createParam('lightColor', 'ParamFloat4').value = [1, 1, 1, 1];
    } else {
        material.createParam('emissive', 'ParamFloat4').value = [0, 0, 0, 1];
        material.createParam('ambient', 'ParamFloat4').value = ambient;
        material.createParam('specular', 'ParamFloat4').value = [0.1, 0.1, 0.1, 0.1];
        material.createParam('shininess', 'ParamFloat').value = 0;
        material.createParam('specularFactor', 'ParamFloat').value = 0.1;
        material.createParam('lightColor', 'ParamFloat4').value = [0.5, 0.5, 0.5, 1];
    }

    var lightPositionParam = material.createParam('lightWorldPos',
                                                'ParamFloat3');

    o3djs.material.attachStandardEffect(pack, material, viewInfo, 'phong');

    // We have to set the light position after calling attachStandardEffect
    // because attachStandardEffect sets it based on the view.
    lightPositionParam.value = [1000, 2000, 3000];

    return material;

}

function AddXMLMesh(node, ptransform, pack, material, parentMaterial) {

    var vertexInfo = o3djs.primitives.createVertexInfo();
    var positionStream = vertexInfo.addStream(3, o3djs.base.o3d.Stream.POSITION);
    var normalStream = vertexInfo.addStream(3, o3djs.base.o3d.Stream.NORMAL);
    var isEmptyShape = true;
    var child = node.firstChild;
    var thiswbb;
    var materialName;

    try {
        materialName = node.getAttribute('Material');
        if (!materialName) {
            materialName = parentMaterial;
        }
        material = GetMaterial(materialName, material);
        ptransform.materialName = materialName;
    }
    catch (e)
    { }

    setStatus(ptransform.materialName);
    while (child) {
        switch (child.nodeName) {
            case "T": // it's a transform matrix

                var vId = child.getAttribute('value');
                if (vId == 'Identity')
                    ptransform.identity();
                else {
                    // ptransform.identity();
                    var v11 = parseFloat(child.getAttribute('M11'));
                    var v12 = parseFloat(child.getAttribute('M12'));
                    var v13 = parseFloat(child.getAttribute('M13'));
                    var v14 = parseFloat(child.getAttribute('M14'));

                    var v21 = parseFloat(child.getAttribute('M21'));
                    var v22 = parseFloat(child.getAttribute('M22'));
                    var v23 = parseFloat(child.getAttribute('M23'));
                    var v24 = parseFloat(child.getAttribute('M24'));

                    var v31 = parseFloat(child.getAttribute('M31'));
                    var v32 = parseFloat(child.getAttribute('M32'));
                    var v33 = parseFloat(child.getAttribute('M33'));
                    var v34 = parseFloat(child.getAttribute('M34'));

                    var v41 = parseFloat(child.getAttribute('M41'));
                    var v42 = parseFloat(child.getAttribute('M42'));
                    var v43 = parseFloat(child.getAttribute('M43'));
                    var v44 = parseFloat(child.getAttribute('M44'));

                    var transform = [[v11, v12, v13, v14], [v21, v22, v23, v34], [v31, v32, v33, v34], [v41, v42, v43, v44]];
                    ptransform.localMatrix = transform;
                }
                break;
            case "PN":
                var px = parseFloat(child.getAttribute('PX'));
                var py = parseFloat(child.getAttribute('PY'));
                var pz = parseFloat(child.getAttribute('PZ'));

                var nx = parseFloat(child.getAttribute('NX'));
                var ny = parseFloat(child.getAttribute('NY'));
                var nz = parseFloat(child.getAttribute('NZ'));

                positionStream.addElement(px, py, pz);
                normalStream.addElement(nx, ny, nz);
                isEmptyShape = false;
                break;
            case "F": // it's a face index list
                var i1 = parseInt(child.getAttribute('I1'));
                var i2 = parseInt(child.getAttribute('I2'));
                var i3 = parseInt(child.getAttribute('I3'));

                vertexInfo.addTriangle(i1, i2, i3);
                isEmptyShape = false;
                break;
            case "Mesh":
                AddXMLMesh(child, ptransform, pack, material, materialName);
                break;
            case "WBB":
                var MnX = parseFloat(child.getAttribute('MnX'));
                var MnY = parseFloat(child.getAttribute('MnY'));
                var MnZ = parseFloat(child.getAttribute('MnZ'));

                var MxX = parseFloat(child.getAttribute('MxX'));
                var MxY = parseFloat(child.getAttribute('MxY'));
                var MxZ = parseFloat(child.getAttribute('MxZ'));

                thiswbb = new o3d.BoundingBox(
                    [MnX, MnY, MnZ],
                    [MxX, MxY, MxZ]
                );

                break;
        }
        child = child.nextSibling;
    }

    if (!isEmptyShape) {
        nodes++;
        setStatus("Created " + nodes + " items");
        var theShape = vertexInfo.createShape(pack, material);
        ptransform.addShape(theShape);
        //theShape.cull = true;
    }
    var sName = node.getAttribute('EntityLabel');
    if (sName != '')
        ptransform.name = sName;

    return thiswbb;
}

function GetMaterial(materialName, defaultMat) {
    if (g_renderMode == RenderModes.XRAY) {
        return selTransparentMaterial;
        }

    if (materialName == "offwhite") {
        return (offWhiteMaterial);
    }
    if (materialName == "window") {
        return (windowMaterial);
    }
    if (materialName == "darkslateblue") {
        return (darkBlueSlateMaterial);
    }
    if (materialName == "darkblue") {
        return (darkBlueMaterial);
    }
    if (materialName == "lightgrey") {
        return (lightGreyMaterial);
    }
    if (materialName == "darkred") {
        return (darkRedMaterial);
    }
    if (materialName == "green") {
        return (greenMaterial);
    }

    return defaultMat;
}

function setAllDefaultMaterial() {
    var values = g_mainmeshRoot.getTransformsInTree();
    for (var iter = 0; iter < values.length; iter++) {
        var thisv = values[iter];

        var material = GetMaterial(thisv.materialName, defaultMaterial);

        setMaterial(thisv, material);
    }
    updateClient();
}

function setMaterial(transform, setMaterial) {
    for (var iShp = 0; iShp < transform.shapes.length; iShp++) {
        transform.shapes[iShp].elements[0].material = setMaterial;
    }
    return;
};

function setDataXML() {
    if (xmlhttp.readyState == 1)
        return;
    if (xmlhttp.status == 500) {
        setStatus("500 Error generating model");
        return;
    }
    if (xmlhttp.readyState != 4 || (xmlhttp.status != 200 && xmlhttp.status != 304)) {
        setStatus("Downloading...");
        return;
    }

//    try {

        var currNode;
        currNode = xmlhttp.responseXML.documentElement;
        if (currNode && currNode.hasChildNodes() && currNode.nodeName == 'XBIMGeometry') {
            currNode = currNode.firstChild;
        }

        setStatus('Loading...');
        var bboxes;
        while (currNode) {
            // now in the mesh list
            if (currNode.nodeName == 'Mesh') {
                // try to get the transform
                //
                var thistransform = g_mainPack.createObject('Transform');
                thistransform.parent = g_mainmeshRoot; //  g_roottransform; // this puts it under the elements that you can navigate around
                var bbox = AddXMLMesh(currNode, thistransform, g_mainPack, defaultMaterial, "default");
                if (bbox) {
                    if (bboxes)
                        bboxes.add(bbox);
                    else
                        bboxes = bbox;
                }
            }
            currNode = currNode.nextSibling;
        }

        g_renderMode = RenderModes.SOLID

        //o3djs.pack.preparePack(g_pack, g_viewInfo);
        ZoomExtents();

        setStatus('Loading complete.');
        $(g_o3dElement).trigger("modelLoaded");

//    }
//    catch (ex) {
//        $(g_o3dElement).trigger("loadingError", "Exception downloading model : " + ex);
//        throw (ex);
//    }

}


function zoomToElement(id) {
    if (typeof id == "number")
        id = '' + id;
    var values = g_mainmeshRoot.getTransformsInTree();
    for (var iter = 0; iter < values.length; iter++) {
        var thisv = values[iter];

        if (id === thisv.name) {
            zoomContent(o3djs.util.getBoundingBoxOfTree(thisv));
            break;
        }
    }
}


function zoomContent(inBox) {

    var bbox = null;
    if (inBox) {
        //updateInfo();
        bbox = inBox;
    }
    else {
        //updateInfo();
        bbox = o3djs.util.getBoundingBoxOfTree(g_client.root);
    }

    var centerPoint = bbCenter(bbox);
    g_camera.target = { x: centerPoint[0], y: centerPoint[1], z: centerPoint[2] };
    var diag = g_math.length(g_math.subVector(bbox.maxExtent, bbox.minExtent));
    var viewpoint = g_math.addVector(g_camera.target, [diag, diag, 0.5 * diag]);
    g_camera.eye.x = viewpoint[0];
    g_camera.eye.y = viewpoint[1];
    g_camera.eye.z = viewpoint[2];
    g_camera.eye.distanceFromTarget = diag * 2;

    g_camera.nearPlane = diag / 1000;
    g_camera.farPlane = diag * 100;

    setClientSize();
    g_camera.update();
    //updateProjection();
}

function bbCenter(bbox) {
    return g_math.lerpVector(bbox.minExtent, bbox.maxExtent, 0.5);
}

function getXML(url) {
    // AJAX code for Mozilla, Safari, Opera etc.
		alert(url);
    setStatus("Loading " + url);
    if (window.XMLHttpRequest) {
        xmlhttp = new XMLHttpRequest();
        xmlhttp.onreadystatechange = setDataXML;
        xmlhttp.addEventListener("progress", updateProgress, false);
        xmlhttp.open("GET", url, true);
        xmlhttp.send(null);
    }
}

function updateProgress(evt) {  
  if (evt.lengthComputable) {  
    var percentComplete = evt.loaded / evt.total * 100;
    setStatus("Downloading data. Percentage complete: " + percentComplete + "%");
  } 
}  


function getSyncronousHTTP(url) {
    if (!window.XMLHttpRequest)
        return null;

    xmlhttp = new XMLHttpRequest();
    xmlhttp.open("GET", url, false);
    xmlhttp.send(null);
    return xmlhttp;
}

function AjaxDo(url, destination) {
    var data = getSyncronousHTTP(url);
    var DestElement = document.getElementById(destination);
    if (DestElement) {
        DestElement.innerHTML = data.responseText;
    }
}

function getSyncronousXML(url, sNodeName) {
    var ret = getSyncronousHTTP(url);
    if (ret) {
        var currNode;
        currNode = ret.responseXML.documentElement;
        if (currNode && currNode.nodeName == sNodeName) {
            return currNode.textContent;
        }
    }
    return '';
}

function debugDump() {
    /*
    o3djs.dump.dump('---dumping context---\n');
    o3djs.dump.dumpParamObject(context);

    o3djs.dump.dump('---dumping root---\n');
    o3djs.dump.dumpTransformTree(g_client.root);

    o3djs.dump.dump('---dumping render root---\n');
    o3djs.dump.dumpRenderNodeTree(g_client.renderGraphRoot);

    o3djs.dump.dump('---dump g_pack shapes---\n');
    var shapes = g_pack.getObjectsByClassName('o3d.Shape');
    for (var t = 0; t < shapes.length; t++) {
    o3djs.dump.dumpShape(shapes[t]);
    }

    o3djs.dump.dump('---dump g_pack materials---\n');
    var materials = g_pack.getObjectsByClassName('o3d.Material');
    for (var t = 0; t < materials.length; t++) {
    o3djs.dump.dump (
    '  ' + t + ' : ' + materials[t].className +
    ' : "' + materials[t].name + '"\n');
    o3djs.dump.dumpParams(materials[t], '    ');
    }

    o3djs.dump.dump('---dump g_pack textures---\n');
    var textures = g_pack.getObjectsByClassName('o3d.Texture');
    for (var t = 0; t < textures.length; t++) {
    o3djs.dump.dumpTexture(textures[t]);
    }

    o3djs.dump.dump('---dump g_pack effects---\n');
    var effects = g_pack.getObjectsByClassName('o3d.Effect');
    for (var t = 0; t < effects.length; t++) {
    o3djs.dump.dump ('  ' + t + ' : ' + effects[t].className +
    ' : "' + effects[t].name + '"\n');
    o3djs.dump.dumpParams(effects[t], '    ');
    }
    */
}

function Picker() {
    return (g_tools[TOOL_PICKER]);
}

function HideElement(selStringsArray) {

    var values = g_mainmeshRoot.getTransformsInTree();
    for (var iter = 0; iter < values.length; iter++) {
        var thisv = values[iter];

        if (selStringsArray.indexOf(thisv.name) != -1) {
            thisv.visible = false;
        }

    }
    updateClient();
}

function ShowElement(selStringsArray) {
    var values = g_mainmeshRoot.getTransformsInTree();
    for (var iter = 0; iter < values.length; iter++) {
        var thisv = values[iter];

        if (selStringsArray.indexOf(thisv.name) != -1) {
            thisv.visible = true;
        }
    }
    updateClient();

}