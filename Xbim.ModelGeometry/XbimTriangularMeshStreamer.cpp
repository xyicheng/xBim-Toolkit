/* ==================================================================

The stream has an unusual doble indirection to points and normals to be able to retain unique position idenity for those
frameworks that do not consider or require normal specifications (therefore saving streaming size and reducing video 
memory usage

Structure of stream for triangular meshes:
CountUniquePositions		// int
CountUniqueNormals			// int
CountUniquePositionNormals	// int
CountAllTriangles			// int // used to prepare index array if needed
CountPolygons				// int
[PosX, PosY, PosZ]			// 3 * floats * CountUniquePositions
...
[NrmX, NrmY, NrmZ]			// 3 * floats * CountUniqueNormals
...
[iPos]						// int, short or byte f(CountUniquePositions)
...					
[iNrm]						// int, short or byte f(CountUniqueNormals)
...
[Polygons:  
	PolyType // byte
	PolygonLen // int
	[UniquePositionNormal]  // int, short or byte f(CountUniquePositions)
	...
]...


Example for a 1x1x1 box:

		8 // number of points
		6 // number of normals (one for each face)
		24 // each of the 8 points in a box belongs to 3 faces; it has therefore 3 normals
		12 // 2 triangles per face
		1  // all shape in one call
	+->	0, 0, 0 (index: 0)  // these are the 8 points
	|	0, 1, 0
	|	1, 0, 0
	|	1, 1, 0
	|	0, 0, 1
	|	0, 1, 1
	|	1, 0, 1
	|	1, 1, 1
	|	0, 0, 1 // top face normal
	|	1, 0, 0 // other normals...
	|	0, 1, 0
	|	0, 0, -1 
	|	-1, 0, 0 
+-> |	0, -1, 0 (index: 5)
|	|	// points indices (1 byte because of size)
|	|	4 (0) // first two triangles (unique point 0 to 3) point to unique positions 0,1,2,3
|	|	5 (1)
|	|	7 (2)
|	|	6 (3)
|	+-=	0 (4) <-------------------------------------------------------------------------+
|		[... omissis...]                                                                |
|		// normal indices                                                               |
|		0 (index:0) // first two triangles (unique point 0 to 3) share normal index 0   |
|		0 (index:1)                                                                     |
|		0 (index:2)                                                                     |
|		0 (index:3)                                                                     |
+---=	5 (index:4) <-------------------------------------------------------------------+
		5                                                                               |
		5                                                                               |
		[... omissis...]                                                                |
		// unique indices per polygon                                                   |
		//                                                                              |
		4 // polygons type (series of triangles)                                        |
		36 // lenght of the stream of indices for the first polygon                     |
		0 // first triangle of top face                                                 |
		1                                                                               |
		2                                                                               |
		0 // second of top face                                                         |
		2                                                                               |
		3                                                                               |
		4 // first triangle of front face is unique point 4 =---------------------------+ (pointing to position 0 and normal 5)
		5
		6
		[...more triangles follow...]
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

void XbimTriangularMeshStreamer::StreamTo(unsigned char* pStream)
{
	int* iCoord = (int *)(pStream);

	int iCPoints =  _points.size(); 
	int iCNormals =  _normals.size(); 
	int iCUnique =  _uniquePN.size(); 

	*iCoord++ = iCPoints;
	*iCoord++ = iCNormals; 
	*iCoord++ = iCUnique; 
	*iCoord++ = 0; // to be filled at the end of the function after analysis of the polygons
	*iCoord++ = _poligons.size(); 

	int (XbimTriangularMeshStreamer::*writePoint) (unsigned char*, unsigned int);
	if(iCPoints<=0xFF) //we will use byte for indices
		writePoint = &XbimTriangularMeshStreamer::WriteByte;
	else if(iCPoints<=0xFFFF) //use  unsigned short int for indices
		writePoint = &XbimTriangularMeshStreamer::WriteShort;
	else //use unsigned int for indices
		writePoint = &XbimTriangularMeshStreamer::WriteInt;

	int (XbimTriangularMeshStreamer::*writeNormal) (unsigned char*, unsigned int);
	if(iCNormals<=0xFF) //we will use byte for indices
		writeNormal = &XbimTriangularMeshStreamer::WriteByte;
	else if(iCNormals<=0xFFFF) //use  unsigned short int for indices
		writeNormal = &XbimTriangularMeshStreamer::WriteShort;
	else //use unsigned int for indices
		writeNormal = &XbimTriangularMeshStreamer::WriteInt;

	int (XbimTriangularMeshStreamer::*writeUniqueIndex) (unsigned char*, unsigned int);
	if(iCUnique<=0xFF) //we will use byte for indices
		writeUniqueIndex = &XbimTriangularMeshStreamer::WriteByte;
	else if(iCUnique<=0xFFFF) //use  unsigned short int for indices
		writeUniqueIndex = &XbimTriangularMeshStreamer::WriteShort;
	else //use unsigned int for indices
		writeUniqueIndex = &XbimTriangularMeshStreamer::WriteInt;

	// starts from here with new pointer type
	float* fCoord = (float *)(iCoord);
	
	std::list<Float3D>::iterator i;
	for (i = _points.begin(); i != _points.end(); i++)
	{
		*fCoord++ = i->Dim1; 
		*fCoord++ = i->Dim2; 
		*fCoord++ = i->Dim3; 
	}
	for (i = _normals.begin(); i != _normals.end(); i++)
	{
		*fCoord++ = i->Dim1; 
		*fCoord++ = i->Dim2; 
		*fCoord++ = i->Dim3; 
	}

	// picks up from fCoord's address
	// 
	// the decision to stream the indices in two blocks should allow a simple reduction of the stream to be sent
	// if normals are not required.
	unsigned char* UICoord = (unsigned char*)fCoord;
	std::list<UIntegerPair>::iterator itUIPair;
	for (itUIPair = _uniquePN.begin(); itUIPair != _uniquePN.end(); itUIPair++)  // write point indices
	{
		UICoord += (this->*writePoint)(UICoord,itUIPair->Int1);
	}
	for (itUIPair = _uniquePN.begin(); itUIPair != _uniquePN.end(); itUIPair++)  // write normal indices
	{
		UICoord += (this->*writeNormal)(UICoord,itUIPair->Int2);
	}

	// now the polygons
	// 
	// setup the iterator for the indices; then use it within the loop of polygons
	std::list<unsigned int>::iterator iInd;
	iInd = _indices.begin();
	std::list<PolygonInfo>::iterator iPol;

	// countTriangles is increased for the count triangles in each polygon in the coming loop
	//
	int countTriangles = 0;
	for (iPol = _poligons.begin(); iPol != _poligons.end(); iPol++)
	{
		GLenum tp = iPol->GLType;
		unsigned int iThisPolyIndexCount = iPol->IndexCount;

		if (tp == GL_TRIANGLES)
			countTriangles += iThisPolyIndexCount / 3; // three point make a triangle
		else
			countTriangles += iThisPolyIndexCount - 2; // after the first triangle every point adds one

		UICoord += WriteByte(UICoord,(unsigned int)iPol->GLType);
		UICoord += WriteInt(UICoord,(unsigned int)iPol->IndexCount);
		unsigned int iThisPolyIndex = 0;
		while (iThisPolyIndex++ < iThisPolyIndexCount)
		{
			UICoord += (this->*writeUniqueIndex)(UICoord,*iInd);
			iInd++; // move to next index
		}
	}
	// write the number of triangles that make up the whole mesh
	iCoord = (int *)(pStream);
	iCoord += 3;
	*iCoord = countTriangles;

	int calcSize = UICoord - pStream;
}

int XbimTriangularMeshStreamer::WriteByte(unsigned char* pStream, unsigned int value)
{
	// byte* bCoord = (byte *)(pStream);
	*pStream = (char)value;
	return sizeof(char);
}

int XbimTriangularMeshStreamer::WriteShort(unsigned char* pStream, unsigned int value)
{
	unsigned short* bCoord = (unsigned short *)(pStream);
	*bCoord = (unsigned short)value;
	return sizeof(unsigned short);
}

int XbimTriangularMeshStreamer::WriteInt(unsigned char* pStream, unsigned int value)
{
	unsigned int* bCoord = (unsigned int *)(pStream);
	*bCoord = (unsigned int)value;
	return sizeof(unsigned int);
}

void XbimTriangularMeshStreamer::BeginFace(int NodesInFace)
{
	if (NodesInFace > 0)
	{
		_useFaceIndexMap = true;
		_faceIndexMap = new unsigned int[ NodesInFace ];
		_facePointIndex = 0;
	}
	else
	{
		_useFaceIndexMap = false;
	}
}

void XbimTriangularMeshStreamer::EndFace()
{
	if (_useFaceIndexMap)
	{
		delete [] _faceIndexMap;
	}
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
		iPolSize += sizeof(char) + sizeof(int); // one byte for type and one int for count
		iPolSize += pol.IndexCount * iUniqueSize;
	}

	int iSize = 5 * sizeof(int);       // initial headers
	iSize += iPos * 3 * sizeof(float); // positions
	iSize += iNrm * 3 * sizeof(float); // normals
	iSize += iUPN * (sizeOptimised(iPos) + sizeOptimised(iNrm)); // unique points
	iSize += iPolSize;

	if (iSize == 6228)
	{
		int iT = 102 + 5;
		iT += 12;
	}

	if (iSize == 5610)
	{
		int iT = 102 + 5;
		iT += 12;
	}
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
	System::Diagnostics::Debug::WriteLine(string.ToString());
}

void XbimTriangularMeshStreamer::info(int Number)
{
	System::Diagnostics::Debug::WriteLine(Number);
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

unsigned int XbimTriangularMeshStreamer::WritePoint(float x, float y, float z)
{
	unsigned int iIndex = 0;
	std::list<Float3D>::iterator i;
	for (i =  _points.begin(); i != _points.end(); i++)
	{
		// Float3D f2 = *i;
		if (
			x == i->Dim1 &&
			y == i->Dim2 &&
			z == i->Dim3 
			)
		{
			// found in existing list
			if (_useFaceIndexMap)
				_faceIndexMap[_facePointIndex++] = iIndex;
			return iIndex;
		}
		iIndex++;
	}
	Float3D f;
	f.Dim1 = x;
	f.Dim2 = y;
	f.Dim3 = z;
	_points.insert(_points.end(), f);
	if (_useFaceIndexMap)
		_faceIndexMap[_facePointIndex++] = iIndex;
	return iIndex;
}

// when called from OpenCascade converts the 1-based index to of one face to the global 0-based index (_useFaceIndexMap)
// otherwise just add the uniquepoint without the face mapping indirection (0-based to 0-based)
void XbimTriangularMeshStreamer::WriteTriangleIndex(unsigned int index)
{
	// System::Diagnostics::Debug::Write("WriteTriangleIndex\r\n");
	_currentPolygonCount++; // used in the closing function of each polygon to write the number of points to the stream

	unsigned int resolvedPoint;
	if (_useFaceIndexMap)
	{
		resolvedPoint = this->getUniquePoint(_faceIndexMap[index-1], _currentNormalIndex); // index-1 becasue OCC starts from 1
	}
	else
	{
		resolvedPoint = this->getUniquePoint(index, _currentNormalIndex); // this call comes from OpenGL; no index - 1
	}
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
