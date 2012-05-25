/*
* viewer-mesh.js
* 
Class to produce meshes from functions that are useful when parsing xbim binary stream 
*/
function ViewerMesh() {
    this.points = [];
    this.normals = [];
    this.uniquePoints = [];
    this.indices = [];
    this.meshType = 0;
    this._pointTally = 0;
    this._indexTally = 0;
}

ViewerMesh.prototype.SetType = function (setMeshType) {
    this.meshType = setMeshType;
    this._pointTally = 0;
    this._fanStartIndex = 0;
    this._lastIndex = 0;
};

ViewerMesh.prototype.AddOneMesh = function (view) {

    var numPoints = view.getInt32();
    var numNormals = view.getInt32();
    var numUniquePN = view.getInt32();
    var numTriangles = view.getInt32();
    var numPolygons = view.getInt32();

    // prepare

    this.points = new Array(numPoints);
    this.normals = new Array(numNormals);
    this.uniquePoints = new Array(numUniquePN);
    this.indices = new Array(numTriangles * 3);

    // functions to read indices. Pos.
    var readPIndex;
    if (numPoints <= 0xFF) //we will use byte for indices
        readPIndex = function (data) { return data.getUint8(); }
    else if (numPoints <= 0xFFFF)
        readPIndex = function (data) { return data.getUint16(); }
    else
        readPIndex = function (data) { return data.getUint32(); }

    // functions to read indices. Normal.
    var readNIndex;
    if (numNormals <= 0xFF) //we will use byte for indices
        readNIndex = function (data) { return data.getUint8(); }
    else if (numNormals <= 0xFFFF)
        readNIndex = function (data) { return data.getUint16(); }
    else
        readNIndex = function (data) { return data.getUint32(); }

    // functions to read indices. UniquePN.
    var readPNIndex;
    if (numUniquePN <= 0xFF) //we will use byte for indices
        readPNIndex = function (data) { return data.getUint8(); }
    else if (numUniquePN <= 0xFFFF)
        readPNIndex = function (data) { return data.getUint16(); }
    else
        readPNIndex = function (data) { return data.getUint32(); }


    // get data
    for (var i = 0; i < numPoints; i++) {
        var px = view.getFloat32();
        var py = view.getFloat32();
        var pz = view.getFloat32();
        var pnt = [px, py, pz];
        this.points[i] = pnt;
    }
    for (var i = 0; i < numNormals; i++) {
        var x = view.getFloat32();
        var y = view.getFloat32();
        var z = view.getFloat32();
        var nrm = [px, py, pz];
        this.numNormals[i] = nrm;
    }
    for (var i = 0; i < numNormals; i++) {
        var pindex = readPIndex(view);
        var nindex = readNIndex(view);

        var tpl = [pindex, nindex];
        this.numNormals[i] = tpl;
    }

    for (p = 0; p < numPolygons; p++) {
        var meshType = view.getUint8();
        this.SetType(meshType);
        var indicesCount = view.getUint32();
        for (var i = 0; i < indicesCount; i++) {
            var index = readPNIndex(view);
            this.AddTriangleIndex(index);
        }
    }
};

// this functions converts all types to triangles
ViewerMesh.prototype.AddTriangleIndex = function (uniqueindex) {
    if (this._pointTally == 0)
        this._fanStartIndex = uniqueindex;
    if (this._pointTally < 3) // no one triangle available yet.
    {
        this.indices.push(uniqueindex);
    }
    else {

        switch (this.meshType) {

            case 4: // 0x0004 - TriangleType.GL_TRIANGLES
                this._indexTally
                this.indices[this._indexTally++] = uniqueindex;
                break;
            case 5: // 0x0005 - TriangleType.GL_TRIANGLE_STRIP
                if (this._pointTally % 2 == 0) {
                    this.indices[this._indexTally++] = this._previousToLastIndex;
                    this.indices[this._indexTally++] = this._lastIndex;
                }
                else {
                    this.indices[this._indexTally++] = this._lastIndex;
                    this.indices[this._indexTally++] = this._previousToLastIndex;
                }
                this.indices[this._indexTally++] = uniqueindex;
                break;
            case 6: //   0x0006 - TriangleType.GL_TRIANGLE_FAN
                this.indices[this._indexTally++] = this._fanStartIndex;
                this.indices[this._indexTally++] = this._lastIndex;
                this.indices[this._indexTally++] = uniqueindex;
                break;
            default:
                break;
        }
    }
    this._previousToLastIndex = this._lastIndex;
    this._lastIndex = uniqueindex;
    this._pointTally++;
};