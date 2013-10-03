// This is the main DLL file.
#pragma once
#include "stdafx.h"
#include "shapefile.h"
#include <vcclr.h>
#include <msclr/marshal.h>
using namespace msclr::interop;
using namespace Xbim::Shapefile;


void SHPObjectData::baseInit(String^ SHPname, ShapeType shpType)
{
	this->shpType = shpType;
	this->panPartStart = gcnew List<int>();
	this->paPartType = gcnew List<int>();
	this->adfX = gcnew List<double>();
	this->adfY = gcnew List<double>();
	this->adfZ = gcnew List<double>();
	this->adfM = gcnew List<double>();

	this->nShapeId = 0;
	this->nParts = 0;
	this->nVertices = 0;

	this->shpName = SHPname;

	marshal_context ^ context = gcnew marshal_context();
	const char* name = context->marshal_as<const char*>(SHPname);

	this->shpHandle = SHPCreate(name, (int)shpType);
	if (this->shpHandle == nullptr)
		throw gcnew Exception("Creation of the new SHP file was not successful.");

	//this part belongs to the IXbimMeshGeometry
	this->matrix3d = nullptr;
	this->points  = gcnew Point3DCollection();
	this->numPoints = 0;

}

SHPObjectData::SHPObjectData(String^ SHPname, ShapeType shpType)
{
	baseInit(SHPname, shpType);
}

SHPObjectData::SHPObjectData(String^ SHPname,ShapeType shpType, Matrix3D^ matrix3d)
{
	baseInit(SHPname, shpType);
	this->matrix3d = matrix3d;
}

SHPObject* SHPObjectData::CreateObject()
		{
			if (this->nVertices == 0) return nullptr;

			array<int>^ panPartStartArr = panPartStart->ToArray();
			pin_ptr<int> panPartStartPtr = &panPartStartArr[0];

			array<int>^ paPartTypeArr = paPartType->ToArray();
			pin_ptr<int> paPartTypePtr = &paPartTypeArr[0];

			array<double, 1>^ adfXArr = adfX->ToArray();
			pin_ptr<double> adfXPtr = &adfXArr[0];

			array<double>^ adfYArr = adfY->ToArray();
			pin_ptr<double> adfYPtr = &adfYArr[0];

			array<double>^ adfZArr = adfZ->ToArray();
			pin_ptr<double> adfZPtr = &adfZArr[0];

			array<double>^ adfMArr = adfM->ToArray();
			pin_ptr<double> adfMPtr = &adfMArr[0];

			return 
				SHPCreateObject((int)shpType, nShapeId, nParts, panPartStartPtr, paPartTypePtr, nVertices, adfXPtr, adfYPtr, adfZPtr, adfMPtr);
		}
void SHPObjectData::addPointToSHP(double X, double Y, double Z)
		{
			adfX->Add(X);
			adfY->Add(Y);
			adfZ->Add(Z);
			adfM->Add(0);
			nVertices++;
		}
void SHPObjectData::addPointToSHP(double X, double Y, double Z, double M)
		{
			adfX->Add(X);
			adfY->Add(Y);
			adfZ->Add(Z);
			adfM->Add(M);
			nVertices++;
		}
void SHPObjectData::addPart(PartType paPartType)
		{
			this->nParts++;
			this->panPartStart->Add(nVertices);
			if (this->paPartType != nullptr)
			    this->paPartType->Add((int)paPartType);
		}
void SHPObjectData::addPart() 
		{
			this->nParts++;
			this->panPartStart->Add(nVertices);
			if (this->paPartType != nullptr)
					this->paPartType->Add((int)PartType::Ring);
			}
	void SHPObjectData::endPart() 
			{
				//it is needed only for shapefiles of the polygon type (multipatch is also based on polygons, but triangles doesn't have to be closed)
				if (this->shpType == ShapeType::MultiPatch || 
					this->shpType == ShapeType::PolygonM || 
					this->shpType == ShapeType::PolygonZ || 
					(this->shpType == ShapeType::Polygon && 
						this->paPartType[this->paPartType->Count-1] != (int)PartType::TriangleStrip && 
						this->paPartType[this->paPartType->Count-1] != (int)PartType::TriangleFan))
				{
					adfX->Add(adfX[panPartStart[panPartStart->Count-1]]);
					adfY->Add(adfY[panPartStart[panPartStart->Count-1]]);
					adfZ->Add(adfZ[panPartStart[panPartStart->Count-1]]);
					adfM->Add(adfM[panPartStart[panPartStart->Count-1]]);
					nVertices++;
				}
				else
				{
					return;
				}
			}
	void SHPObjectData::writeParams() 
			{
				Console::WriteLine("Shape type:");
				Console::WriteLine(shpType);
				Console::WriteLine("Shape id:");
				Console::WriteLine(nShapeId);
				Console::WriteLine("Parts:");
				Console::WriteLine(nParts);
				Console::WriteLine("Parts start vertices:");
				for each(double s in panPartStart) 
				{
					Console::Write("{0} ", s);
				}
				Console::Write("\n");
				Console::WriteLine("Number of vertices:");
				Console::WriteLine(nVertices);
				Console::WriteLine("X coordinates:");
				for each(double x in adfX) 
				{
					Console::Write("{0} ", x);
				}
				Console::Write("\n");
				Console::WriteLine("Y coordinates:");
				for each(double y in adfY) 
				{
					Console::Write("{0} ", y);
				}
				Console::Write("\n");
				Console::WriteLine("Z coordinates:");
				for each(double z in adfZ) 
				{
					Console::Write("{0} ", z);
				}
				Console::Write("\n");
				Console::WriteLine("M values");
				for each(double m in adfM) 
				{
					Console::Write("{0} ", m);
				}
				Console::WriteLine();
			}

	void SHPObjectData::clearData()
	{
		this->nParts = 0;
        this->panPartStart->Clear();
        this->paPartType->Clear();
        this->nVertices = 0;
        this->adfX->Clear();
        this->adfY->Clear();
        this->adfZ->Clear();
        this->adfM->Clear();

	}


	void SHPObjectData::writeData()
	{
		if (this->nVertices == 0) return;
		SHPObject * psObject = this->CreateObject();
		if (psObject == nullptr) 
			throw gcnew Exception("Creation of the feature class was not successful.");
		//inserts new feature class into the shapefile
		SHPWriteObject( this->shpHandle, -1, psObject );
		SHPDestroyObject(psObject);
		this->nShapeId++;
		this->clearData();
	}

	SHPObjectData::~SHPObjectData()
	{
		this->clearData();
		delete this->shpName;
		delete this->matrix3d;
		delete this->points;
		delete this->panPartStart;
		delete this->paPartType;
		delete this->adfX;
		delete this->adfY;
		delete this->adfZ;
		delete this->adfM;

		//finalize unmanaged resources
		this->!SHPObjectData();
	}

	SHPObjectData::!SHPObjectData()
	{
		SHPClose(this->shpHandle);
		//I can't use this method, because all resources are freed by the previous C function and next calling of free raises assertion.
		//this->InstanceCleanup();
	}