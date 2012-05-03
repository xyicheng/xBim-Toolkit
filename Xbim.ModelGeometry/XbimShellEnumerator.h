#pragma once
#include "XbimShell.h"

#include <TopoDS.hxx>
#include <TopExp_Explorer.hxx>
#include <TopoDS_Compound.hxx>


using namespace System::Collections::Generic;
using namespace Xbim::XbimExtensions::Interfaces;

namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimShellEnumerator : IEnumerator<XbimShell^>
		{
		private:
			TopExp_Explorer * pExplorer;
			bool atStart;

		public:
			XbimShellEnumerator(const TopoDS_Compound&  compound) 
			{
				pExplorer = new TopExp_Explorer(compound, TopAbs_SHELL);
				atStart=true;
			};
			~XbimShellEnumerator()
			{
				InstanceCleanup();
			}

			!XbimShellEnumerator()
			{
				InstanceCleanup();
			}
			void InstanceCleanup()
			{   
				int temp = System::Threading::Interlocked::Exchange((int)(void*)pExplorer, 0);
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

			virtual property XbimShell^ Current
			{
				XbimShell^ get()
				{
					XbimShell^ shell = gcnew XbimShell(TopoDS::Shell(pExplorer->Current()));
					return shell;
				}
			};
			// This is required as IEnumerator<T> also implements IEnumerator
			virtual property Object^ Current2
			{
				virtual Object^ get()  sealed = System::Collections::IEnumerator::Current::get
				{
					XbimShell^ shell = gcnew XbimShell(TopoDS::Shell(pExplorer->Current()));
					return shell;
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