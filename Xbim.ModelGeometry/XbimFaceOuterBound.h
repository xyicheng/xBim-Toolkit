#pragma once
#include "XbimFaceBound.h"

namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimFaceOuterBound : XbimFaceBound
		{
		public:
			XbimFaceOuterBound(const TopoDS_Wire & wire, const TopoDS_Face & face): XbimFaceBound(wire, face){};
			
		};

	}
}