#pragma once

#include <TopLoc_Location.hxx>
#include <gp_Trsf.hxx>


namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimLocation
		{
			TopLoc_Location * pLocation;
		public:
			XbimLocation(const TopLoc_Location & location)
			{
				pLocation = new TopLoc_Location();
				*pLocation = location;
			}

			~XbimLocation()
			{
				InstanceCleanup();
			}

			!XbimLocation()
			{
				InstanceCleanup();
			}
			void InstanceCleanup()
			{   
				int temp = System::Threading::Interlocked::Exchange((int)(void*)pLocation, 0);
				if(temp!=0)
				{
					if (pLocation)
					{
						delete pLocation;
						pLocation=0;
						System::GC::SuppressFinalize(this);
					}
				}
			}

			virtual property TopLoc_Location* Handle
			{
				TopLoc_Location* get(){return pLocation;};			
			}

			void Print()
			{
				gp_Trsf m = pLocation->Transformation();
				for(int r = 1;r<=3;r++)
				{
					for(int c = 1;c<=4;c++)
					{
						System::Diagnostics::Debug::Write(m.Value(r,c));
						System::Diagnostics::Debug::Write(", ");
					}
					System::Diagnostics::Debug::WriteLine("");
				}


			}

			XbimLocation^ Inverted()
			{
				return gcnew XbimLocation(pLocation->Inverted());
			}
		};
	}
}

