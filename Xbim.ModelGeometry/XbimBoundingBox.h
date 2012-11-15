#pragma once
#include "XbimVertexPoint.h"
#include <Bnd_Box.hxx>
#include <gp_Pnt.hxx>
using namespace System;


namespace Xbim
{
	namespace ModelGeometry
	{
		using namespace System::Runtime::InteropServices;
		public ref class XbimBoundingBox
		{
		protected:
			Bnd_Box * pBox;
		public:
			XbimBoundingBox(Bnd_Box * box)
			{		
				pBox = box;
			}
			XbimBoundingBox()
			{		
				pBox = new Bnd_Box();
			}
			~XbimBoundingBox()
			{
				InstanceCleanup();
			}

			!XbimBoundingBox()
			{
				InstanceCleanup();
			}
			void InstanceCleanup()
			{   
				int temp = System::Threading::Interlocked::Exchange((int)(void*)pBox, 0);
				if(temp!=0)
				{
					if (pBox)
					{
						delete pBox;
						pBox=0;
						System::GC::SuppressFinalize(this);
					}
				}
			}

			bool Intersects(XbimBoundingBox^ other)
			{
				if(pBox->IsOut(*(other->pBox)))
					return false;
				else
					return true;
			}

			

			void Add(XbimBoundingBox^ other)
			{
				pBox->Add(*(other->pBox));
			}

			void Add(Point3D  point)
			{
				pBox->Add(gp_Pnt(point.X, point.Y, point.Z));
			}

			property double SquareExtent
			{
				double get()
				{
					return (double) pBox->SquareExtent();
				}
			}
			
			bool Is2D()
			{
				return pBox->IsZThin(XbimVertexPoint::Precision) || pBox->IsXThin(XbimVertexPoint::Precision)|| pBox->IsYThin(XbimVertexPoint::Precision);
			}

			

			Rect3D GetRect3D()
			{
				Standard_Real srXmin, srYmin, srZmin, srXmax, srYmax, srZmax;
				if(pBox->IsVoid()) return Rect3D();
				pBox->Get(srXmin, srYmin, srZmin, srXmax, srYmax, srZmax);
				return Rect3D(srXmin, srYmin, srZmin, srXmax-srXmin,  srYmax-srYmin, srZmax-srZmin);
			}

			void  Get( [Out] Double% aXmin, [Out] Double% aYmin,[Out] Double% aZmin,[Out] Double% aXmax,[Out] Double% aYmax,[Out] Double% aZmax)
			{
				Standard_Real srXmin, srYmin, srZmin, srXmax, srYmax, srZmax;
				pBox->Get(srXmin, srYmin, srZmin, srXmax, srYmax, srZmax);
				aXmin=srXmin;
				aYmin=srYmin;
				aZmin=srZmin;
				aXmax=srXmax;
				aYmax=srYmax;
				aZmax=srZmax;

			}
		};
	}
}

