#pragma once
#include "stdafx.h"
#include "shapefile.h"
#include <vcclr.h>
#include <msclr/marshal.h>
using namespace msclr::interop;
using namespace Xbim::Shapefile;

void swapPointStacks(Point3DCollection^% points, Point3DCollection^% points_new)
{
	Point3DCollection^ pointsFoo;
	pointsFoo = points;
	points = points_new;
	points_new = pointsFoo;
}

void SHPObjectData::BeginTessellate(GLenum type)
{
	//this->lastTesselation = this->actualTesselation;
	//this->actualTesselation = numPoints;	
}

void SHPObjectData::EndTessellate()
{
}

void SHPObjectData::TessellateError(GLenum errNum)
{
	throw gcnew Exception("Tesselate error");
}

void SHPObjectData::AddVertex(IntPtr index)
{
	throw gcnew Exception("Method AddVertex() was called but not implemented.");
}

void SHPObjectData::AddPoint(double X, double Y, double Z)
{
	this->points->Add(Point3D(X, Y, Z));
	this->numPoints++;
}

void SHPObjectData::AddNormal(double X, double Y, double Z)
{
	//do nothing, because normals are not contained in the shapefile geometry model
}

void SHPObjectData::AddTriangleIndices(int a, int b, int c)
{
	this->addPart(PartType::TriangleFan);

	Point3D p1;
	Point3D p2; 
	Point3D p3; 
	//int lastTes = this->lastTesselation;

	if (matrix3d != nullptr)
	{
		p1 = matrix3d->Transform(points[a]);
		p2 = matrix3d->Transform(points[b]);
		p3 = matrix3d->Transform(points[c]);
	}
	else
	{
		p1 = points[a];
		p2 = points[b];
		p3 = points[c];
	}

	this->addPointToSHP(p1.X, p1.Y, p1.Z);
	this->addPointToSHP(p2.X, p2.Y, p2.Z);
	this->addPointToSHP(p3.X, p3.Y, p3.Z);
}

//if the mesher use this function, it insertes "XbimMeshGeometry" objects. It is parsed here to the SHPObjectData
void SHPObjectData::AddChild(IXbimMeshGeometry^ xchild)
{
	XbimMeshGeometry^ child = (XbimMeshGeometry^)xchild;
	for (int i = 0; i < child->TriangleIndices->Count; i+=3)
	{
		this->addPart(PartType::TriangleFan);

		Point3D p1;
		Point3D p2; 
		Point3D p3; 
		//int lastTes = this->lastTesselation;

		//indexes of points:
		int ind_1 = child->TriangleIndices[i];
		int ind_2 = child->TriangleIndices[i+1];
		int ind_3 = child->TriangleIndices[i+2];

		if (matrix3d != nullptr)
		{
			p1 = matrix3d->Transform(child->Positions[ind_1]);
			p2 = matrix3d->Transform(child->Positions[ind_2]);
			p3 = matrix3d->Transform(child->Positions[ind_3]);
		}
		else
		{
			p1 = child->Positions[ind_1];
			p2 = child->Positions[ind_2];
			p3 = child->Positions[ind_3];
		}

		this->addPointToSHP(p1.X, p1.Y, p1.Z);
		this->addPointToSHP(p2.X, p2.Y, p2.Z);
		this->addPointToSHP(p3.X, p3.Y, p3.Z);
	}
	if (child->Children!=nullptr)
	{
		for each(IXbimMeshGeometry^ child_2 in child->Children)
		{
			this->AddChild(child_2);
		}
	}
}

void SHPObjectData::Clear()
{
	this->points->Clear();
	this->numPoints=0;
}

int SHPObjectData::PositionCount()
{
	return this->nVertices;
}

void SHPObjectData::Freeze()
{
	this->Clear();
}

void SHPObjectData::setTransformMatrix(Matrix3D^ matrix3d)
{
	this->matrix3d = matrix3d;
}

