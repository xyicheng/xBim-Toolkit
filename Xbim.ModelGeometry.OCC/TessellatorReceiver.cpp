#include "StdAfx.h"
#include "TessellatorReceiver.h"
using namespace System::Runtime::InteropServices;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			TessellatorReceiver::TessellatorReceiver(void)
			{
				_points = gcnew List<Point3D>();
				_indices = gcnew List<Int32>();
				_normals = gcnew List<Vector3D>();

			}

			void TessellatorReceiver::BeginTessellate(GLenum type)
			{
				_meshType = type;
				_pointTally=0;
				_previousToLastIndex=0;
				_lastIndex = 0;
				_fanStartIndex=0;
			}

			void TessellatorReceiver::EndTessellate()
			{

			}

			void TessellatorReceiver::TessellateError(GLenum err)
			{
				Logger->WarnFormat("Error in TesselatorReceivor : errorcode {0}", err);
			}

			void TessellatorReceiver::Clear()
			{
				_points->Clear();
				_indices->Clear();
				_normals->Clear();
				_pointTally=0;
				_previousToLastIndex=0;
				_lastIndex = 0;
				_fanStartIndex=0;
			}

			void TessellatorReceiver::AddPoint(double X, double Y, double Z)
			{
				_points->Add(Point3D(X,Y,Z));
			}

			void TessellatorReceiver::AddNormal(double X, double Y, double Z)
			{
				_normals->Add(Vector3D(X,Y,Z));
			}

			void TessellatorReceiver::AddTriangleIndices(int a, int b, int c)
			{
				_indices->Add(a);
				_indices->Add(b);
				_indices->Add(c);
			}

			int TessellatorReceiver::PositionCount()
			{
				return _points->Count;
			}

			void TessellatorReceiver::AddVertex(IntPtr vIdx)
			{

				int index = vIdx.ToInt32();
				if(_pointTally==0)
					_fanStartIndex=index;
				if(_pointTally  < 3) //first time
				{
					_indices->Add(index);

				}
				else 
				{

					switch(_meshType)
					{

					case GL_TRIANGLES://      0x0004
						_indices->Add(index);
						break;
					case GL_TRIANGLE_STRIP:// 0x0005
						if(_pointTally % 2 ==0)
						{
							_indices->Add(_previousToLastIndex);
							_indices->Add(_lastIndex);
						}
						else
						{
							_indices->Add(_lastIndex);
							_indices->Add(_previousToLastIndex);
						} 
						_indices->Add(index);
						break;
					case GL_TRIANGLE_FAN://   0x0006

						_indices->Add(_fanStartIndex);
						_indices->Add(_lastIndex);
						_indices->Add(index);
						break;

					}
				}
				_previousToLastIndex = _lastIndex;
				_lastIndex = index;
				_pointTally++;
			}
		}
	}
}




