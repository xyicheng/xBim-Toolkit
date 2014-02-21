/*global define, IndexReader, jDataView, glMatrix, vec3, mat4*/
(function (factory, global) {
    "use strict";
    if (typeof define === 'function' && define.amd) {
        // AMD. Register as an anonymous module.
        define([
            'Geometry/IndexReader',
            'Geometry/jDataView',
            'Geometry/gl-matrix.1.3.7'], factory);
    } else {
        // Browser globals
        global.GeometryMesher = factory(IndexReader, jDataView, undefined);
    }
}
(function (IndexReader, jDataView, glMatrix, undefined) {
    "use strict";
    var TriangleType =
    {
        GL_TRIANGLES: 0x0004,
        GL_TRIANGLE_STRIP: 0x0005,
        GL_TRIANGLE_FAN: 0x0006
    };

    function GeometryMesher() {
        this.obj = {};
        this.Positions;
        this.Normals;
        this.Indices;
        this._posoffset = 0, this._normaloffset = 0, this._indoffset = 0;

        this._meshType, this._previousToLastIndex, this._lastIndex, this._pointTally, this._fanStartIndex;
    }
    GeometryMesher.prototype.AddPosition = function (x, y, z) {
        this.Positions[this._posoffset + 0] = x;
        this.Positions[this._posoffset + 1] = y;
        this.Positions[this._posoffset + 2] = z;
        this._posoffset += 3;
    }
    GeometryMesher.prototype.AddNormal = function (x, y, z) {
        this.Normals[this._normaloffset + 0] = x;
        this.Normals[this._normaloffset + 1] = y;
        this.Normals[this._normaloffset + 2] = z;
        this._normaloffset += 3;
    }
    GeometryMesher.prototype.BeginPolygon = function (meshType) {
        this._meshType = meshType;
        this._pointTally = 0;
        this._previousToLastIndex = 0;
        this._lastIndex = 0;
        this._fanStartIndex = 0;
    }
    GeometryMesher.prototype.AddTriangleIndex = function (index) {
        if (this._pointTally == 0)
            this._fanStartIndex = index;
        if (this._pointTally < 3) //first time
        {
            this.Indices[this._indoffset] = index;
            this._indoffset++;
        }
        else {
            switch (this._meshType) {
                case TriangleType.GL_TRIANGLES://      0x0004
                    this.Indices[this._indoffset] = index;
                    this._indoffset++;
                    break;
                case TriangleType.GL_TRIANGLE_STRIP:// 0x0005
                    if (this._pointTally % 2 == 0) {
                        this.Indices[this._indoffset] = this._previousToLastIndex;
                        this._indoffset++;
                        this.Indices[this._indoffset] = this._lastIndex;
                        this._indoffset++;
                    }
                    else {
                        this.Indices[this._indoffset] = this._lastIndex;
                        this._indoffset++;
                        this.Indices[this._indoffset] = this._previousToLastIndex;
                        this._indoffset++;
                    }
                    this.Indices[this._indoffset] = index;
                    this._indoffset++;
                    break;
                case TriangleType.GL_TRIANGLE_FAN://   0x0006
                    this.Indices[this._indoffset] = this._fanStartIndex;
                    this._indoffset++;
                    this.Indices[this._indoffset] = this._lastIndex;
                    this._indoffset++;
                    this.Indices[this._indoffset] = index;
                    this._indoffset++;
                    break;
                default:
                    break;
            }
        }
        this._previousToLastIndex = this._lastIndex;
        this._lastIndex = index;
        this._pointTally++;
    }
    GeometryMesher.prototype.Mesh = function (br, transform) {
        transform = mat4.create(transform);
        var numPositions = br.getUint32();
        //if we the mesh is smaller that 64K then try and add it to this mesh, if it is bigger than 65K we just have to stake what we can
        //if (numPositions < ushort.MaxValue && builder.PositionCount > 0 && (builder.PositionCount + numPositions >= ushort.MaxValue)) //we cannot build meshes bigger than this and pass them through to standard graphics buffers
        //    return false;        
        var numNormals = br.getUint32();
        var numUniques = br.getUint32();
        var numTriangles = br.getUint32();
        var numPolygons = br.getUint32();

        //If this piece of geometry is too big then return empty
        if (numTriangles * 3 > 256 * 256) {
            return null;
        }

        this.Positions = new Float32Array(numUniques * 3);
        this.Normals = new Float32Array(numUniques * 3);
        this.Indices = new Int32Array(numTriangles * 3);

        var PositionReader = new IndexReader(numPositions, br);
        var NormalsReader = new IndexReader(numNormals, br);
        var UniquesReader = new IndexReader(numUniques, br);

        var pos = new Float32Array(br.buffer, br.tell(), numPositions * 3);
        br.seek(br.tell() + (numPositions * 3 * 4));
        var nrm = new Float32Array(br.buffer, br.tell(), numNormals * 3);
        br.seek(br.tell() + (numNormals * 3 * 4));

        //// coordinates of positions
        ////
        //for (var i = 0; i < numPositions*3; )
        //{
        //    pos[i++] = br.getFloat32();
        //    pos[i++] = br.getFloat32();
        //    pos[i++] = br.getFloat32();
        //}
        //// dimensions of normals
        ////
        //for (var i = 0; i < numNormals*3; )
        //{
        //    nrm[i++] = br.getFloat32();
        //    nrm[i++] = br.getFloat32();
        //    nrm[i++] = br.getFloat32();
        //}

        // loop twice for how many indices to create the point/normal combinations.  
        if (mat4.equal(mat4.identity, transform)) {
            for (var i = 0; i < numUniques; i++) {
                var readpositionI = PositionReader.ReadIndex();
                this.AddPosition(pos[readpositionI * 3], pos[readpositionI * 3 + 1], pos[readpositionI * 3 + 2]);
            }
            for (var i = 0; i < numUniques; i++) {
                var readnormalI = NormalsReader.ReadIndex();
                this.AddNormal(nrm[readnormalI * 3], nrm[readnormalI * 3 + 1], nrm[readnormalI * 3 + 2]);
            }
        }
        else {
            for (var i = 0; i < numUniques; i++) {
                var readpositionI = PositionReader.ReadIndex();
                var tfdPosition = mat4.multiplyVec3(transform, vec3.createFrom(pos[readpositionI * 3], pos[readpositionI * 3 + 1], pos[readpositionI * 3 + 2]));
                this.AddPosition(tfdPosition[0], tfdPosition[1], tfdPosition[2]);
            }
            for (var i = 0; i < numUniques; i++) {
                // todo: use a quaternion extracted from the matrix instead
                //
                var readnormalI = NormalsReader.ReadIndex();
                var v = vec3.createFrom(nrm[readnormalI * 3], nrm[readnormalI * 3 + 1], nrm[readnormalI * 3 + 2]);
                vec3.normalize(v, v);
                this.AddNormal(v[0], v[1], v[2]);
            }
        }

        for (var p = 0; p < numPolygons; p++) {
            // set the state
            var mt = br.getUint8();
            switch (mt) {
                case TriangleType.GL_TRIANGLE_FAN:
                    this._meshType = TriangleType.GL_TRIANGLE_FAN;
                    break;
                case TriangleType.GL_TRIANGLE_STRIP:
                    this._meshType = TriangleType.GL_TRIANGLE_STRIP;
                    break;
                case TriangleType.GL_TRIANGLES:
                    this._meshType = TriangleType.GL_TRIANGLES;
                    break;
            }

            var indicesCount = br.getUint32();
            this.BeginPolygon(this._meshType, indicesCount);
            //get the triangles
            for (var i = 0; i < indicesCount; i++) {
                this.AddTriangleIndex(UniquesReader.ReadIndex());
            }
        }
        return { Positions:this.Positions, Normals: this.Normals, Indices: this.Indices };
    }

    return GeometryMesher;
}, this));