#pragma once
#include "XbimFaceBound.h"

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			public ref class XbimFaceOuterBound : XbimFaceBound
			{
			public:
				XbimFaceOuterBound(const TopoDS_Wire & wire, const TopoDS_Face & face): XbimFaceBound(wire, face){};

			};
		}
	}
}