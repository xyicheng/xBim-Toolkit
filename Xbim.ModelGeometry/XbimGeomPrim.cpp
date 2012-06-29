#include "StdAfx.h"
#include "XbimGeomPrim.h"

#include <gp_Dir2d.hxx>
#include <gp_Dir.hxx>
#include <gp_Dir2d.hxx>
#include <gp_Vec2d.hxx>
#include <gp_XYZ.hxx>
#include <gp_XY.hxx>

#include <gp_Trsf.hxx> 
#include <gp_Trsf2d.hxx> 
#include <gp_Ax2.hxx>
#include <gp_Ax2d.hxx>
#include <gp_Ax3.hxx>
#include <gp_Mat2d.hxx>

using namespace System;
using namespace System::Windows::Media::Media3D;

namespace Xbim
{
	namespace ModelGeometry
	{
		XbimGeomPrim::XbimGeomPrim(void)
		{
		}

		// Converts an Axis2Placement3D into a TopLoc_Location
		TopLoc_Location XbimGeomPrim::ToLocation(IfcPlacement^ placement)
		{
			if(dynamic_cast<IfcAxis2Placement3D^>(placement))
			{
				IfcAxis2Placement3D^ axis3D=(IfcAxis2Placement3D^) placement;
				return ToLocation(axis3D);
			}
			else if(dynamic_cast<IfcAxis2Placement2D^>(placement))
			{
				IfcAxis2Placement2D^ axis2D=(IfcAxis2Placement2D^) placement;
				return ToLocation(axis2D);
			}
			else
			{
				throw(gcnew NotImplementedException("XbimGeomPrim. Unsupported Placement type, need to implement Grid Placement"));
			}
		}



		TopLoc_Location XbimGeomPrim::ToLocation(IfcAxis2Placement^ placement)
		{
			if(dynamic_cast<IfcAxis2Placement3D^>(placement))
			{
				IfcAxis2Placement3D^ axis3D=(IfcAxis2Placement3D^) placement;
				return ToLocation(axis3D);
			}
			else if(dynamic_cast<IfcAxis2Placement2D^>(placement))
			{
				IfcAxis2Placement2D^ axis2D=(IfcAxis2Placement2D^) placement;
				return ToLocation(axis2D);
			}
			else if(placement == nullptr)
				return TopLoc_Location();
			else
			{
				throw(gcnew NotImplementedException("XbimGeomPrim. Unsupported Placement type, need to implement Grid Placement"));
			}

		}


		TopLoc_Location XbimGeomPrim::ToLocation(IfcAxis2Placement3D^ axis3D)
		{
			gp_Trsf trsf;
			trsf.SetTransformation(ToAx3(axis3D),gp_Ax3());	
			return TopLoc_Location(trsf);
		}

		gp_Ax3 XbimGeomPrim::ToAx3(IfcAxis2Placement3D^ axis3D)
		{
			gp_XYZ loc(axis3D->Location->X,axis3D->Location->Y, axis3D->Location->Z);
			if(axis3D->Axis != nullptr && axis3D->RefDirection != nullptr) //if one or other is null then use default axis (Ifc Rule)
			{
				gp_Dir zDir(axis3D->Axis->X,axis3D->Axis->Y, axis3D->Axis->Z);
				gp_Dir xDir(axis3D->RefDirection->X,axis3D->RefDirection->Y, axis3D->RefDirection->Z);
				return gp_Ax3(gp_Ax2(loc, zDir, xDir));
			}
			else
			{	
				gp_Dir zDir(0,0,1);
				gp_Dir xDir(1,0,0);
				return gp_Ax3(gp_Ax2(loc, zDir, xDir));	
			}	
		}

		TopLoc_Location XbimGeomPrim::ToLocation(IfcAxis2Placement2D^ axis2D)
		{
			gp_Pnt2d loc(axis2D->Location->X,axis2D->Location->Y);

			// If problems with creation of direction occur default direction is used
			gp_Dir2d Vxgp = gp_Dir2d(1.,0.);
			if(axis2D->RefDirection != nullptr) 
			{
				Standard_Real X = axis2D->RefDirection->X;
				Standard_Real Y = axis2D->RefDirection->Y;

				//Direction is not created if it has null magnitude
				gp_XY aXY(X, Y);  
				Standard_Real res2 = gp::Resolution()*gp::Resolution();
				Standard_Real aMagnitude = aXY.SquareModulus();
				if(aMagnitude > res2) 
				{
					Vxgp = gp_Dir2d(aXY);
				}

			}
			gp_Ax2d axis2d(loc,Vxgp);
			gp_Trsf2d trsf;
			trsf.SetTransformation(axis2d);
			trsf.Invert();
			return TopLoc_Location(trsf);
		}

		gp_Pln XbimGeomPrim::ToPlane(IfcAxis2Placement3D^ axis3D)
		{
			return gp_Pln(ToAx3(axis3D));
		}

		gp_Trsf XbimGeomPrim::ToTransform(IfcCartesianTransformationOperator^ tForm)
		{
			if(dynamic_cast<IfcCartesianTransformationOperator3DnonUniform^>(tForm))
				//Call the special case method for non uniform transforms and use BRepBuilderAPI_GTransform
				//instead of BRepBuilderAPI_Transform,  see opencascade issue
				// http://www.opencascade.org/org/forum/thread_300/?forum=3
				throw (gcnew XbimGeometryException("XbimGeomPrim. IfcCartesianTransformationOperator3DnonUniform require a specific call"));
			else if(dynamic_cast<IfcCartesianTransformationOperator3D^>(tForm))
				return ToTransform((IfcCartesianTransformationOperator3D^) tForm);
			else if(dynamic_cast<IfcCartesianTransformationOperator2D^>(tForm))
				return ToTransform((IfcCartesianTransformationOperator2D^) tForm);
			else if(tForm==nullptr)
				return gp_Trsf();
			else
				throw(gcnew ArgumentOutOfRangeException("XbimGeomPrim. Unsupported CartesianTransformationOperator type"));
		}

