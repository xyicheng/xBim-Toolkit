#include "StdAfx.h"
#include "XbimBoundingBox.h"
using namespace Xbim::Ifc2x3::Extensions;
#include <msclr/lock.h>

using namespace System;
using namespace System::Threading;
using namespace msclr;
namespace Xbim
{
	namespace ModelGeometry
	{

		XbimBoundingBox::XbimBoundingBox(double Xmin, double Ymin, double Zmin, double Xmax, double Ymax,  double Zmax)
		{	
			pBox = new Bnd_Box();
			pBox->Add(gp_Pnt(Xmin,Ymin,Zmin));
			pBox->Add(gp_Pnt(Xmax,Ymax,Zmax));
		}
	}
}