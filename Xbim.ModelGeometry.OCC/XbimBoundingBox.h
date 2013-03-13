#pragma once
#include "XbimVertexPoint.h"
#include <Bnd_Box.hxx>
#include <gp_Pnt.hxx>
using namespace System;
using namespace System::IO;
using namespace Xbim::Ifc2x3::TopologyResource;
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
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
				XbimBoundingBox(double Xmin, double Ymin, double Zmin, double Xmax, double Ymax,  double Zmax);

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

				array<Byte>^ ToArray()
				{
					array<Byte>^ arr= gcnew array<Byte>(6*sizeof(double));
					if(pBox->IsVoid()) return arr;
					Standard_Real srXmin, srYmin, srZmin, srXmax, srYmax, srZmax;
					pBox->Get(srXmin, srYmin, srZmin, srXmax, srYmax, srZmax);
					MemoryStream^ ms = gcnew MemoryStream(arr);
					BinaryWriter^ bw = gcnew BinaryWriter(ms);
					bw->Write(srXmin);
					bw->Write(srYmin);
					bw->Write(srZmin);
					bw->Write(srXmax);
					bw->Write(srYmax);
					bw->Write(srZmax);
					return arr;
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
}