		gp_Trsf XbimGeomPrim::ToTransform(IfcCartesianTransformationOperator3D^ ct3D)
		{
			Vector3D U3; //Z Axis Direction
			Vector3D U2; //X Axis Direction
			Vector3D U1; //Y axis direction
			if(ct3D->Axis3!=nullptr)
			{
				IfcDirection% dir = (IfcDirection%)ct3D->Axis3;
				U3 = Vector3D(dir.DirectionRatios[0],dir.DirectionRatios[1],dir.DirectionRatios[2]); 
				U3.Normalize();
			}
			else
				U3 = Vector3D(0.,0.,1.); 
			if(ct3D->Axis1!=nullptr)
			{
				IfcDirection% dir = (IfcDirection%)ct3D->Axis1;
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

			if(ct3D->Axis2!=nullptr)
			{
				IfcDirection% dir = (IfcDirection%)ct3D->Axis2;
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

			Point3D% LO = ct3D->LocalOrigin->WPoint3D(); //local origin

			gp_Trsf trsf;
			trsf.SetValues(	U1.X, U1.Y, U1.Z, 0,
				U2.X, U2.Y, U2.Z, 0,
				U3.X, U3.Y, U3.Z, 0, 
				Precision::Angular() ,Precision::Approximation());

			trsf.SetTranslationPart(gp_Vec(LO.X, LO.Y, LO.Z));
			trsf.SetScaleFactor(ct3D->Scl);
			return trsf;
		}

		gp_GTrsf XbimGeomPrim::ToTransform(IfcCartesianTransformationOperator3DnonUniform^ ct3D)
		{
			Vector3D U3; //Z Axis Direction
			Vector3D U2; //X Axis Direction
			Vector3D U1; //Y axis direction
			if(ct3D->Axis3!=nullptr)
			{
				IfcDirection% dir = (IfcDirection%)ct3D->Axis3;
				U3 = Vector3D(dir.DirectionRatios[0],dir.DirectionRatios[1],dir.DirectionRatios[2]); 
				U3.Normalize();
			}
			else
				U3 = Vector3D(0.,0.,1.); 
			if(ct3D->Axis1!=nullptr)
			{
				IfcDirection% dir = (IfcDirection%)ct3D->Axis1;
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

			if(ct3D->Axis2!=nullptr)
			{
				IfcDirection% dir = (IfcDirection%)ct3D->Axis2;
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

			Point3D% LO = ct3D->LocalOrigin->WPoint3D(); //local origin

			double s1=ct3D->Scl*U1.X;
			double s2=ct3D->Scl2* U2.Y;
			double s3=ct3D->Scl3* U3.Z;
			gp_GTrsf trsf(
				gp_Mat(	s1, U1.Y, U1.Z,
				U2.X, s2 , U2.Z,
				U3.X, U3.Y, s3 
				),
				gp_XYZ(LO.X, LO.Y, LO.Z));

			return trsf;
		}

		gp_Trsf XbimGeomPrim::ToTransform(Matrix3D m3D)
		{
			gp_Trsf trsf;
			trsf.SetValues(	m3D.M11, m3D.M12, m3D.M13, 0,
				m3D.M21, m3D.M22, m3D.M23, 0,
				m3D.M31, m3D.M32, m3D.M33, 0, 
				Precision::Angular() ,Precision::Approximation());
			trsf.SetTranslationPart(gp_Vec(m3D.OffsetX, m3D.OffsetY, m3D.OffsetZ));
			return trsf;
		}

		gp_Trsf XbimGeomPrim::ToTransform(IfcCartesianTransformationOperator2D^ ct)
		{
			gp_Trsf2d m;
			IfcDirection^ axis1 = ct->Axis1;
			IfcDirection^ axis2 = ct->Axis2;
			double scale = ct->Scl;
			IfcCartesianPoint^ o = ct->LocalOrigin;
			gp_Mat2d mat = m.HVectorialPart();
			if (axis1 != nullptr)
			{
				System::Windows::Vector d1 = axis1->WVector();
				d1.Normalize();


				mat.SetValue(1,1,d1.X);
				mat.SetValue(1,2,d1.Y);
				mat.SetValue(2,1,-d1.Y);
				mat.SetValue(2,2,d1.X);

				if (axis2 != nullptr)
				{
					System::Windows::Vector v(-d1.Y, d1.X);
					double factor = System::Windows::Vector::Multiply(axis2->WVector(),v );
					if (factor < 0)
					{
						mat.SetValue(2,1,d1.Y);
						mat.SetValue(2,2,-d1.X);
					}
				}
			}
			else
			{
				if (axis2 != nullptr)
				{
					System::Windows::Vector d1 = axis2->WVector();
					d1.Normalize();
					mat.SetValue(1,1,d1.Y);
					mat.SetValue(1,2,-d1.X);
					mat.SetValue(2,1,d1.X);
					mat.SetValue(2,2,d1.X);
				}
			}

			m.SetScaleFactor(scale);
			m.SetTranslationPart(gp_Vec2d(o->X, o->Y));
			return m;

		}
	}
}