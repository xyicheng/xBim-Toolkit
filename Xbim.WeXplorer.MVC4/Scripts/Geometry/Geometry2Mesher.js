/*global define, IndexReader, jDataView, glMatrix, vec3, mat4*/
(function (factory, global) {
    "use strict";
    if (typeof define === 'function' && define.amd) {
        // AMD. Register as an anonymous module.
        define([
            'Geometry/gl-matrix.1.3.7'], factory);
    } else {
        // Browser globals
        global.GeometryMesher = factory(undefined);
    }
}
(function (glMatrix,  undefined) {
    "use strict";

    function Read(data, trans)
    {
        var TriangleIndices = [], Positions = [], Normals = [];

        var q = quat4.create();
        if (trans) {
            q = quat4.fromRotationMatrix(mat4.toMat3(trans));
        }

        var vertexList = []; //holds the actual positions of the vertices in this data set in the mesh
        var normalList = []; //holds the actual normals of the vertices in this data set in the mesh
        var line = "";
        // Read and display lines from the data until the end of
        // the data is reached.
        var lines = data.split('\n');
        for(var j = 0; j < lines.length; j++)
        {
            var line = lines[j];
				   
            var tokens = line.split(' ');
            if (tokens.length > 1) //we need a command and some data
            {
                var command = tokens[0].trim().toUpperCase();
                switch (command)
                {
                    case "P": //todo size
                        vertexList = [];
                        normalList = [];
                        break;
                    case "V": //process vertices
                        for (var i = 1; i < tokens.length; i++)
                        {
                            var xyz = tokens[i].split(',');
                            var p = vec3.create([parseFloat(xyz[0]),parseFloat(xyz[1]),parseFloat(xyz[2])]);
                            if (trans)
                                mat4.multiplyVec3(trans, p, p);
                            vertexList.push(p[0]);
                            vertexList.push(p[1]);
                            vertexList.push(p[2]);
                        }
                        break;
                    case "N": //processes normals
                        for (var i = 1; i < tokens.length; i++)
                        {
                            var  xyz = tokens[i].split(',');
                            var v = vec3.create([parseFloat(xyz[0]),parseFloat(xyz[1]),parseFloat(xyz[2])]);
                            normalList.push(v[0]);
                            normalList.push(v[1]);
                            normalList.push(v[2]);
                        }
                        break;
                    case "T": //process triangulated meshes
                        
                        var currentNormal = vec3.create();		
                        //each time we start a new mesh face we have to duplicate the vertices to ensure that we get correct shading of planar and non planar faces
                        var writtenVertices = [];

                        for (var i = 1; i < tokens.length; i++)
                        {
                            var triangleIndices = tokens[i].split(',');
                            if (triangleIndices.length != 3) throw "Invalid triangle definition";
                            for (var t = 0; t < 3; t++)
                            {
                                var indexNormalPair = triangleIndices[t].split('/');

                                if (indexNormalPair.length > 1) //we have a normal defined
                                {
                                    var normalStr = indexNormalPair[1].trim();
                                    switch (normalStr)
                                    {
                                        case "F": //Front
                                            currentNormal = vec3.create([0, -1, 0]);
                                            break;
                                        case "B": //Back
                                            currentNormal = vec3.create([0, 1, 0]);
                                            break;
                                        case "L": //Left
                                            currentNormal = vec3.create([-1, 0, 0]);
                                            break;
                                        case "R": //Right
                                            currentNormal = vec3.create([1, 0, 0]);
                                            break;
                                        case "U": //Up
                                            currentNormal = vec3.create([0, 0, 1]); 
                                            break;
                                        case "D": //Down
                                            currentNormal = vec3.create([0, 0, -1]);               
                                            break;
                                        default: //it is an index number
                                            var normalIndex = parseInt(indexNormalPair[1], 10);
                                            currentNormal = vec3.create(normalList[normalIndex / 3 + 0], normalList[normalIndex / 3 + 1], normalList[normalIndex / 3 + 2]);
                                            break;
                                    }
                                    if (trans)
                                    {
                                        var v = vec3.create();
                                        quat4.multiplyVec3(q, currentNormal, v);
                                        currentNormal = v;
                                                
                                    }
                                }

                                //now add the index
                                var index = parseInt(indexNormalPair[0], 10);
                                       
                                var alreadyWrittenAt = index; //in case it is the first mesh
                                if (!writtenVertices[index]) //if we haven't  written it in this mesh pass, add it again unless it is the first one which we know has been written
                                {
                                    //all vertices will be unique and have only one normal
                                    writtenVertices[index] = Positions.length/3;
                                    TriangleIndices.push(Positions.length/3);
                                    Positions.push(vertexList[index * 3 + 0]);
                                    Positions.push(vertexList[index * 3 + 1]);
                                    Positions.push(vertexList[index * 3 + 2]);
                                    Normals.push(currentNormal[0]);
                                    Normals.push(currentNormal[1]);
                                    Normals.push(currentNormal[2]);
                                }
                                else //just add the index reference
                                {
                                    alreadyWrittenAt = writtenVertices[index];
                                    TriangleIndices.push(alreadyWrittenAt);
                                }
                            }
                        }
                                
                        break;
                    default:
                        throw "Invalid Geometry Command";
                               
                }
            }
        }
        return {Positions:Positions, Normals:Normals, Indices:TriangleIndices};
    }

    return Read;
}));