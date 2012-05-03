XbimClientViewer3DControl = function (elementid, serviceurl) {
    this.transformGraph = new XbimTransformGraph();
    this._ServiceUrl = serviceurl;
    this.AsyncActivityQueue = [];
}

XbimClientViewer3DControl.prototype = {
    setSelectionMode: function (mode) {
        g_tools[TOOL_PICKER].setSelectionMode(mode);
        return false;
    },
    setNodeVisibility: function (filter, visibility) {
        function setVis(node) {
            node.Wtransform.visible = true;
        }
        function setHid(node) {
            node.Wtransform.visible = false;
        }
        if (visibility)
            this.transformGraph.Root.onMatchingNodes(filter, setVis);
        else
            this.transformGraph.Root.onMatchingNodes(filter, setHid);
        updateClient();
    },
    setNodeOverrideMaterial: function (filter, material) {
        function setOMat(node, material) {
            node.materialOverride = material;
        }
        this.transformGraph.Root.onMatchingNodes(filter, setOMat, material);    
        this.setVisualPropertiesAll();
    },
    addAsyncQueue: function (f) {
        this.AsyncActivityQueue.push(f);
    },
    progressAsyncQueue: function () {
        if (this.AsyncActivityQueue.length > 0) {
            var Qi = this.AsyncActivityQueue[0];
            if (typeof Qi == "string") {
                if (Qi == "downloadTG") {
                    this.downloadTG();
                }
                else if (Qi == "downloadWholeModel") {
                    this.downloadWholeModel();
                }
                this.AsyncActivityQueue.shift();
            }
            else {
                this.AsyncActivityQueue[0]();
                this.AsyncActivityQueue.shift();
            }
        }
    },
    downloadWholeModel: function () {
        var elements = this.transformGraph.Root.getNodeLabelsByType(new Array('IfcOpeningElement', 'IfcSpace'), false);
        this.loadLabelMesh(elements);
        $("#o3d").triggerHandler("modelLoaded");
    },
    setVisualPropertiesAll: function () {
        var values = this.transformGraph.getAllNodes();
        for (var iter = 0; iter < values.length; iter++) {
            var thisv = values[iter];
            if (thisv.materialOverride != null)
                setMaterial(thisv.Wtransform, thisv.materialOverride);
            else if (g_tools[TOOL_PICKER].currentSelection != null && g_tools[TOOL_PICKER].currentSelection.indexOf(thisv.EntityLabel) != -1)
                setMaterial(thisv.Wtransform, selSelectedMaterial);
            else {
                var material = this.getMaterial(thisv.Type);
                setMaterial(thisv.Wtransform, material);
            }
        }
        updateClient();
    },
    select: function (selStringsArray) {
        if (!(selStringsArray instanceof Array)) {
            var v = [];
            v.push(selStringsArray);
            selStringsArray = v;
        }
        for (var i = 0; i < selStringsArray.length; i++) {
            if (typeof selStringsArray[i] != "string")
                selStringsArray[i] = '' + selStringsArray[i];
        }
        g_tools[TOOL_PICKER].currentSelection = selStringsArray;
        this.setVisualPropertiesAll();
        return;
    },
    reportWalls: function () {
        var tps = new Array('IfcWall', 'IfcWallStandardCase');
        var arr = this.transformGraph.Root.getNodeLabelsByType(tps);
        return arr;
    },
    isAlive: function () {
        return 'yes, I ClientViewer3DControl is alive, url service is: ' + this._ServiceUrl;
    },
    downloadTG: function () {
        this.transformGraph = new XbimTransformGraph();
        this.getXML(this.UrlTG(), this.ParseXMLTG);
    },
    UrlTG: function () {
        return this._ServiceUrl + "?Data=TG";
    },
    UrlXmlMesh: function () {
        return this._ServiceUrl + "?Data=XMLMESH";
    },
    UrlBinaryMesh: function (entitylabels, post) {
        if (post == null)
            post = true;
        if (post)
            return this._ServiceUrl + "?Data=MSH";
        return
        this._ServiceUrl + "?Data=MSH&EL=" + entitylabels;
    },
    getXML: function (url, onComplete) {
        // AJAX code for Mozilla, Safari, Opera etc.
        // setStatus("Loading " + url);
        if (window.XMLHttpRequest) {
            xmlhttp = new XMLHttpRequest();
            xmlhttp.VVR = this;
            xmlhttp.onreadystatechange = onComplete;
            // xmlhttp.addEventListener("progress", updateProgress, false);
            xmlhttp.open("GET", url, true);
            xmlhttp.send(null);
        }
    },
    ParseXMLTG: function () {
        if (xmlhttp.readyState == 1)
            return;
        if (xmlhttp.status == 500) {
            return;
        }
        if (xmlhttp.readyState != 4 || (xmlhttp.status != 200 && xmlhttp.status != 304)) {
            return;
        }
        //        try {
        var currNode;
        currNode = xmlhttp.responseXML.documentElement;
        if (currNode && currNode.hasChildNodes() && currNode.nodeName == 'XBIMTransformGraph') {
            currNode = currNode.firstChild; // scene
            currNode = currNode.firstChild; // should be the first node of the tree
        }
        while (currNode) {
            // now in the mesh list
            if (currNode.nodeName == 'Node') {
                this.VVR.transformGraph.Root = this.VVR.ParseNodeRecursive(currNode, this.VVR)
            }
            currNode = currNode.nextSibling;
        }
        // o3djs.pack.preparePack(g_pack, g_viewInfo);
        // ZoomExtents();
        this.VVR.progressAsyncQueue();
    },
    ParseNodeRecursive: function (node, viewer) {
        var thisNode = new XbimTransformNode();
        thisNode.EntityLabel = node.getAttribute('label');
        try {
            thisNode.Type = node.getAttribute('t');
        }
        catch (e)
        { }

        var child = node.firstChild;
        while (child) {
            switch (child.nodeName) {
                case "Node": // it's a transform matrix
                    thisNode.Nodes.push(viewer.ParseNodeRecursive(child, viewer));
                    break;
                case "WBB": // it's a bounding box
                    break;
                case "WT": // it's a world transform matrix
                    thisNode.Wtransform = g_mainPack.createObject('Transform');
                    thisNode.Wtransform.name = thisNode.EntityLabel;
                    thisNode.Wtransform.parent = g_mainmeshRoot;
                    var vId = child.getAttribute('value');
                    if (vId == 'Identity')
                        thisNode.Wtransform.identity();
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
                        thisNode.Wtransform.localMatrix = transform;
                    }
                    break;
            }
            child = child.nextSibling;
        }
        return thisNode;
    },
    tgStruct: function (divname, pattern) {
        document.getElementById(divname).innerHTML = this.transformGraph.Root.NodesToList(pattern);
    },
    initO3D: function () {
        o3djs.webgl.makeClients(this.initO3DStep2);
    },
    initO3DStep2: function (clientElements) {
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
              [1, 1, 1, 1]
              );

        var root = g_client.root;

        var target = [0, 0, 0];
        var eye = [0, 0, 5];
        var up = [0, 1, 0];
        g_viewInfo.drawContext.view = g_math.matrix4.lookAt(eye, target, up);
        g_viewInfo.performanceState.getStateParam('CullMode').value = g_o3d.State.CULL_NONE;
        g_viewInfo.zOrderedState.getStateParam('CullMode').value = g_o3d.State.CULL_NONE;

        setClientSize();

        var paramObject = g_mainPack.createObject('ParamObject');
        // Set the light at the same position as the camera to create a headlight
        // that illuminates the object straight on.
        g_lightPosParam = paramObject.createParam('lightWorldPos', 'ParamFloat3');
        g_lightPosParam.value = eye;
        prepareMaterials(g_mainPack);
        // loadModel(g_meshFile);

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

        g_mainmeshRoot = g_mainPack.createObject('o3d.Transform');
        g_mainmeshRoot.name = 'mainmesh';
        g_mainmeshRoot.parent = g_client.root;

        g_pickManager = o3djs.picking.createPickManager(g_mainmeshRoot);
        g_tools = [
          new PickTool(g_viewInfo.drawContext, g_mainmeshRoot),
          new OrbitTool(g_camera),
          new PanTool(g_camera),
          new ZoomTool(g_camera)
        ];
        _Viewer.progressAsyncQueue();
    },
    loadLabelMesh: function (labels) {
        // the internal function is used to achieve that the this.keyword recovers the instance of the calling class rather than the httprequest object
        // http://stackoverflow.com/questions/3162391/how-to-pass-a-value-to-jquery-ajax-success-handler
        this.success = function (data) { // define an internal function
            this.binaryDataParse(data);  // here you can call the class function
        };
        var that = this; // this is part of the trick... 
        $.post(this.UrlBinaryMesh(null, true),
            { "EL": labels.join(',') },
            function (data) { that.success(data); }  // calling from "that" concludes the hack... 
            );
    },
    binaryDataParse: function (data) {
        var view = new jDataView(data);
        var msg = "Id:";

        while (view.tell() < view.byteLength) {
            var id = view.getInt32();
            var dlen = view.getInt32();
            var nextStream = view.tell() + dlen;
            var numChildren = view.getInt16();
            var hasData = view.getInt8();

            var thisMesh = new ViewerMesh();

            if (hasData) {
                thisMesh.AddOneMesh(view);
            }
            for (var i = 0; i < numChildren; i++) {
                thisMesh.AddOneMesh(view);
            }

            view.seek(nextStream);  // there are extra bytes at the end of the stream that I do not understand

            // 
            var xbimtn = this.transformGraph.findNode(id);

            // now we must add the mesh to the tree
            var vinfo = this.vertexInfoViewerMesh(thisMesh);
            if (vinfo != null && xbimtn != null) {
                var material = this.getMaterial(xbimtn.Type);
                theShape = vinfo.createShape(g_mainPack, material);
                xbimtn.Wtransform.addShape(theShape);
            }
            // we must get the right transform in the tree


            // ptransform.addShape(theShape);
        }
        ZoomExtents();
        this.progressAsyncQueue();
    },
    getMaterial: function (type) {
        if (type == null)
            return defaultMaterial;
        if (type == 'IfcProduct') 
            return offWhiteMaterial;
        if (type == 'IfcWall')
            return offWhiteMaterial;
        if (type == 'IfcWallStandardCase')
            return offWhiteMaterial;
        if (type == 'IfcRoof') 
            return darkBlueSlateMaterial;
        if (type == 'IfcBeam') 
            return darkBlueMaterial;
        if (type == 'IfcColumn') 
            return darkBlueMaterial;
        if (type == 'IfcSlab') 
            return lightGreyMaterial;
        if (type == 'IfcWindow') 
            return windowMaterial;
        if (type == 'IfcCurtainWall') 
            return windowMaterial;
        if (type == 'IfcPlate') 
            return windowMaterial;
        if (type == 'IfcDoor')
            return darkRedMaterial;
        if (type == 'IfcMember')
            return darkGreyMaterial;
        if (type == 'IfcSpace') 
            return windowMaterial;
        if (type == 'IfcDistributionElement') 
            return darkBlueMaterial;
        if (type == 'IfcElectricalElement') 
            return greenMaterial;
        return defaultMaterial;
    },
    vertexInfoViewerMesh: function (vMesh) {
        var vertexInfo = o3djs.primitives.createVertexInfo();
        var positionStream = vertexInfo.addStream(3, o3djs.base.o3d.Stream.POSITION);
        var normalStream = vertexInfo.addStream(3, o3djs.base.o3d.Stream.NORMAL);
        var isEmptyShape = true;

        for (var i = 0; i < vMesh.uniquePoints.length; i++) {
            var pindex = vMesh.uniquePoints[i][0]; // index of point in pts
            positionStream.addElement(
                vMesh.points[pindex][0],
                vMesh.points[pindex][1],
                vMesh.points[pindex][2]
                );
            pindex = vMesh.uniquePoints[i][1]; // index of normal
            normalStream.addElement(
                vMesh.normals[pindex][0],
                vMesh.normals[pindex][1],
                vMesh.normals[pindex][2]
                );
            isEmptyShape = false;
        }

        for (var i = 0; i < vMesh.indices.length; ) {
            // loop
            var i1 = vMesh.indices[i++];
            var i2 = vMesh.indices[i++];
            var i3 = vMesh.indices[i++];
            vertexInfo.addTriangle(i1, i2, i3);
            isEmptyShape = false;
        }

        if (!isEmptyShape)
            return vertexInfo;
        return null;
    },
    loadWholeMesh: function () {
        var v = "";
        this.getXML(this.UrlXmlMesh(), setDataXML);
    }
}
Type.registerNamespace('Xbim.Web.Viewer3D'); Xbim.Web.Viewer3D.Resource = {};