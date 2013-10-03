#pragma once
#include "XbimPolyhedron.h"
#include <carve/csg.hpp>


namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			ref class XbimCsg
			{
			public:
				//Creates a CSG tree with precision for calculations of vertex equivalence
				XbimCsg(double precision);

			private:
				void InstanceCleanup();
				carve::csg::CSG * _csg;
				~XbimCsg()
				{
					InstanceCleanup();
				}

				!XbimCsg()
				{
					InstanceCleanup();
				}

			public:
				//subtracts polyhedron B from A
				XbimPolyhedron^ Subtract(XbimPolyhedron^ a, XbimPolyhedron^ b);
				//combines all geometry of polyhedron B with A
				XbimPolyhedron^ Combine(XbimPolyhedron^ a, XbimPolyhedron^ b);
				//Unions the geometry of polyhedron B with A
				XbimPolyhedron^ Union(XbimPolyhedron^ a, XbimPolyhedron^ b);
				//in a or b, but not both
			    XbimPolyhedron^ SymetricDifference(XbimPolyhedron^ a, XbimPolyhedron^ b);
				
			};
		}
	}
}

