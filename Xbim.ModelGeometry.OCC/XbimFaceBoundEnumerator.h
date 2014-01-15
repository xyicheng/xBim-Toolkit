#pragma once

#include <TopAbs.hxx> 
#include <TopoDS.hxx> 
#include <TopoDS_Shape.hxx> 
#include <TopExp_Explorer.hxx> 
#include <BRepTools.hxx> 

#include "XbimFaceOuterBound.h"
using namespace Xbim::XbimExtensions::Interfaces;

using namespace System::Collections::Generic;
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			public ref class XbimFaceBoundEnumerator: System::Collections::Generic::IEnumerator<XbimFaceBound^>
			{
				TopExp_Explorer * pExplorer;
				TopoDS_Wire * pOuterBound;
				TopoDS_Wire * current;
				TopoDS_Face * pFace;
				bool atStart;

			public:


				XbimFaceBoundEnumerator(const TopoDS_Shape &  face) 
				{
					pFace = new TopoDS_Face();
					*pFace = TopoDS::Face(face);
					pExplorer = new TopExp_Explorer(face, TopAbs_WIRE);
					atStart=true;
					pOuterBound = new TopoDS_Wire();
					*pOuterBound = BRepTools::OuterWire(TopoDS::Face(face));
					current = new TopoDS_Wire();
					*current = *pOuterBound;
				};
				~XbimFaceBoundEnumerator()
				{
					InstanceCleanup();
				}

				!XbimFaceBoundEnumerator()
				{
					InstanceCleanup();
				}
				void InstanceCleanup()
				{   
					IntPtr temp = System::Threading::Interlocked::Exchange(IntPtr(pExplorer), IntPtr(0));
					if(temp!=IntPtr(0))
					{
						if (pExplorer)
						{
							delete pExplorer;
							pExplorer=0;
							delete pOuterBound;
							pOuterBound=0;
							delete current;
							current=0;
							delete pFace;
							pFace=0;

							System::GC::SuppressFinalize(this);
						}
					}

				}

				virtual bool MoveNext(void)
				{
					if(atStart) //skip the first moveNext
						return (pExplorer->More() == Standard_True); 
					pExplorer->Next();
					if(pExplorer->More())
					{
						while(pExplorer->Current().IsEqual(*pOuterBound) ) //skip the outer bound
							pExplorer->Next();
						*current = TopoDS::Wire(pExplorer->Current());
					}
					return (pExplorer->More() == Standard_True);
				}

				virtual property XbimFaceBound^ Current
				{
					XbimFaceBound^ get()
					{
						if(atStart)
						{
							atStart = false;
							return gcnew XbimFaceOuterBound(*current, *pFace);
						}
						else
							return gcnew XbimFaceBound(*current, *pFace);
					}
				};
				// This is required as IEnumerator<T> also implements IEnumerator
				virtual property Object^ Current2
				{
					virtual Object^ get() sealed = System::Collections::IEnumerator::Current::get
					{
						if(atStart)
						{
							return gcnew XbimFaceOuterBound(*current, *pFace);
							atStart = false;
						}
						else
							return gcnew XbimFaceBound(*current, *pFace);
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

