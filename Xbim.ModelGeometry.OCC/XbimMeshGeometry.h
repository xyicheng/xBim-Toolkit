#pragma once
#include "IXbimMeshGeometry.h"
#include <windows.h>
#include <gl/gl.h>
#include <gl/glu.h>



using namespace Xbim::XbimExtensions;
using namespace System::Collections::Generic;
using namespace System::Windows::Media;
using namespace System::Windows::Media::Media3D;
using namespace System::Linq;
using namespace System::IO;
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			[Serializable] 
			public ref class XbimMeshGeometry :IXbimMeshGeometry
			{
			private:

				MemoryStream^ _dataStream;
			protected:
				Point3DCollection^ _points;
				Int32Collection^ _indices;
				Vector3DCollection^ _normals;
				List<IXbimMeshGeometry^>^ _children;
			public:
				void Write(BinaryWriter^ bWriter);
				void Read(BinaryReader^ bReader);
				XbimMeshGeometry(void);
				virtual void BeginTessellate(GLenum type);
				virtual void EndTessellate();
				virtual void TessellateError(GLenum err);
				virtual void AddVertex(IntPtr index);
				virtual void AddPoint(double X, double Y, double Z);
				virtual void AddNormal(double X, double Y, double Z);
				virtual void AddTriangleIndices(int a, int b, int c);
				virtual void AddChild(IXbimMeshGeometry^ child);
				virtual void Clear();
				virtual int PositionCount();
				virtual void Freeze();
				property Point3DCollection^ Positions
				{
					Point3DCollection^ get() { return _points; }
				}

				property Int32Collection^ TriangleIndices
				{
					Int32Collection^ get() { return _indices; }
				}

				property Vector3DCollection^ Normals
				{
					Vector3DCollection ^ get() { return _normals; }
				}

				property IEnumerable<IXbimMeshGeometry^>^ Children
				{
					IEnumerable<IXbimMeshGeometry^>^ get() 
					{
						if(_children == nullptr) 
							return Enumerable::Empty<IXbimMeshGeometry^>(); 
						else 
							return _children; 
					}
				}
				virtual void InitFromStream(Stream^ dataStream);


			};
		}
	}
}