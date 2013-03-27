#pragma once
#include <TopLoc_Location.hxx> 
#include <TopoDS_Face.hxx> 
#include <BRep_Tool.hxx>
#include <Poly.hxx>
#include <Handle_Poly_Triangulation.hxx>
#include <Poly_Triangulation.hxx>

using namespace System::Windows::Media::Media3D;
using namespace System::Windows::Media;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			public ref class XbimMeshedFace
			{
			private:
				TopoDS_Face * pFace;
				TopLoc_Location * pTopLocation;
				Handle_Poly_Triangulation * pTriangulation;
			public:
				XbimMeshedFace(const TopoDS_Face& face)
				{
					pFace = new TopoDS_Face();
					*pFace = face;
					pTopLocation = new TopLoc_Location();
					pTriangulation = new Handle_Poly_Triangulation();
					*pTriangulation = BRep_Tool::Triangulation(*pFace,*pTopLocation);

				}
				~XbimMeshedFace()
				{
					InstanceCleanup();
				}

				!XbimMeshedFace()
				{
					InstanceCleanup();
				}
				void InstanceCleanup()
				{   
					int temp = System::Threading::Interlocked::Exchange((int)(void*)pFace, 0);
					if(temp!=0)
					{
						if (pFace)
						{
							delete pFace;
							pFace=0;
							System::GC::SuppressFinalize(this);
						}
					}
					temp = System::Threading::Interlocked::Exchange((int)(void*)pTopLocation, 0);
					if(temp!=0)
					{
						if (pTopLocation)
						{
							delete pTopLocation;
							pTopLocation=0;
							System::GC::SuppressFinalize(this);
						}
					}
					temp = System::Threading::Interlocked::Exchange((int)(void*)pTriangulation, 0);
					if(temp!=0)
					{
						if (pTriangulation)
						{
							delete pTriangulation;
							pTriangulation=0;
							System::GC::SuppressFinalize(this);
						}
					}
				}

				property Vector3DCollection^ Normals
				{
					Vector3DCollection^ get();
				}


				property Int32Collection^ TriangleIndices
				{
					Int32Collection^ get();
				}

				property Point3DCollection^ Positions
				{
					Point3DCollection^ get();
				}

			};
		}
	}
}
