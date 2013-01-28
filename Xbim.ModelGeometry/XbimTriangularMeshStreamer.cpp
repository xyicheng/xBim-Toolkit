// All cases when the geometry of a mesh is calculated shold be passed through this class to ensure consisstency in the stream. 
//
// for documentation on the binary format of the stream have a look at Xbim.ModelGeometry.Scene.XbimTriangulatedModelStream

#include "StdAfx.h"
#include "XbimTriangularMeshStreamer.h"
// #pragma unmanaged

// ==================================================================
// begin class TriangularMeshStreamer
//
XbimTriangularMeshStreamer::XbimTriangularMeshStreamer(int repLabel, int surfaceLabel) 
{
	RepresentationLabel=repLabel;
	SurfaceStyleLabel = surfaceLabel;
}

unsigned int XbimTriangularMeshStreamer::StreamTo(unsigned char* pStream)
{
	unsigned int* iCoord = (unsigned int *)(pStream);

	unsigned int iCPoints =  _points.size(); 
	unsigned int iCNormals =  _normals.size(); 
	unsigned int iCUnique =  _uniquePN.size(); 

	*iCoord++ = iCPoints;
	*iCoord++ = iCNormals; 
	*iCoord++ = iCUnique; 
	*iCoord++ = 0; // to be filled at the end of the function after analysis of the polygons
	*iCoord++ = _polygons.size(); 

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
		unsigned int thisval = itUIPair->PositionIndex;
		if (thisval >= iCPoints)
		{
			int iWhatWrong = 0;
			iWhatWrong++;
		}
		UICoord += (this->*writePoint)(UICoord,thisval);
	}
	for (itUIPair = _uniquePN.begin(); itUIPair != _uniquePN.end(); itUIPair++)  // write normal indices
	{
		UICoord += (this->*writeNormal)(UICoord,itUIPair->NormalIndex);
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
	for (iPol = _polygons.begin(); iPol != _polygons.end(); iPol++)
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
	iCoord = (unsigned int *)(pStream);
	iCoord += 3;
	*iCoord = countTriangles;

	unsigned int calcSize = UICoord - pStream;
	return calcSize;
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

unsigned int XbimTriangularMeshStreamer::StreamSize()
{
	int iPos = _points.size();
	int iNrm = _normals.size();
	int iUPN = _uniquePN.size();
	int iUniqueSize = sizeOptimised(iUPN);
	
	// calc polygon size
	int iPolSize = 0;
	unsigned int iIndex = 0;
	std::list<PolygonInfo>::iterator i;
	for (i =  _polygons.begin(); i != _polygons.end(); i++)
	{
		PolygonInfo pol = *i;
		iPolSize += sizeof(char) + sizeof(int); // one byte for type and one int for count
		iPolSize += pol.IndexCount * iUniqueSize;
	}

	unsigned int iSize = 5 * sizeof(int);       // initial headers
	iSize += iPos * 3 * sizeof(float); // positions
	iSize += iNrm * 3 * sizeof(float); // normals
	iSize += iUPN * (sizeOptimised(iPos) + sizeOptimised(iNrm)); // unique points
	iSize += iPolSize;

	if (iSize == 2846)
	{
		int b = 0;
		b++;
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
	// System::Diagnostics::Debug::Write("Begin polygon: " + type + "\r\n");
	PolygonInfo p;
	p.GLType = type;
	p.IndexCount = 0;

	_polygons.push_back( p);
	_currentPolygonCount = 0;
}

void XbimTriangularMeshStreamer::EndPolygon()
{
	// System::Diagnostics::Debug::Write("End polygon\r\n");
	_polygons.back().IndexCount = _currentPolygonCount;
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
	Float3D f(x,y,z);
	std::pair< std::unordered_map<Float3D,unsigned int>::iterator, bool > pr;
    pr = _normalsMap.insert(Float3DUInt_Pair(f, _normals.size()));
	if(pr.second) //element  not found and  inserted so we add it to the list
		_normals.push_back(f);
	_currentNormalIndex = (pr.first)->second;
}

unsigned int XbimTriangularMeshStreamer::WritePoint(float x, float y, float z)
{
	
	Float3D f(x,y,z);
	std::pair< std::unordered_map<Float3D,unsigned int>::iterator, bool > pr;
    pr = _pointsMap.insert(Float3DUInt_Pair(f, _points.size()));
	if (_useFaceIndexMap)
			_faceIndexMap[_facePointIndex++] = (pr.first)->second;
	if(pr.second) //element not found and inserted so we add it to the list
		_points.push_back( f);
	unsigned int idx =  (pr.first)->second;
	return idx;
}

// when called from OpenCascade converts the 1-based index to of one face to the global 0-based index (_useFaceIndexMap)
// otherwise just add the uniquepoint without the face mapping indirection (0-based to 0-based)
void XbimTriangularMeshStreamer::WriteTriangleIndex(unsigned int index)
{
	_currentPolygonCount++; // used in the closing function of each polygon to write the number of points to the stream
	unsigned int resolvedPoint;
	if (_useFaceIndexMap)
	{
		// System::Diagnostics::Debug::Write("WriteTriangleIndex: " + index + " -> " + _faceIndexMap[index-1] + "\r\n");
		resolvedPoint = this->getUniquePoint(_faceIndexMap[index-1], _currentNormalIndex); // index-1 becasue OCC starts from 1
	}
	else
	{
		// System::Diagnostics::Debug::Write("WriteTriangleIndex: " + index + "\r\n");
		resolvedPoint = this->getUniquePoint(index, _currentNormalIndex); // this call comes from OpenGL; no index - 1
	}
	_indices.push_back(resolvedPoint);
}

unsigned int XbimTriangularMeshStreamer::getUniquePoint(unsigned int pointIndex, unsigned int normalIndex)
{
	// System::Diagnostics::Debug::Write("getUniquePoint: p: " + pointIndex + " n: " + normalIndex +"");
	UIntegerPair f;
	f.PositionIndex = pointIndex;
	f.NormalIndex = normalIndex; 
	std::pair< std::unordered_map<UIntegerPair,unsigned int>::iterator, bool > pr;
    pr = _uniquePNMap.insert(std::pair<UIntegerPair,unsigned int>(f,_uniquePN.size()));
	if(pr.second) //element  not found and  inserted so we add it to the list
		_uniquePN.push_back(f);
	unsigned int idx =  (pr.first)->second;
	return idx;
}
