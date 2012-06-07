#pragma once
#include "ITessellatorReceiver.h"
#include <windows.h>
#include <gl/gl.h>
#include <gl/glu.h>
using namespace System::Windows::Media;
using namespace System::Windows::Media::Media3D;
using namespace Xbim::XbimExtensions;
using namespace System::Collections::Generic;
using namespace Xbim::Common::Logging;

namespace Xbim
{
	namespace ModelGeometry
	{
		[Serializable] 
		public ref class TessellatorReceiver abstract:ITessellatorReceiver
		{
		private:
			GLenum _meshType;
			int _pointTally;
			int _lastIndex;
			int _previousToLastIndex;
			int _fanStartIndex;
			Stream^ _dataStream;
			static ILogger^ Logger = LoggerFactory::GetLogger();
		protected:
			List<Point3D>^ _points;
			List<Int32>^ _indices;
			List<Vector3D>^ _normals;
		public:

			TessellatorReceiver(void);
			virtual void BeginTessellate(GLenum type);
			virtual void EndTessellate();
			virtual void TessellateError(GLenum err);
			virtual void AddVertex(IntPtr index);
			virtual void AddPoint(double X, double Y, double Z);
			virtual void AddNormal(double X, double Y, double Z);
			virtual void AddTriangleIndices(int a, int b, int c);
			virtual void Clear();
			virtual int PositionCount();
			virtual property Stream^ DataStream
			{
				virtual Stream^ get()
				{
					return _dataStream;
				}
				virtual void set(Stream^ strm)
				{
					_dataStream = strm;
				}
			}
		};
	}
}