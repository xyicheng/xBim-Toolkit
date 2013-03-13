#pragma once
#include "XbimMeshedFace.h"
#include <BRepMesh_IncrementalMesh.hxx>
#include <TopExp_Explorer.hxx> 
#include <TopoDS_Solid.hxx>
#include <TopoDS.hxx>

using namespace System::Collections::Generic;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			ref class XbimMeshedFaceEnumerator:System::Collections::Generic::IEnumerator<XbimMeshedFace^>
			{

			private:
				BRepMesh_IncrementalMesh * pIncrementalMesh; 
				TopoDS_Shape * pSolid;
				TopExp_Explorer* pExplorer;
				bool atStart;
			public:
				XbimMeshedFaceEnumerator(const TopoDS_Shape&  solid, double meshDeflection) 
				{
					pIncrementalMesh = new BRepMesh_IncrementalMesh(solid, meshDeflection);
					pExplorer = new TopExp_Explorer(solid, TopAbs_FACE);
					pSolid = new TopoDS_Shape();
					*pSolid = solid;
					atStart=true;
				};

				~XbimMeshedFaceEnumerator()
				{
					InstanceCleanup();
				}

				!XbimMeshedFaceEnumerator()
				{
					InstanceCleanup();
				}
				void InstanceCleanup()
				{   
					int temp = System::Threading::Interlocked::Exchange((int)(void*)pIncrementalMesh, 0);
					if(temp!=0)
					{
						if (pIncrementalMesh)
						{
							delete pIncrementalMesh;
							pIncrementalMesh=0;
							System::GC::SuppressFinalize(this);
						}
					}
					temp = System::Threading::Interlocked::Exchange((int)(void*)pSolid, 0);
					if(temp!=0)
					{
						if (pSolid)
						{
							delete pSolid;
							pSolid=0;
							System::GC::SuppressFinalize(this);
						}
					}
					temp = System::Threading::Interlocked::Exchange((int)(void*)pExplorer, 0);
					if(temp!=0)
					{
						if (pExplorer)
						{
							delete pExplorer;
							pExplorer=0;
							System::GC::SuppressFinalize(this);
						}
					}

				}

				virtual bool MoveNext(void)
				{
					if(atStart) //skip the first moveNext
					{
						atStart=false; 
						return(pExplorer->More() == Standard_True); 
					}
					pExplorer->Next();
					return(pExplorer->More() == Standard_True);
				}

				virtual property XbimMeshedFace^ Current
				{
					XbimMeshedFace^ get()
					{
						return gcnew XbimMeshedFace(TopoDS::Face(pExplorer->Current()));
					}
				};
				// This is required as IEnumerator<T> also implements IEnumerator
				virtual property Object^ Current2
				{
					virtual Object^ get()  sealed = System::Collections::IEnumerator::Current::get
					{
						return gcnew XbimMeshedFace(TopoDS::Face(pExplorer->Current()));
					}
				};

				virtual void Reset()
				{
					atStart=true;
					pExplorer->ReInit();
				}
			};
		}
	}
}