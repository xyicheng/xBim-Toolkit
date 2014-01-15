#pragma once

#include <TopAbs.hxx> 
#include <TopoDS.hxx> 
#include <TopoDS_Shape.hxx> 
#include <BRepTools_WireExplorer.hxx> 
#include <TopoDS_Wire.hxx> 
#include "XbimOrientedEdge.h"

using namespace Xbim::XbimExtensions::Interfaces;
using namespace System::Collections::Generic;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			ref class XbimEdgeLoopEnumerator: IEnumerator<XbimOrientedEdge^>
			{	
				BRepTools_WireExplorer * pExplorer;
				TopoDS_Wire * pWire;
				TopoDS_Face * pFace;
				bool atStart;

			public:

				XbimEdgeLoopEnumerator(const TopoDS_Wire&  wire, const TopoDS_Face&  face) 
				{
					pWire = new TopoDS_Wire();
					*pWire = wire;
					pFace = new TopoDS_Face();
					*pFace = face;
					pExplorer = new BRepTools_WireExplorer(*pWire, *pFace);	
					atStart=true;
				};

				~XbimEdgeLoopEnumerator()
				{
					InstanceCleanup();
				}

				!XbimEdgeLoopEnumerator()
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
							delete pWire;
							pWire=0;
							delete pFace;
							pFace=0;
							System::GC::SuppressFinalize(this);
						}
					}

				}

				virtual bool MoveNext(void)
				{
					if(atStart) //skip the first moveNext
					{
						atStart=false; 
						return pExplorer->More()==Standard_True; 
					}
					pExplorer->Next();
					return pExplorer->More()==Standard_True;
				}

				virtual property XbimOrientedEdge^ Current
				{
					XbimOrientedEdge^ get()
					{

						return gcnew XbimOrientedEdge(pExplorer->Current(), pExplorer->CurrentVertex(), pExplorer->Orientation());
					}
				};
				// This is required as IEnumerator<T> also implements IEnumerator
				virtual property Object^ Current2
				{
					virtual Object^ get() sealed = System::Collections::IEnumerator::Current::get
					{
						return gcnew XbimOrientedEdge(pExplorer->Current(), pExplorer->CurrentVertex(), pExplorer->Orientation());
					}
				};

				virtual void Reset()
				{
					atStart=true;
					pExplorer->Init(*pWire);
				}
			};
		}
	}
}