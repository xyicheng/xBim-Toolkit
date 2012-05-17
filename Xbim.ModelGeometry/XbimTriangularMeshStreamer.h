#pragma once
#include <list>
#include "XbimLocation.h"
#include "XbimBoundingBox.h"
#include "IXbimMeshGeometry.h"
#include "IXbimGeometryModel.h"
#include <TopoDS_Shape.hxx>
#include <TopoDS_Compound.hxx>
#include <TopTools_DataMapOfShapeInteger.hxx>
#include <TopTools_IndexedMapOfShape.hxx>

#pragma unmanaged

// suporting structs
struct Float3D{
	float Dim1;
	float Dim2;
	float Dim3;
};
struct UIntegerPair {
	unsigned int Int1;
	unsigned int Int2;
};
struct PolygonInfo {
	GLenum GLType;
	int IndexCount;
};

// Class to receive the calls that create the memory stream of the geometry cache files. (CB)
//


public class XbimTriangularMeshStreamer
{
public: 
	XbimTriangularMeshStreamer();
	void BeginPolygon(GLenum type);
	void SetNormal(float x, float y, float z);
	void WritePoint(float x, float y, float z);
	void WriteTriangleIndex(unsigned int index);
	void EndPolygon();
	void info(char string);
	void info(int Number);
	int StreamSize();
private:
	unsigned int getUniquePoint(unsigned int pointIndex, unsigned int normalIndex);
	int sizeOptimised(unsigned int maxIndex);
	unsigned int _currentNormalIndex;
	std::list<Float3D> _points;
	std::list<Float3D> _normals;
	std::list<UIntegerPair> _uniquePN;	// unique point and normal combination
	std::list<PolygonInfo> _poligons;	// polygon informations
	std::list<unsigned int> _indices;	
	unsigned int _currentPolygonCount;
};
#pragma managed