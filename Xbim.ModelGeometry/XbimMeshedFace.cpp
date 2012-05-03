#include "StdAfx.h"
#include "XbimMeshedFace.h"

#include <TShort_Array1OfShortReal.hxx>
#include <TColgp_Array1OfPnt.hxx>
#include <Poly_Array1OfTriangle.hxx>
#include <TopAbs_Orientation.hxx>

namespace Xbim
{
	namespace ModelGeometry
	{
		Vector3DCollection^ XbimMeshedFace::Normals::get()
		{
			if((*pTriangulation).IsNull()) return gcnew Vector3DCollection();
			Poly::ComputeNormals(*pTriangulation);
			Standard_Integer nbNodes = (*pTriangulation)->NbNodes();
			Vector3DCollection^ coll = gcnew Vector3DCollection(nbNodes);
			const TShort_Array1OfShortReal& normals =  (*pTriangulation)->Normals();			
			int nTally = 0;
			for(Standard_Integer nd = 1 ; nd <= nbNodes ; nd++)
			{
				coll->Add(Vector3D(normals.Value(nTally+1),normals.Value(nTally+2),normals.Value(nTally+3)));
				nTally+=3;
			}
			coll->Freeze();
			return coll;
		}

		Point3DCollection^ XbimMeshedFace::Positions::get()
		{
			if((*pTriangulation).IsNull()) return gcnew Point3DCollection();
			Standard_Integer nbNodes = (*pTriangulation)->NbNodes();
			Point3DCollection^ coll = gcnew Point3DCollection(nbNodes);

			const TColgp_Array1OfPnt& points = (*pTriangulation)->Nodes();
			int nTally = 0;

			for(Standard_Integer nd = 1 ; nd <= nbNodes ; nd++)
			{
				gp_XYZ pt = points(nd).Coord();
				pTopLocation->Transformation().Transforms(pt);
				coll->Add(Point3D(pt.X(), pt.Y(), pt.Z()));	
				nTally+=3;
			}
			coll->Freeze();
			return coll;
		}

		Int32Collection^ XbimMeshedFace::TriangleIndices::get()
		{
			if((*pTriangulation).IsNull()) return gcnew Int32Collection();
			bool reversed = pFace->Orientation()==TopAbs_REVERSED;	
			const Poly_Array1OfTriangle& triangles = (*pTriangulation)->Triangles();
			Standard_Integer nbTriangles = 0, n1, n2, n3;
			nbTriangles = (*pTriangulation)->NbTriangles();
			Int32Collection^ coll = gcnew Int32Collection(nbTriangles * 3);
			for(Standard_Integer tr = 1 ; tr <= nbTriangles ; tr++)
			{
				triangles(tr).Get(n1, n2, n3);
				if(reversed)
				{
					coll->Add(n3);
					coll->Add(n2);
					coll->Add(n1);
				}
				else
				{
					coll->Add(n1);
					coll->Add(n2);
					coll->Add(n3);
				}
			}
			coll->Freeze();
			return coll;

		}
	}
}