/*
* picktool.js
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
* LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
* DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
* THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
* (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
* OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/


/**
* A tool to allow transforms within the scene to be picked
*
* Requires the picking Client 3d Utilities API.
*/
function PickTool(context, root) {
    this.daysEnum = Object.freeze({"replace":1, "toggle":2});
    this.context = context;
    this.root = root;
    this.pickManager = g_pickManager;
    this.currentSelection = null;
    this.thisSelection = null;
    this.ctrlDown = false;
    this.selectionMode = this.daysEnum.replace;
}


PickTool.prototype.handleMouseDown = function (e) {
    try {
        var worldRay = o3djs.picking.clientPositionToWorldRay(
			e.x,
			e.y,
			this.context,
			g_client.width,
			g_client.height);

        this.pickManager.update();
        var pickInfo = this.pickManager.pick(worldRay);
        if (pickInfo) {
            this.thisSelection = pickInfo.shapeInfo.parent.transform.name;
            //--------- Martin's tweek to have one more event before selection
            $("#o3d").triggerHandler("elementClicked", [this.thisSelection]);
            //----------end of tweek -------------------
            var valid = $("#o3d").triggerHandler("selectValidate", [this.thisSelection]);
            if (valid == undefined)
                valid = true;
            if (valid) {
                $("#o3d").triggerHandler("selectElementClicked", [this.thisSelection]);
                if (this.currentSelection == null || (this.multiSelect() == false)) {
                    if (this.currentSelection != null)
                        $("#o3d").triggerHandler("selectionRemoved", [this.currentSelection]);
                    this.currentSelection = new Array(pickInfo.shapeInfo.parent.transform.name);
                    $("#o3d").triggerHandler("selectionAdded", [this.thisSelection]);
                } else {
                    var iSel = this.currentSelection.indexOf(this.thisSelection);
                    if (iSel != -1) {
                        // removing object from selection
                        this.currentSelection.splice(iSel, 1);
                        $("#o3d").triggerHandler("selectionRemoved", [this.thisSelection]);
                    } else {
                        this.currentSelection = this.currentSelection.concat(this.thisSelection);
                        $("#o3d").triggerHandler("selectionAdded", [this.thisSelection]);
                    }

                }
                $("#o3d").triggerHandler("selectionChanged", [this.currentSelection]);
            }
        } else {
            setStatus('Nothing Selected');
            if (this.currentSelection != null) {
                $("#o3d").triggerHandler("selectionRemoved", [this.currentSelection]);
                $("#o3d").triggerHandler("selectionChanged", []);
            }
            this.currentSelection = null;
            this.thisSelection = null;
        }

    } catch (e) {
        setStatus(('Error in picker: ' + e));
    }
};

PickTool.prototype.multiSelect = function () {
    return (this.ctrlDown || (this.selectionMode == this.daysEnum.toggle))
}

PickTool.prototype.setSelectionMode = function (mode) {
    this.selectionMode = this.daysEnum[mode];
}

PickTool.prototype.handleMouseUp = function (e) {
    if (this.currentSelection) {
        this.SelectInViewer(this.currentSelection);
        // $(g_o3dElement).trigger("elementPicked", [this.thisSelection]);
    } else {
        this.SelectInViewer(null);
        //setAllDefaultMaterial();
    }
};

PickTool.prototype.handleMouseMove = function (e) {
    return;
};

PickTool.prototype.handleKeyDown = function (key) {
    if (key == CTRL) {
        this.ctrlDown = true;
    }
};

PickTool.prototype.handleKeyUp = function (key) {
    if (key == DELETE) {
        if (this.currentSelection != null) {
            HideElement(this.currentSelection);
        }
    }
    if (key == CTRL) {
        this.ctrlDown = false;
    }
};

PickTool.prototype.reset = function () {
    // Nothing yet
};


PickTool.prototype.SelectInViewer = function (selStringsArray) {
    // todo: should be able to remove reference to _viewer
    _Viewer.select(selStringsArray);
//    var values = this.root.getTransformsInTree();
//    for (var iter = 0; iter < values.length; iter++) {
//        var thisv = values[iter];
//        // if (thisv.name == selString) {
//        if (selStringsArray.indexOf(thisv.name) != -1)
//            setMaterial(thisv, selSelectedMaterial);
//        else {
//            var material = GetMaterial(thisv.materialName, defaultMaterial);
//            setMaterial(thisv, material);
//        }
//    }
//    updateClient();
//    // now get the first item in the array and notify listeners
//    //
//    if (selStringsArray.length > 0) {
//        var thisId = selStringsArray[0];
//    }
    return;
};

