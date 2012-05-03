/*
* orbittool.js
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
 * The Orbit tool allows the user to look around the model by click-dragging.
 * The shift key enters a pan mode instead of orbiting.
 */
function OrbitTool(camera) {
  this.camera = camera;
  this.lastOffset = null;
  this.mouseLeftDown = false;
  this.mouseMiddleDown = false;
}

OrbitTool.prototype.handleMouseDown = function (e) {
    this.lastOffset = { x: e.x, y: e.y };
    if (e.button == g_o3d.Event.BUTTON_LEFT) {
        this.mouseLeftDown = true;
        $('#o3d').triggerHandler('orbitStart');
    } else if (e.button == g_o3d.Event.BUTTON_MIDDLE) {
        this.mouseMiddleDown = true;
        $('#o3d').triggerHandler('orbitStart');
    }
};

OrbitTool.prototype.handleMouseMove = function(e) {
  if (e.x !== undefined && (this.mouseLeftDown || this.mouseMiddleDown)) {
      // move Coordinates are relative to the window. Get relative to the canvas
      var offset = getRelativeCoordinates(e, $('#o3d'));

    dY = (offset.y - this.lastOffset.y);
    dX = (offset.x - this.lastOffset.x);
    this.lastOffset = offset;
    panInsteadOfOrbit = (this.mouseLeftDown && this.mouseMiddleDown) ||
        e.shiftKey;
    if (panInsteadOfOrbit) {
      dX *= -1;
      this.camera.target.x -= (dY * Math.cos(this.camera.eye.rotZ) +
          dX * Math.sin(this.camera.eye.rotZ)) /
          (700 / this.camera.eye.distanceFromTarget);
      this.camera.target.y += (-dY * Math.sin(this.camera.eye.rotZ) +
          dX * Math.cos(this.camera.eye.rotZ)) /
          (700 / this.camera.eye.distanceFromTarget);
    } else {
      this.camera.eye.rotZ -= dX / 300;
      this.camera.eye.rotH -= dY / 300;
      this.camera.eye.rotH = peg(this.camera.eye.rotH, 0.1, Math.PI - 0.1);
      //document.getElementById('output').innerHTML = this.camera.eye.rotH;
    }
    this.camera.update();
  }
};

OrbitTool.prototype.handleMouseUp = function(e) {
  if (e.button == g_o3d.Event.BUTTON_LEFT) {
      this.mouseLeftDown = false;
      $('#o3d').triggerHandler('orbitFinish');
  } else if (e.button == g_o3d.Event.BUTTON_MIDDLE) {
    this.mouseMiddleDown = false;
    $('#o3d').triggerHandler('orbitFinish');
}
};

OrbitTool.prototype.handleKeyDown = function (key) {
    if (key == UP) {
        this.camera.target.z += (this.camera.eye.distanceFromTarget/100);
        this.camera.update();
    } else if (key == DOWN) {
        this.camera.target.z -= (this.camera.eye.distanceFromTarget / 100);
        this.camera.update();
    } else if (key == LEFT) {
        this.camera.target.y += (this.camera.eye.distanceFromTarget / 100);
        this.camera.update();
    } else if (key == RIGHT) {
        this.camera.target.y -= (this.camera.eye.distanceFromTarget / 100);
        this.camera.update();
    }
    return false;
};

OrbitTool.prototype.handleKeyUp = function (key) {
    return false;
};

OrbitTool.prototype.reset = function () {
    this.lastOffset = null;
    this.mouseLeftDown = false;
    this.mouseMiddleDown = false;
}