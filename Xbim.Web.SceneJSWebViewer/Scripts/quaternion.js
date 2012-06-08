function Quaternion(w, x, y, z) {
    this.w = w;
    this.x = x;
    this.y = y;
    this.z = z;
    this.tolerance = 0.00001;
    this.PIOVER180 = Math.PI / 180;
}

Quaternion.prototype.normalize = function () {
    var mag2 = this.w * this.w + this.x * this.x + this.y * this.y + this.z * this.z;
    if (Math.abs(mag2) > this.tolerance && Math.abs(mag2 - 1.0) > this.tolerance) {
        var mag = Math.sqrt(mag2);
        this.w /= mag;
        this.x /= mag;
        this.y /= mag;
        this.z /= mag;
    }
}

Quaternion.prototype.getConjugate = function () {
    return new Quaternion(-this.x, -this.y, -this.z, w);
}

Quaternion.prototype.multiply = function (rq) {
    return new Quaternion(this.w * rq.x + this.x * rq.w + this.y * rq.z - this.z * rq.y,
	                        this.w * rq.y + this.y * rq.w + this.z * rq.x - this.x * rq.z,
	                        this.w * rq.z + this.z * rq.w + this.x * rq.y - this.y * rq.x,
	                        this.w * rq.w - this.x * rq.x - this.y * rq.y - this.z * rq.z);
}

Quaternion.prototype.multiply = function (x, y, z) {
    var vecQuat = new Quaternion(),
        resQuat = new Quaternion();
    vecQuat.x = x;
    vecQuat.y = y;
    vecQuat.z = z;
    vecQuat.w = 0.0;

    resQuat = vecQuat.multiply(this.getConjugate());
    resQuat = this.multiply(resQuat);

    return { "x": resQuat.x, "y": resQuat.y, "z": resQuat.z };
}

Quaternion.prototype.FromAxis = function (vector, angle) {
    angle *= 0.5;

    var sinAngle = Math.sin(angle);

    this.x = (vn.x * sinAngle);
    this.y = (vn.y * sinAngle);
    this.z = (vn.z * sinAngle);
    this.w = Math.cos(angle);
}
Quaternion.prototype.FromEuler = function (pitch, yaw, roll) {

    var p = pitch * this.PIOVER180 / 2.0;
    var y = yaw * this.PIOVER180 / 2.0;
    var r = roll * this.PIOVER180 / 2.0;

    var sinp = Math.sin(p);
    var siny = Math.sin(y);
    var sinr = Math.sin(r);
    var cosp = Math.cos(p);
    var cosy = Math.cos(y);
    var cosr = Math.cos(r);

    this.x = sinr * cosp * cosy - cosr * sinp * siny;
    this.y = cosr * sinp * cosy + sinr * cosp * siny;
    this.z = cosr * cosp * siny - sinr * sinp * cosy;
    this.w = cosr * cosp * cosy + sinr * sinp * siny;

    this.normalize();
}
Quaternion.prototype.getAxisAngle = function () {
    var scale = Math.sqrt(this.x * this.x + this.y * this.y + this.z * this.z);
    var x = x / scale;
    var y = y / scale;
    var z = z / scale;
    var angle = Math.acos(w) * 2.0;
    return { "x": x, "y": y, "z": z, "angle": angle };
}