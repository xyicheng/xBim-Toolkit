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
    this.currentNormal = -1;
    this._pointTally = 0;
}
ViewerMesh.prototype.addPoint = function (px, py, pz) {
    var pnt = [px, py, pz];
    this.points.push(pnt);
    return this.points.length - 1;
};
ViewerMesh.prototype.setNormal = function (nx, ny, nz) {
    
    var fnd = -1;
    for (var i = 0; i < this.normals.length; i++) {
        if (this.normals[i][0] == nx
            && this.normals[i][1] == ny
             && this.normals[i][2] == nz
             ) {
            fnd = i;
            break;
        }
    }
    if (fnd > -1) {
        this.currentNormal = fnd;
    }
    else {
        var nrm = [nx, ny, nz];
        this.normals.push(nrm);
        this.currentNormal = this.normals.length - 1;
    }
    return this.currentNormal;

};
ViewerMesh.prototype.SetType = function (setMeshType) {
    this.meshType = setMeshType;
    this._pointTally = 0;
    this._fanStartIndex = 0;
    this._lastIndex = 0;
};

ViewerMesh.prototype.AddOneMesh = function (view) {

    var numPoints = view.getInt32();
    if (numPoints == 0) {
        return; //nothing to do
    }

    var indexSize;
    if (numPoints <= 0xFF) //we will use byte for indices
        indexSize = 1;
    else if (numPoints <= 0xFFFF)
        indexSize = 2; //use  unsigned short int for indices
    else
        indexSize = 4; //use unsigned int for indices

    // when adding another mesh indices need to be mapped to the new position in the list
    // 
    var basePointCount = this.points.length;
    for (i = 0; i < numPoints; i++) {
        var x = view.getFloat32();
        var y = view.getFloat32();
        var z = view.getFloat32();
        this.addPoint(x, y, z);
    }
    var currentNormal;

    var numFaces = view.getUint16();
    for (f = 0; f < numFaces; f++) {
        var numPolygons = view.getUint16();
        //get the normals
        var numNormals = view.getUint16();
        for (n = 0; n < numNormals; n++) {
            //get the face normal
            var nx = view.getFloat64();
            var ny = view.getFloat64();
            var nz = view.getFloat64();
            currentNormalIndex = this.setNormal(nx, ny, nz);
        }
        for (p = 0; p < numPolygons; p++) {
            var meshType = view.getUint8();
            this.SetType(meshType);
            var indicesCount = view.getUint16();
            for (var i = 0; i < indicesCount; i++) {
                var index;
                switch (indexSize) {
                    case 1:
                        index = view.getUint8();
                        break;
                    case 2:
                        index = view.getUint16();
                        break;
                    default:
                        index = view.getUint32();
                        break;
                }
                this.AddTriangleIndex(index + basePointCount);
            }
        }
    }
};

ViewerMesh.prototype.AddTriangleIndex = function (iIndex) {
    var uniqueindex = this.UniqueIndex(iIndex);


    if (this._pointTally == 0)
        this._fanStartIndex = uniqueindex;
    if (this._pointTally < 3) // no one triangle available yet.
    {
        this.indices.push(uniqueindex);
    }
    else {

        switch (this.meshType) {

            case 4: // 0x0004 - TriangleType.GL_TRIANGLES
                this.indices.push(uniqueindex);
                break;
            case 5: // 0x0005 - TriangleType.GL_TRIANGLE_STRIP
                if (this._pointTally % 2 == 0) {
                    this.indices.push(this._previousToLastIndex);
                    this.indices.push(this._lastIndex);

                }
                else {
                    this.indices.push(this._lastIndex);
                    this.indices.push(this._previousToLastIndex);
                }
                this.indices.push(uniqueindex);
                break;
            case 6: //   0x0006 - TriangleType.GL_TRIANGLE_FAN
                this.indices.push(this._fanStartIndex);
                this.indices.push(this._lastIndex);
                this.indices.push(uniqueindex);
                break;
            default:
                break;
        }
    }
    this._previousToLastIndex = this._lastIndex;
    this._lastIndex = uniqueindex;
    this._pointTally++;
};

ViewerMesh.prototype.UniqueIndex = function (iIndex) {
    for (var i = 0; i < this.uniquePoints.length; i++) {
        if (this.uniquePoints[i][0] == iIndex && this.uniquePoints[i][1] == this.currentNormal) {
            return i;
        }
    }
    var tuple = [iIndex, this.currentNormal];
    this.uniquePoints.push(tuple);
    return this.uniquePoints.length - 1;
};
