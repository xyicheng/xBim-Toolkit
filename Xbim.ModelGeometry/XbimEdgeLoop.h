#pragma once
#include <TopoDS_Wire.hxx>
#include <TopoDS_Face.hxx>
#include "XbimEdgeLoopEnumerator.h"

using namespace Xbim::XbimExtensions::Interfaces;
using namespace System::Collections::Generic;

namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimEdgeLoop:  IEnumerable<XbimOrientedEdge^>
		{
		private:
			TopoDS_Wire * pWire;
			TopoDS_Face * pFace;
		public:
			XbimEdgeLoop(const TopoDS_Wire & wire, const TopoDS_Face & face);
			~XbimEdgeLoop()
			{
				InstanceCleanup();
			}

			!XbimEdgeLoop()
			{
				InstanceCleanup();
			}
			void InstanceCleanup()
			{   
				int temp = System::Threading::Interlocked::Exchange((int)(void*)pWire, 0);
				if(temp!=0)
				{
					if (pWire)
					{
						delete pWire;
						pWire=0;
						delete pFace;
						pFace=0;
						System::GC::SuppressFinalize(this);
					}
				}

			}
			/*Interface*/
			virtual property IEnumerable<XbimOrientedEdge^>^ EdgeList
			{
				IEnumerable<XbimOrientedEdge^>^ get();
			}

			// IEnumerable<IIfcOrientedEdge^> Members

			virtual IEnumerator<XbimOrientedEdge^>^ GetEnumerator()
			{

				return gcnew XbimEdgeLoopEnumerator(*pWire, *pFace);
			}


			// IEnumerable Members

			virtual System::Collections::IEnumerator^ GetEnumerator2() sealed = System::Collections::IEnumerable::GetEnumerator
			{ 
				return gcnew XbimEdgeLoopEnumerator(*pWire, *pFace);
			}
			void Print();
		};

	}
}