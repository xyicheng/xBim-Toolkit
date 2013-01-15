using glMatrix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Xbim.Xplorer
{
    public class Camera
    {
        vec3 _angles = vec3.create();
        vec3 _position = vec3.create();
        Int32 speed = 100;
        List<System.Windows.Forms.Keys> _pressedKeys = new List<System.Windows.Forms.Keys>();
        mat4 _viewMat = mat4.create();
        mat4 _cameraMat = mat4.create();
        bool _dirty = true;
        Int32 _lastX, _lastY;

        Double _centerX;
        Double _centerY;
        Double _centerZ;
        Double _setZoom;
        mat4 _lookat = mat4.create();
        vec3 modelCenter = vec3.create();
        vec3 _side = vec3.create();
        vec3 _up = vec3.create();
        vec3 _forward = vec3.create();

        public Single moveMod = 1.0f;
        public Single DEG2RAD = (Single)(Math.PI / 180);
        public Single RAD2DEG = (Single)(180 / Math.PI);

        public bool Moving { get; set; }

        public void KeyDown(System.Windows.Forms.Keys keyCode)
        {
            _pressedKeys.Add(keyCode);
        }

        public void KeyUp(System.Windows.Forms.Keys keyCode)
        {
            _pressedKeys.Remove(keyCode);
        }

        public void MouseDown(Int32 X, Int32 Y, MouseButton Button)
        {
            if (Button == MouseButton.Left) Moving = true;
            _lastX = X;
            _lastY = Y;
        }

        public void MouseUp(Int32 X, Int32 Y, MouseButton Button)
        {
            if (Button == MouseButton.Left) Moving = false;
            _lastX = X;
            _lastY = Y;
        }

        public void MouseMove(Int32 X, Int32 Y)
        {
            if (Moving)
            {
                var xDelta = X - _lastX;
                var yDelta = Y - _lastY;

                _lastX = X;
                _lastY = Y;

                _angles[1] += xDelta * 0.012f;
                _angles[0] += yDelta * 0.012f;

                _dirty = true;
            }
        }

        public vec3 getAngles()
        {
            return _angles;
        }

        public void setAngles(vec3 value)
        {
            this._angles = value;
            this._dirty = true;
        }

        public vec3 getPosition()
        {
            return this._position;
        }

        public void setPosition(vec3 value)
        {
            this._position = value;
            this._dirty = true;
        }

        public vec3 sideVec
        {
            get
            {
                var mv = this._lookat;
                this._side[0] = mv[0];
                this._side[1] = mv[4];
                this._side[2] = mv[8];
                return this._side;
            }
        }

        public vec3 upVec
        {
            get
            {
                var mv = this._lookat;
                this._up[0] = mv[1];
                this._up[1] = mv[5];
                this._up[2] = mv[9];
                return this._up;
            }
        }

        public vec3 forwardVec
        {
            get
            {
                var mv = this._lookat;
                this._forward[0] = mv[2];
                this._forward[1] = mv[6];
                this._forward[2] = mv[10];
                return this._forward;
            }
        }

        public void buildRotationMatrix(mat4 mv)
        {
            mat4.rotate(mv, this._angles[0], this.sideVec);
            mat4.rotate(mv, this._angles[1], this.upVec);
            mat4.rotate(mv, this._angles[2], this.forwardVec);
        }

        public mat4 getViewMat()
        {
            if (this._dirty)
            {
                var mv = this._viewMat;

                //start off with the current lookat matrix
                mat4.identity(mv);
                mat4.multiply(mv, this._lookat, mv);

                //rotate by FPS Camera
                this.buildRotationMatrix(mv);

                //translate our camera to its correct position
                mat4.translate(mv, vec3.create(-this._position[0], -this._position[1], -this._position[2]));


                this._dirty = false;
            }

            return this._viewMat;
        }

        public void update(UInt64 frameTime)
        {
            var dir = vec3.create(0, 0, 0);
            var speed = this.moveMod * (frameTime / 10);
            var cam = this._cameraMat;

            // This is our first person movement code. It's not really pretty, but it works
            if (this._pressedKeys.Contains(System.Windows.Forms.Keys.W))
            {
                dir[2] -= speed;
            }
            if (this._pressedKeys.Contains(System.Windows.Forms.Keys.S))
            {
                dir[2] += speed;
            }
            if (this._pressedKeys.Contains(System.Windows.Forms.Keys.A))
            {
                dir[0] -= speed;
            }
            if (this._pressedKeys.Contains(System.Windows.Forms.Keys.D))
            {
                dir[0] += speed;
            }
            if (this._pressedKeys.Contains(System.Windows.Forms.Keys.Space))
            { // Space, moves up
                dir[1] += speed;
            }
            if (this._pressedKeys.Contains(System.Windows.Forms.Keys.Control))
            { // Ctrl, moves down
                dir[1] -= speed;
            }

            if (dir[0] != 0 || dir[1] != 0 || dir[2] != 0)
            {
                mat4.identity(cam);
                mat4.multiply(cam, this._lookat, cam);
                //rotate by FPS Camera
                this.buildRotationMatrix(cam);

                mat4.inverse(cam);

                mat4.multiplyVec3(cam, dir);

                // Move the camera in the direction we are facing
                vec3.add(this._position, dir);

                this._dirty = true;
            }
        }

        public void SetZoomExtents(Double centerX, Double centerY, Double centerZ, Double setZoom)
        {
            this._centerX = centerX;
            this._centerY = centerY;
            this._centerZ = centerZ;
            this._setZoom = setZoom;
        }

        public void ZoomExtents()
        {
            this.resetView();
            mat4.identity(this._lookat);

            var eye = vec3.create(this._centerX, this._centerY - this._setZoom, this._centerZ);
            var forward = vec3.create(0, 1, 0);
            var up = vec3.create(0, 0, 1);

            this.SetLookAt(eye, forward, up);

            this.modelCenter = vec3.create(this._centerX, this._centerY, this._centerZ);

            this._dirty = true;
        }

        private void SetLookAt(vec3 eye, vec3 forward, vec3 up)
        {
            this.resetView();

            eye = vec3.create(eye[0], eye[2], eye[1]);

            var lookatMatrix = mat4.create();

            var side = vec3.create();
            vec3.cross(forward, up, side);
            vec3.normalize(side);

            mat4.identity(lookatMatrix);

            lookatMatrix[0] = side[0];
            lookatMatrix[1] = up[0];
            lookatMatrix[2] = -forward[0];

            lookatMatrix[4] = side[1];
            lookatMatrix[5] = up[1];
            lookatMatrix[6] = -forward[1];

            lookatMatrix[8] = side[2];
            lookatMatrix[9] = up[2];
            lookatMatrix[10] = -forward[2];

            this._lookat = lookatMatrix;

            this._angles = vec3.create();

            this._position[0] = eye[0];
            this._position[1] = eye[2];
            this._position[2] = eye[1];

            this._dirty = true;
        }

        private void resetView()
        {
            _angles = vec3.create();
            _position = vec3.create();

            this._viewMat = mat4.create();
            this._cameraMat = mat4.create();
            this._dirty = true;
        }

    }
}
