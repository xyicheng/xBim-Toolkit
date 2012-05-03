#include "StdAfx.h"
#include "CartesianTransform.h"
namespace Xbim
{
	namespace ModelGeometry
	{

		// Builds a windows Matrix3D from a CartesianTransformationOperator3D
		Matrix3D CartesianTransform::ConvertMatrix3D(IfcCartesianTransformationOperator3D ^ stepTransform)
		{
			Vector3D U3; //Z Axis Direction
			Vector3D U2; //X Axis Direction
			Vector3D U1; //Y axis direction
			if(stepTransform->Axis3!=nullptr)
			{
				IfcDirection% dir = (IfcDirection%)stepTransform->Axis3;
				U3 = Vector3D(dir.DirectionRatios[0],dir.DirectionRatios[1],dir.DirectionRatios[2]); 
				U3.Normalize();
			}
			else
				U3 = Vector3D(0.,0.,1.); 
			if(stepTransform->Axis1!=nullptr)
			{
				IfcDirection% dir = (IfcDirection%)stepTransform->Axis1;
				U1 = Vector3D(dir.DirectionRatios[0],dir.DirectionRatios[1],dir.DirectionRatios[2]); 
				U1.Normalize();
			}
			else
			{
				Vector3D defXDir(1.,0.,0.);
				if(U3 != defXDir)
					U1 = defXDir;
				else
					U1 = Vector3D(0.,1.,0.);
			}
			Vector3D xVec = Vector3D::Multiply(Vector3D::DotProduct(U1,U3),U3);
			Vector3D xAxis = Vector3D::Subtract(U1,xVec);
			xAxis.Normalize();

			if(stepTransform->Axis2!=nullptr)
			{
				IfcDirection% dir = (IfcDirection%)stepTransform->Axis2;
				U2 = Vector3D(dir.DirectionRatios[0],dir.DirectionRatios[1],dir.DirectionRatios[2]); 
				U2.Normalize();
			}
			else
				U2 = Vector3D(0.,1.,0.); 

			Vector3D tmp = Vector3D::Multiply(Vector3D::DotProduct(U2,U3),U3);
			Vector3D yAxis = Vector3D::Subtract(U2,tmp);
			tmp = Vector3D::Multiply(Vector3D::DotProduct(U2,xAxis),xAxis);
			yAxis = Vector3D::Subtract(yAxis,tmp);
			yAxis.Normalize();
			U2 = yAxis;
			U1 = xAxis;

			Point3D% LO = stepTransform->LocalOrigin->WPoint3D(); //local origin
			double S = 1.;
			if(stepTransform->Scale.HasValue)
				S = stepTransform->Scale.Value;

			return Matrix3D  (	U1.X, U1.Y, U1.Z, 0,
				U2.X, U2.Y, U2.Z, 0,
				U3.X, U3.Y, U3.Z, 0,
				LO.X, LO.Y, LO.Z , S);
		}

		// Builds a windows Matrix3D from an ObjectPlacement
		Matrix3D CartesianTransform::ConvertMatrix3D(IfcObjectPlacement ^ objPlacement)
		{
			if(dynamic_cast<IfcLocalPlacement^>(objPlacement))
			{
				IfcLocalPlacement% locPlacement = (IfcLocalPlacement%)objPlacement;
				if (dynamic_cast<IfcAxis2Placement3D^>(locPlacement.RelativePlacement))
				{
					IfcAxis2Placement3D% axis3D = (IfcAxis2Placement3D%)locPlacement.RelativePlacement;
					Vector3D ucsXAxis(axis3D.RefDirection->DirectionRatios[0], axis3D.RefDirection->DirectionRatios[1], axis3D.RefDirection->DirectionRatios[2]);
					Vector3D ucsZAxis(axis3D.Axis->DirectionRatios[0], axis3D.Axis->DirectionRatios[1], axis3D.Axis->DirectionRatios[2]);
					ucsXAxis.Normalize();
					ucsZAxis.Normalize();
					Vector3D ucsYAxis = Vector3D::CrossProduct(ucsZAxis, ucsXAxis);
					ucsYAxis.Normalize();
					Point3D% ucsCentre = axis3D.Location->WPoint3D();

					Matrix3D ucsTowcs (	ucsXAxis.X, ucsXAxis.Y, ucsXAxis.Z, 0,
						ucsYAxis.X, ucsYAxis.Y, ucsYAxis.Z, 0,
						ucsZAxis.X, ucsZAxis.Y, ucsZAxis.Z, 0,
						ucsCentre.X, ucsCentre.Y, ucsCentre.Z , 1);
					if (locPlacement.PlacementRelTo != nullptr)
					{
						return Matrix3D::Multiply(ucsTowcs, CartesianTransform::ConvertMatrix3D(locPlacement.PlacementRelTo));
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
