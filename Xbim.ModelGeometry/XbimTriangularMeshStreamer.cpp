/* ==================================================================
proposed structure of stream for triangular meshes:
CountUniquePositions		// int
CountUniqueNormals			// int
CountUniquePositionNormals	// int
CountAllTriangles			// int // used to prepare index array if needed
CountPolygons				// int
[PosX, PosY, PosZ]			// 3 * floats * CountUniquePositions
...
[NrmX, NrmY, NrmZ]			// 3 * floats * CountUniqueNormals
...
[iPos, INrm]				// int, short or byte f(CountUniquePositions, CountUniqueNormals)
...
[Polygons:  
	PolyType // byte
	PolygonLen // int
	iUniquePositionNormals // int, short or byte f(CountUniquePositions, CountUniqueNormals)
	...
]...
*/

#include "StdAfx.h"
#include "XbimTriangularMeshStreamer.h"
// #pragma unmanaged

// ==================================================================
// begin class TriangularMeshStreamer
//
XbimTriangularMeshStreamer::XbimTriangularMeshStreamer() 
{
	// System::Diagnostics::Debug::Write("TriangularMeshStreamer Init\r\n");
}
int XbimTriangularMeshStreamer::StreamSize()
{
	
	int iPos = _points.size();
	int iNrm = _normals.size();
	int iUPN = _uniquePN.size();
	int iUniqueSize = sizeOptimised(iUPN);
	
	int iPolSize = 0;
	unsigned int iIndex = 0;
	std::list<PolygonInfo>::iterator i;
	for (i =  _poligons.begin(); i != _poligons.end(); i++)
	{
		PolygonInfo pol = *i;
		iPolSize += sizeof(PolygonInfo);
		iPolSize += pol.IndexCount * iUniqueSize;
	}

	int iSize = 5 * sizeof(int);       // initial headers
	iSize += iPos * 3 * sizeof(float); // positions
	iSize += iNrm * 3 * sizeof(float); // normals
	iSize += iUPN * (sizeOptimised(iPos) + sizeOptimised(iNrm)); // unique points
	iSize += iPolSize;

	return iSize;
}
int XbimTriangularMeshStreamer::sizeOptimised(unsigned int maxIndex)
{
	int indexSize;
	if(maxIndex <= 0xFF) //we will use byte for indices
		indexSize = sizeof(unsigned char) ;
	else if(maxIndex <= 0xFFFF) 
		indexSize = sizeof(unsigned short); //use  unsigned short int for indices
	else
		indexSize = sizeof(unsigned int); //use unsigned int for indices
	return indexSize;
}
void XbimTriangularMeshStreamer::BeginPolygon(GLenum type)
{
	// System::Diagnostics::Debug::Write("Begin polygon\r\n");
	PolygonInfo p;
	p.GLType = type;
	p.IndexCount = 0;

	_poligons.insert(_poligons.end(), p);
	_currentPolygonCount = 0;
}
void XbimTriangularMeshStreamer::EndPolygon()
{
	// System::Diagnostics::Debug::Write("End polygon\r\n");
	_poligons.back().IndexCount = _currentPolygonCount;
}

void XbimTriangularMeshStreamer::info(char string)
{
	// System::Diagnostics::Debug::WriteLine(string.ToString());
}

void XbimTriangularMeshStreamer::info(int Number)
{
	// System::Diagnostics::Debug::WriteLine(Number);
}

void XbimTriangularMeshStreamer::SetNormal(float x, float y, float z)
{
	// finds the index of the current normal
	// otherwise adds it to the collection
	//
	// System::Diagnostics::Debug::Write("Set normal\r\n");
	unsigned int iIndex = 0;
	std::list<Float3D>::iterator i;
	for (i =  _normals.begin(); i != _normals.end(); i++)
	{
		Float3D f2 = *i;
		if (
			x == f2.Dim1 &&
			y == f2.Dim2 &&
			z == f2.Dim3 
			)
		{
			_currentNormalIndex = iIndex;
			return;
		}
		iIndex++;
	}
	Float3D f;
	f.Dim1 = x;
	f.Dim2 = y;
	f.Dim3 = z;
	_normals.insert(_normals.end(), f);
	_currentNormalIndex = iIndex;
}
void XbimTriangularMeshStreamer::WritePoint(float x, float y, float z)
{
	// System::Diagnostics::Debug::Write("WritePoint\r\n");
	Float3D f;
	f.Dim1 = x;
	f.Dim2 = y;
	f.Dim3 = z;
	_points.insert(_points.end(), f);
}
void XbimTriangularMeshStreamer::WriteTriangleIndex(unsigned int index)
{
	// System::Diagnostics::Debug::Write("WriteTriangleIndex\r\n");
	_currentPolygonCount++; // used in the closing function of each polygon
	unsigned int resolvedPoint = this->getUniquePoint(index, _currentNormalIndex);
	_indices.insert(_indices.end(), resolvedPoint);
}

unsigned int XbimTriangularMeshStreamer::getUniquePoint(unsigned int pointIndex, unsigned int normalIndex)
{
	// System::Diagnostics::Debug::Write("getUniquePoint\r\n");
	unsigned int iIndex = 0;
	std::list<UIntegerPair>::iterator i;
	for (i = _uniquePN.begin(); i != _uniquePN.end(); i++)
	{
		UIntegerPair iter = *i;
		if (
			pointIndex == iter.Int1 &&
			normalIndex == iter.Int2 
			)
		{
			return iIndex;
		}
		iIndex++;
	}
	UIntegerPair f;
	f.Int1 = pointIndex;
	f.Int2 = normalIndex; 
	_uniquePN.insert(_uniquePN.end(), f);
	return iIndex;
}