void SHPObjectData::InitFromStream(Stream^ dataStream)
{
	////store the stream away as a copy
	//	dataStream->Seek(0, SeekOrigin::Begin);
	//	_dataStream = gcnew MemoryStream((int)dataStream->Length);
	//	dataStream->CopyTo(_dataStream);
	//	_dataStream->Seek(0, SeekOrigin::Begin);

	//	BinaryReader^ br = gcnew BinaryReader(_dataStream);
	//	
	//	
	//	unsigned int numPoints = br->ReadUInt32();
	//	if(numPoints==0) return;
	//	int indexSize;
	//	if(numPoints<=0xFF) //we will use byte for indices
	//		indexSize =sizeof(unsigned char) ;
	//	else if(numPoints<=0xFFFF) 
	//		indexSize = sizeof(unsigned short); //use  unsigned short int for indices
	//	else
	//		indexSize = sizeof(unsigned int); //use unsigned int for indices
	//	Clear();
	//	List<Point3D>^ points = gcnew List<Point3D>(numPoints);

	//	for(unsigned int i=0;i<numPoints;i++)
	//	{
	//		double x = br->ReadDouble();
	//		double y = br->ReadDouble();
	//		double z = br->ReadDouble();
	//		points->Add(Point3D(x,y,z));
	//	}

	//	unsigned short numFaces = br->ReadUInt32();

	//	for(unsigned short f=0;f<numFaces;f++)
	//	{
	//		//get the number of polygons
	//		unsigned short numPolygons = br->ReadUInt16();
	//		//get the normals
	//		unsigned short numNormals = br->ReadUInt16();
	//		for(unsigned short n=0;n<numNormals;n++)
	//		{
	//			//get the face normal
	//			double x = br->ReadDouble();
	//			double y = br->ReadDouble();
	//			double z = br->ReadDouble();
	//			Vector3D normal = Vector3D(x,y,z);
	//		}
	//		for(unsigned int p=0;p<numPolygons;p++)
	//		{
	//			//set the state
	//			int meshType = br->ReadByte();
	//			int pointTally=0;
	//			int previousToLastIndex=0;
	//			int lastIndex = 0;
	//			int fanStartIndex=0;
	//			unsigned int indicesCount = br->ReadUInt16();

	//			//get the triangles
	//			for(unsigned int i=0;i<indicesCount;i++)
	//			{
	//				int index;
	//				switch(indexSize)
	//				{
	//				case sizeof(unsigned char):
	//					index = br->ReadByte();
	//					break;
	//				case sizeof(unsigned short):
	//					index = br->ReadUInt16();
	//					break;
	//				default:
	//					index = br->ReadUInt32();
	//					break;
	//				}

	//				if(pointTally==0)
	//					fanStartIndex=index;
	//				if(pointTally  < 3) //first time
	//				{
	//					_indices->Add(_points->Count);
	//					_points->Add(points[index]);
	//				}
	//				else 
	//				{

	//					switch(meshType)
	//					{

	//					case GL_TRIANGLES://      0x0004
	//						_indices->Add(_points->Count);
	//						_points->Add(points[index]);
	//						break;
	//					case GL_TRIANGLE_STRIP:// 0x0005
	//						if(pointTally % 2 ==0)
	//						{
	//							_indices->Add(_points->Count);
	//							_points->Add(points[previousToLastIndex]);
	//							_indices->Add(_points->Count);
	//							_points->Add(points[lastIndex]);
	//						}
	//						else
	//						{
	//							_indices->Add(_points->Count);
	//							_points->Add(points[lastIndex]);
	//							_indices->Add(_points->Count);
	//							_points->Add(points[previousToLastIndex]);
	//						} 
	//						_indices->Add(_points->Count);
	//						_points->Add(points[index]);

	//						break;
	//					case GL_TRIANGLE_FAN://   0x0006

	//						_indices->Add(_points->Count);
	//						_points->Add(points[fanStartIndex]);
	//						_indices->Add(_points->Count);
	//						_points->Add(points[lastIndex]);
	//						_indices->Add(_points->Count);
	//						_points->Add(points[index]);
	//						break;
	//					default:
	//						break;
	//					}
	//				}
	//				previousToLastIndex = lastIndex;
	//				lastIndex = index;
	//				pointTally++;
	//			}
	//		}
	//}
	//	}
	}

