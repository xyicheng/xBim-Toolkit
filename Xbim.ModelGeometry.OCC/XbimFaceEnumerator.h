#pragma once
#include <TopAbs.hxx> 
#include <TopoDS.hxx> 
#include <TopoDS_Shape.hxx> 
#include <TopExp_Explorer.hxx> 

#include "XbimFace.h"

using namespace Xbim::XbimExtensions::Interfaces;
using namespace System::Collections::Generic;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			public ref class XbimFaceEnumerator : IEnumerator<XbimFace^>
			{
			private:
				TopExp_Explorer * pExplorer;
				bool atStart;

			public:
				XbimFaceEnumerator(const TopoDS_Shape&  solid) 
				{
					pExplorer = new TopExp_Explorer(solid, TopAbs_FACE);
					atStart=true;
				};
				~XbimFaceEnumerator()
				{
					InstanceCleanup();
				}

				!XbimFaceEnumerator()
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

				virtual property XbimFace^ Current
				{
					XbimFace^ get()  
					{
						XbimFace^ face = gcnew XbimFace(TopoDS::Face(pExplorer->Current()));
						return face;
					}
				};
				// This is required as IEnumerator<T> also implements IEnumerator
				virtual property Object^ Current2
				{
					virtual Object^ get()  sealed = System::Collections::IEnumerator::Current::get
					{
						XbimFace^ face = gcnew XbimFace(TopoDS::Face(pExplorer->Current()));
						return face;
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