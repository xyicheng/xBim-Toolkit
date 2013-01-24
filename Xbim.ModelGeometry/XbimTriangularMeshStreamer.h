#pragma once

#include <list>
#include <map>
#include <unordered_map>
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
struct Float3D
{
	float Dim1;
	float Dim2;
	float Dim3;
	Float3D(float D1,	float D2,	float D3)
		:Dim1(D1),Dim2(D2),Dim3(D3)
	{

	}

	operator size_t () const
	{
		std::hash<float> h;
		return (size_t)(h(Dim1) ^ h(Dim2) ^ h(Dim3));
	}

	
	bool operator==(const Float3D& A) const
	{
		return Dim1 == A.Dim1 && Dim2 == A.Dim2 && Dim3==A.Dim3;	
	}

	bool operator<(const Float3D& A) const
	{
		if (Dim1 != A.Dim1)
			return Dim1 < A.Dim1;
		if (Dim2 != A.Dim2)
			return Dim2 < A.Dim2;
		return Dim3 < A.Dim3;
	}

	
};

struct UIntegerPair {
	unsigned int PositionIndex;
	unsigned int NormalIndex;

	bool operator<(const UIntegerPair& A) const
	{
		if (PositionIndex != A.PositionIndex)
			return PositionIndex < A.PositionIndex;
		return NormalIndex < A.NormalIndex;
	}

	bool operator==(const UIntegerPair& A) const
	{
		return PositionIndex == A.PositionIndex && NormalIndex == A.NormalIndex;	
	}

	 operator size_t() const
	{
		return (size_t)( PositionIndex ^ NormalIndex);
	}

};
struct PolygonInfo {
	GLenum GLType;
	int IndexCount;
};

typedef std::pair <Float3D,unsigned int> Float3DUInt_Pair;
// Class to receive the calls that create the memory stream of the geometry cache files. (CB)
//
public class XbimTriangularMeshStreamer
{
public: 
	XbimTriangularMeshStreamer(int repLabel, int surfaceLabel);
	int RepresentationLabel;
	int SurfaceStyleLabel;
	void BeginFace(int NodesInFace);
	void EndFace();
	void BeginPolygon(GLenum type);
	void EndPolygon();
	void SetNormal(float x, float y, float z);
	unsigned int WritePoint(float x, float y, float z);
	void WriteTriangleIndex(unsigned int index);
	void info(char string);
	void info(int Number);
	unsigned int StreamSize();
	unsigned int StreamTo(unsigned char* pStream);
private:
	

	unsigned int getUniquePoint(unsigned int pointIndex, unsigned int normalIndex);
	int sizeOptimised(unsigned int maxIndex);
	unsigned int _currentNormalIndex;
	unsigned int _facePointIndex;

	std::unordered_map<Float3D,unsigned int> _pointsMap;
	std::list<Float3D> _points;

	std::unordered_map<Float3D,unsigned int> _normalsMap;
	std::list<Float3D> _normals;

	std::unordered_map<UIntegerPair,unsigned int> _uniquePNMap;
	std::list<UIntegerPair> _uniquePN;	// unique point and normal combination

	std::list<PolygonInfo> _polygons;	// polygon informations
	std::list<unsigned int> _indices;	
	unsigned int _currentPolygonCount;
	unsigned int * _faceIndexMap;       // we're removing duplicates for the points; this array contains the mapping of non-optimised to optimised indices for a face
	bool _useFaceIndexMap;
	int WriteByte(unsigned char* pStream, unsigned int value);
	int WriteShort(unsigned char* pStream, unsigned int value);
	int WriteInt(unsigned char* pStream, unsigned int value);

};
#pragma managed