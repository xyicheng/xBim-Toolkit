#include "StdAfx.h"
#include "XbimMeshGeometry.h"
using namespace System::Runtime::InteropServices;

namespace Xbim
{
	namespace ModelGeometry
	{
		
		void XbimMeshGeometry::Write(BinaryWriter^ bWriter)
		{

			
		/*	bWriter->Write(_points->Count);
			for each( Point3D p in _points)
			{
				bWriter->Write(p.X);
				bWriter->Write(p.Y);
				bWriter->Write(p.Z);
			}
			bWriter->Write(_indices->Count);
			for each( Int32 i in _indices)
				bWriter->Write(i);
			bWriter->Write(_normals->Count);
			for each( Vector3D v in _normals)
			{
				bWriter->Write(v.X);
				bWriter->Write(v.Y);
				bWriter->Write(v.Z);
			}
			if(_children == nullptr) 
				bWriter->Write((Int32) 0);
			else
			{
				bWriter->Write(_children->Count);
				for each( XbimMeshGeometry^ child in _children)
					child->Write(bWriter);
			}*/
			if(_dataStream!=nullptr) //it may have no geometry only children
			{
				_dataStream->Seek(0, SeekOrigin::Begin);
				int len = Convert::ToInt32(_dataStream->Length);
				bWriter->Write(len);
				bWriter->Write(_dataStream->GetBuffer(), 0 , len);
			}
			else
				bWriter->Write((Int32) 0);
			if(_children == nullptr) 
				bWriter->Write((Int32) 0);
			else
			{
				bWriter->Write(_children->Count);
				for each( XbimMeshGeometry^ child in _children)
					child->Write(bWriter);
			}
		}

