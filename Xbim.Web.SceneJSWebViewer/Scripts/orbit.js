function orbit() {
    this.name = "orbit";
    this.fullscreenelement = null;

    this.lastX;
    this.lastY;
    this.deltaX;
    this.deltaY;
    this.mouseLeft;
    this.mouseRight;
    this.movementX;
    this.movementY;
    this.amount;
}
orbit.prototype.init = function () {
    camera.rotationX = 0;
    camera.rotationY = 0;
    camera.rotationZ = 0;
    ZoomExtents();
}
orbit.prototype.mousewheel = function (event) {
    //get mouse wheel amount, capping it at 4x

    //hide quick properties while scrolling
    $("#quickProperties").hide();

    this.amount = (event.wheelDelta / 120) || -event.detail;
    if (this.amount > 0) {
        this.amount = this.amount > 4 ? 4 : this.amount;
    } else {
        this.amount = this.amount < -4 ? -4 : this.amount;
    }
    camera.MoveForward(this.amount * moveMod * 10);
    event.preventDefault();
}
orbit.prototype.mousedown = function (event) {
    this.lastX = (event.pageX - canvas.offsetLeft);
    this.lastY = (event.pageY - canvas.offsetTop);
}
orbit.prototype.mousemove = function (event, leftDown, rightDown, dragging) {
    this.deltaX = ((event.pageX - canvas.offsetLeft) - this.lastX);
    this.deltaY = ((event.pageY - canvas.offsetTop) - this.lastY);

    this.lastX = (event.pageX - canvas.offsetLeft);
    this.lastY = (event.pageY - canvas.offsetTop);

    //if we are only dragging left mouse
    if (leftDown && !rightDown) {
        RotationZ += this.deltaX * 0.5;
        RotationY += this.deltaY * 0.5;
        newInput = true;
    } else if (!leftDown) { //only dragging right
        moveXAmount += this.deltaX * moveMod;
        moveYAmount -= this.deltaY * moveMod;
        newInput = true;
    } else {  //dragging both buttons
    }
}