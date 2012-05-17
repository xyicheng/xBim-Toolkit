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

	unsigned char* UICoord = (unsigned char*)fCoord;
	std::list<UIntegerPair>::iterator itUIPair;
	for (itUIPair = _uniquePN.begin(); itUIPair != _uniquePN.end(); itUIPair++)
	{
		UICoord += (this->*writePoint)(UICoord,itUIPair->Int1);
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
	_faceIndexMap = new unsigned int[ NodesInFace ];
	_facePointIndex = 0;
}
void XbimTriangularMeshStreamer::EndFace()
{
	delete [] _faceIndexMap;
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
	unsigned int iIndex = 0;
	std::list<Float3D>::iterator i;
	for (i =  _points.begin(); i != _points.end(); i++)
	{
		Float3D f2 = *i;
		if (
			x == f2.Dim1 &&
			y == f2.Dim2 &&
			z == f2.Dim3 
			)
		{
			// found in existing list
			_faceIndexMap[_facePointIndex++] = iIndex;
			return;
		}
		iIndex++;
	}
	Float3D f;
	f.Dim1 = x;
	f.Dim2 = y;
	f.Dim3 = z;
	_points.insert(_points.end(), f);
	_faceIndexMap[_facePointIndex++] = iIndex;

	// System::Diagnostics::Debug::Write("WritePoint\r\n");
	//Float3D f;
	//f.Dim1 = x;
	//f.Dim2 = y;
	//f.Dim3 = z;
	//_points.insert(_points.end(), f);
}
void XbimTriangularMeshStreamer::WriteTriangleIndex(unsigned int index)
{
	// System::Diagnostics::Debug::Write("WriteTriangleIndex\r\n");
	_currentPolygonCount++; // used in the closing function of each polygon
	unsigned int resolvedPoint = this->getUniquePoint(_faceIndexMap[index], _currentNormalIndex);
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