		void XbimMeshGeometry::Read(BinaryReader^ bReader)
		{
			int len = bReader->ReadInt32();
			if(len > 0)
			{
				_dataStream = gcnew MemoryStream(len);
				_dataStream->Write(bReader->ReadBytes(len),0,len);
			}
			else
				_dataStream=nullptr;
			
			int childCount = bReader->ReadInt32();
			if(childCount>0)
				_children = gcnew List<IXbimMeshGeometry^>(childCount);
			for(int i=0;i<childCount;i++)
			{
				XbimMeshGeometry^ child = gcnew XbimMeshGeometry();
				child->Read(bReader);
				AddChild(child);
			}
			//_points = gcnew Point3DCollection();
			//_indices = gcnew Int32Collection();
			//_normals = gcnew Vector3DCollection();
			//_children = nullptr;
			////read vertices
			//int pointCount = bReader->ReadInt32();
			//for( int i=0;i<pointCount;i++)
			//{
			//	double x,y,z;
			//	x=bReader->ReadDouble();
			//	y=bReader->ReadDouble();
			//	z=bReader->ReadDouble();
			//	_points->Add(Point3D(x,y,z));
			//}
			////read indices
			//int idxCount = bReader->ReadInt32();
			//for( int i=0;i<idxCount;i++)
			//	_indices->Add(bReader->ReadInt32());
			////read normals
			//int normCount = bReader->ReadInt32();
			//for( int i=0;i<normCount;i++)
			//{
			//	double x,y,z;
			//	x=bReader->ReadDouble();
			//	y=bReader->ReadDouble();
			//	z=bReader->ReadDouble();
			//	_normals->Add(Vector3D(x,y,z));
			//}
			//int childCount = bReader->ReadInt32();
			//if(childCount>0)
			//	_children = gcnew List<IXbimMeshGeometry^>(childCount);
			//for(int i=0;i<childCount;i++)
			//{
			//	XbimMeshGeometry^ child = gcnew XbimMeshGeometry();
			//	child->Read(bReader);
			//	AddChild(child);
			//}
		
			if(_dataStream!=nullptr)
			{
			_dataStream->Seek(0, SeekOrigin::Begin);
			BinaryReader^ br = gcnew BinaryReader(_dataStream);
			
			
			unsigned int numPoints = br->ReadUInt32();
			if(numPoints==0) return;
			int indexSize;
			if(numPoints<=0xFF) //we will use byte for indices
				indexSize =sizeof(unsigned char) ;
			else if(numPoints<=0xFFFF) 
				indexSize = sizeof(unsigned short); //use  unsigned short int for indices
			else
				indexSize = sizeof(unsigned int); //use unsigned int for indices
			Clear();
			List<Point3D>^ points = gcnew List<Point3D>(numPoints);

			for(unsigned int i=0;i<numPoints;i++)
			{
				double x = br->ReadDouble();
				double y = br->ReadDouble();
				double z = br->ReadDouble();
				points->Add(Point3D(x,y,z));
			}

			unsigned short numFaces = br->ReadUInt32();

			for(unsigned short f=0;f<numFaces;f++)
			{
				//get the number of polygons
				unsigned short numPolygons = br->ReadUInt16();
				//get the normals
				unsigned short numNormals = br->ReadUInt16();
				for(unsigned short n=0;n<numNormals;n++)
				{
					//get the face normal
					double x = br->ReadDouble();
					double y = br->ReadDouble();
					double z = br->ReadDouble();
					Vector3D normal = Vector3D(x,y,z);
				}
				for(unsigned int p=0;p<numPolygons;p++)
				{
					//set the state
					int meshType = br->ReadByte();
					int pointTally=0;
					int previousToLastIndex=0;
					int lastIndex = 0;
					int fanStartIndex=0;
					unsigned int indicesCount = br->ReadUInt16();

					//get the triangles
					for(unsigned int i=0;i<indicesCount;i++)
					{
						int index;
						switch(indexSize)
						{
						case sizeof(unsigned char):
							index = br->ReadByte();
							break;
						case sizeof(unsigned short):
							index = br->ReadUInt16();
							break;
						default:
							index = br->ReadUInt32();
							break;
						}

						if(pointTally==0)
							fanStartIndex=index;
						if(pointTally  < 3) //first time
						{
							_indices->Add(_points->Count);
							_points->Add(points[index]);
						}
						else 
						{

							switch(meshType)
							{

							case GL_TRIANGLES://      0x0004
								_indices->Add(_points->Count);
								_points->Add(points[index]);
								break;
							case GL_TRIANGLE_STRIP:// 0x0005
								if(pointTally % 2 ==0)
								{
									_indices->Add(_points->Count);
									_points->Add(points[previousToLastIndex]);
									_indices->Add(_points->Count);
									_points->Add(points[lastIndex]);
								}
								else
								{
									_indices->Add(_points->Count);
									_points->Add(points[lastIndex]);
									_indices->Add(_points->Count);
									_points->Add(points[previousToLastIndex]);
								} 
								_indices->Add(_points->Count);
								_points->Add(points[index]);

								break;
							case GL_TRIANGLE_FAN://   0x0006

								_indices->Add(_points->Count);
								_points->Add(points[fanStartIndex]);
								_indices->Add(_points->Count);
								_points->Add(points[lastIndex]);
								_indices->Add(_points->Count);
								_points->Add(points[index]);
								break;
							default:
								break;
							}
						}
						previousToLastIndex = lastIndex;
						lastIndex = index;
						pointTally++;
					}
				}
			}
		}

			
		}

		XbimMeshGeometry::XbimMeshGeometry(void)
		{
			_points = gcnew Point3DCollection();
			_indices = gcnew Int32Collection();
			_normals = gcnew Vector3DCollection();

		}

		void XbimMeshGeometry::BeginTessellate(GLenum type)
		{
			
		}

		void XbimMeshGeometry::EndTessellate()
		{		

		}
		
		void XbimMeshGeometry::Freeze()
		{
			_points->Freeze();
			_indices->Freeze();
			_normals->Freeze();
			for each(XbimMeshGeometry^ child in Children)
				child->Freeze();

		}
		void XbimMeshGeometry::TessellateError(GLenum err)
		{
		}

		void XbimMeshGeometry::Clear()
		{
			
			_children = nullptr;
		}

		void XbimMeshGeometry::AddChild(IXbimMeshGeometry^ child)
		{
			if(_children == nullptr) _children = gcnew List<IXbimMeshGeometry^>();
			_children->Add(child);
		}

		void XbimMeshGeometry::AddPoint(double X, double Y, double Z)
		{
			_points->Add(Point3D(X,Y,Z));
		}

