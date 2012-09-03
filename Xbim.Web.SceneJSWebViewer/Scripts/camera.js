function Camera(sceneJSCamera) {
    this.scenecamera = SceneJS.scene("Scene").findNode("camera");
    this.lookat = sceneJSCamera;
    this.optics = this.scenecamera.get("optics");
    this.rotationX = 0;
    this.rotationY = 0;
    this.rotationZ = 0;
    this.pixPerDegreeX = 0;
    this.pixPerDegreeY = 0;

    this.lx;
    this.ly;
    this.lz;
    this.lz1;
    this.eye;
    this.look;

    this.rotation = new Quaternion();
}
Camera.prototype.SetOptics = function (optics) {
    this.scenecamera.set("optics", optics)
    this.optics = this.scenecamera.get("optics");
}
Camera.prototype.GetFOV = function () {
    return this.optics.fovy;
}
Camera.prototype.SetFOV = function (fov) {
    this.optics.set("fovy", fov)
    this.scenecamera.set("optics", this.optics);
}
Camera.prototype.SetPosition = function (eye, look) {
    this.lookat.set("eye", eye);
    this.lookat.set("look", look);
}
Camera.prototype.SetAspectRatio = function (width, height) {
    this.optics.aspect = width / height;
    this.scenecamera.set("optics", this.optics);

    //setup pixel rotation amounts for this aspect ratio/screen size
    this.pixPerDegreeX = this.optics.fovy * this.optics.aspect / width;
    this.pixPerDegreeY = this.optics.fovy / height;

    this.optics = this.scenecamera.get("optics");
}
Camera.prototype.Strafe = function (amount) {
    this.lx = Math.sin((this.rotationY + 90) * DEG2RAD) * amount;
    this.lz1 = (-Math.cos((this.rotationY + 90) * DEG2RAD) * amount);
    this.eye = this.lookat.get("eye");
    this.look = this.lookat.get("look");

    this.eye.x += this.lx;
    this.look.x += this.lx;
    this.eye.z += this.lz1;
    this.look.z += this.lz1;

    this.lookat.set("eye", this.eye);
    this.lookat.set("look", this.look);
}
Camera.prototype.MoveForward = function (amount) {
    this.lx = Math.sin(this.rotationY * DEG2RAD) * amount;
    this.lz1 = (-Math.cos(this.rotationY * DEG2RAD) * amount);

    this.eye = this.lookat.get("eye");
    this.look = this.lookat.get("look");

    this.eye.x += this.lx;
    this.look.x += this.lx;
    this.eye.z += this.lz1;
    this.look.z += this.lz1;

    this.lookat.set("eye", this.eye);
    this.lookat.set("look", this.look);
}
Camera.prototype.Rotate = function (angleY, angleX) {
    if (angleX == 0 && angleY == 0) return;

    //deal with left/right
    this.rotationY += angleY;
    if (this.rotationY >= 360) this.rotationY -= 360; //max 360 degrees for full rotation
    if (this.rotationY < 0) this.rotationY += 360;

    //deal with up/down
    //dont let camera flip to behind as we can only look to our feet and the ceiling, no furthur
    if (this.rotationX + angleX > 89 && this.rotationX + angleX < 271)
        return;

    this.rotationX += angleX;
    if (this.rotationX >= 360) this.rotationX -= 360; //max 360 degrees for full rotation
    if (this.rotationX < 0) this.rotationX += 360;
}
Camera.prototype.CalculateRotation = function () {
    //get the eye and lookat objects
    this.eye = this.lookat.get("eye");
    this.look = this.lookat.get("look");

    //get our new variables
    this.lx = Math.sin(this.rotationY * DEG2RAD);
    this.ly = Math.sin(this.rotationX * DEG2RAD);
    this.lz = -Math.cos(this.rotationY * DEG2RAD); //note this doesn't take account of up down rotation z changes. this is prob fine though

    this.lookat.set("look", { "x": this.eye.x + this.lx, "y": this.eye.y + this.ly, "z": this.eye.z + this.lz });
}