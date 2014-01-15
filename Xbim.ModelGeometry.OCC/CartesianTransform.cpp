#include "StdAfx.h"
#include "CartesianTransform.h"
using namespace Xbim::Common::Geometry;
using namespace System::Collections::Generic;
using namespace Xbim::Ifc2x3::Extensions;
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
		// Builds a windows Matrix3D from a CartesianTransformationOperator3D
		XbimMatrix3D CartesianTransform::ConvertMatrix3D(IfcCartesianTransformationOperator3D ^ stepTransform)
		{
			XbimVector3D U3; //Z Axis Direction
			XbimVector3D U2; //X Axis Direction
			XbimVector3D U1; //Y axis direction
			if(stepTransform->Axis3!=nullptr)
			{
				IfcDirection^ dir = (IfcDirection^)stepTransform->Axis3;
				U3 = XbimVector3D(dir->DirectionRatios[0],dir->DirectionRatios[1],dir->DirectionRatios[2]); 
				U3.Normalize();
			}
			else
				U3 = XbimVector3D(0.,0.,1.); 
			if(stepTransform->Axis1!=nullptr)
			{
				IfcDirection^ dir = (IfcDirection^)stepTransform->Axis1;
				U1 = XbimVector3D(dir->DirectionRatios[0],dir->DirectionRatios[1],dir->DirectionRatios[2]); 
				U1.Normalize();
			}
			else
			{
				XbimVector3D defXDir(1.,0.,0.);
				if(U3 != defXDir)
					U1 = defXDir;
				else
					U1 = XbimVector3D(0.,1.,0.);
			}
			XbimVector3D xVec = XbimVector3D::Multiply(XbimVector3D::DotProduct(U1,U3),U3);
			XbimVector3D xAxis = XbimVector3D::Subtract(U1,xVec);
			xAxis.Normalize();

			if(stepTransform->Axis2!=nullptr)
			{
				IfcDirection^ dir = (IfcDirection^)stepTransform->Axis2;
				U2 = XbimVector3D(dir->DirectionRatios[0],dir->DirectionRatios[1],dir->DirectionRatios[2]); 
				U2.Normalize();
			}
			else
				U2 = XbimVector3D(0.,1.,0.); 

			XbimVector3D tmp = XbimVector3D::Multiply(XbimVector3D::DotProduct(U2,U3),U3);
			XbimVector3D yAxis = XbimVector3D::Subtract(U2,tmp);
			tmp = XbimVector3D::Multiply(XbimVector3D::DotProduct(U2,xAxis),xAxis);
			yAxis = XbimVector3D::Subtract(yAxis,tmp);
			yAxis.Normalize();
			U2 = yAxis;
			U1 = xAxis;

			XbimPoint3D LO = stepTransform->LocalOrigin->XbimPoint3D(); //local origin
			float S = 1.;
			if(stepTransform->Scale.HasValue)
				S = (float)stepTransform->Scale.Value;

			return XbimMatrix3D (	U1.X, U1.Y, U1.Z, 0,
				U2.X, U2.Y, U2.Z, 0,
				U3.X, U3.Y, U3.Z, 0,
				LO.X, LO.Y, LO.Z , S);
		}

		// Builds a windows Matrix3D from an ObjectPlacement
		XbimMatrix3D CartesianTransform::ConvertMatrix3D(IfcObjectPlacement ^ objPlacement)
		{
			if(dynamic_cast<IfcLocalPlacement^>(objPlacement))
			{
				IfcLocalPlacement^ locPlacement = (IfcLocalPlacement^)objPlacement;
				if (dynamic_cast<IfcAxis2Placement3D^>(locPlacement->RelativePlacement))
				{				
					XbimMatrix3D ucsTowcs = Axis2Placement3DExtensions::ToMatrix3D((IfcAxis2Placement3D^)(locPlacement->RelativePlacement), nullptr);
					if (locPlacement->PlacementRelTo != nullptr)
					{
						return XbimMatrix3D::Multiply(CartesianTransform::ConvertMatrix3D(locPlacement->PlacementRelTo),ucsTowcs);
					}
					else
						return ucsTowcs;

				}
				else //must be 2D
				{
					throw(gcnew System::NotImplementedException("Support for Placements other than 3D not implemented"));
				}

			}
			else //probably a Grid
			{
				throw(gcnew System::NotImplementedException("Support for Placements other than Local not implemented"));
			}

		}
	}
}
}