		void XbimMeshGeometry::AddNormal(double X, double Y, double Z)
		{
			_normals->Add(Vector3D(X,Y,Z));
		}

		void XbimMeshGeometry::AddTriangleIndices(int a, int b, int c)
		{
			_indices->Add(a);
			_indices->Add(b);
			_indices->Add(c);
		}

		int XbimMeshGeometry::PositionCount()
		{
			return _points->Count;
		}




		void XbimMeshGeometry::AddVertex(IntPtr vIdx)
		{

		
		}

	

		void XbimMeshGeometry::InitFromStream(Stream^ dataStream)
		{
			//store the stream away as a copy
			dataStream->Seek(0, SeekOrigin::Begin);
			_dataStream = gcnew MemoryStream((int)dataStream->Length);
			dataStream->CopyTo(_dataStream);
			_dataStream->Seek(0, SeekOrigin::Begin);
	
			BinaryReader^ br = gcnew BinaryReader(_dataStream);
			
			
			unsigned int numPoints = br->ReadUInt32();
			if(numPoints==0) return;
			int indexSize;
			if(numPoints<=0xFF) //we will use byte for indices
				indexSize =sizeof(unsigned char) ;
			else if(numPoints<=0xFFFF) 
				indexSize = sizeof(unsigned short); //use  unsigned short int for indices
			else
				indexSize = sizeof(unsigned int); //use unsigned int for indices
			Clear();
			List<Point3D>^ points = gcnew List<Point3D>(numPoints);

			for(unsigned int i=0;i<numPoints;i++)
			{
				double x = br->ReadDouble();
				double y = br->ReadDouble();
				double z = br->ReadDouble();
				points->Add(Point3D(x,y,z));
			}

			unsigned short numFaces = br->ReadUInt32();

			for(unsigned short f=0;f<numFaces;f++)
			{
				//get the number of polygons
				unsigned short numPolygons = br->ReadUInt16();
				//get the normals
				unsigned short numNormals = br->ReadUInt16();
				for(unsigned short n=0;n<numNormals;n++)
				{
					//get the face normal
					double x = br->ReadDouble();
					double y = br->ReadDouble();
					double z = br->ReadDouble();
					Vector3D normal = Vector3D(x,y,z);
				}
				for(unsigned int p=0;p<numPolygons;p++)
				{
					//set the state
					int meshType = br->ReadByte();
					int pointTally=0;
					int previousToLastIndex=0;
					int lastIndex = 0;
					int fanStartIndex=0;
					unsigned int indicesCount = br->ReadUInt16();

					//get the triangles
					for(unsigned int i=0;i<indicesCount;i++)
					{
						int index;
						switch(indexSize)
						{
						case sizeof(unsigned char):
							index = br->ReadByte();
							break;
						case sizeof(unsigned short):
							index = br->ReadUInt16();
							break;
						default:
							index = br->ReadUInt32();
							break;
						}

						if(pointTally==0)
							fanStartIndex=index;
						if(pointTally  < 3) //first time
						{
							_indices->Add(_points->Count);
							_points->Add(points[index]);
						}
						else 
						{

							switch(meshType)
							{

							case GL_TRIANGLES://      0x0004
								_indices->Add(_points->Count);
								_points->Add(points[index]);
								break;
							case GL_TRIANGLE_STRIP:// 0x0005
								if(pointTally % 2 ==0)
								{
									_indices->Add(_points->Count);
									_points->Add(points[previousToLastIndex]);
									_indices->Add(_points->Count);
									_points->Add(points[lastIndex]);
								}
								else
								{
									_indices->Add(_points->Count);
									_points->Add(points[lastIndex]);
									_indices->Add(_points->Count);
									_points->Add(points[previousToLastIndex]);
								} 
								_indices->Add(_points->Count);
								_points->Add(points[index]);

								break;
							case GL_TRIANGLE_FAN://   0x0006

								_indices->Add(_points->Count);
								_points->Add(points[fanStartIndex]);
								_indices->Add(_points->Count);
								_points->Add(points[lastIndex]);
								_indices->Add(_points->Count);
								_points->Add(points[index]);
								break;
							default:
								break;
							}
						}
						previousToLastIndex = lastIndex;
						lastIndex = index;
						pointTally++;
					}
				}
			}
		}
		
	}
}