// shapefile.h


#pragma once
#include <Windows.h>
#include <gl/gl.h>
#include <gl/glu.h>
#pragma unmanaged
#include "shapefil.h"
#pragma managed
#include "IShapefileSHPWriter.h"
#include "IShapefileDBFWriter.h"
using namespace System;
using namespace System::IO;
using namespace System::Collections::Generic;
using namespace std;
using namespace Xbim::ModelGeometry;
using namespace System::Windows::Media::Media3D;

namespace Xbim
{
	namespace Shapefile {


		public ref class SHPObjectData:IXbimMeshGeometry,IShapefileSHPWriter
		{
		private: 
			//these atributs are persistent for the whole live of the object
			ShapeType shpType;
			String^ shpName;
			SHPHandle shpHandle;
			int nShapeId;

			//these atributs belong to the implementation of the IXbimMeshGeometry
			Matrix3D^ matrix3d;
			Point3DCollection^ points;
			int numPoints;
			MemoryStream^ _dataStream;
			//int lastTesselation;
			//int actualTesselation;

			//these atributs are changed every time when "writeData()" is called
			int nParts;
			List<int>^ panPartStart;
			List<int>^ paPartType;
			int nVertices;
			List<double>^ adfX;
			List<double>^ adfY;
			List<double>^ adfZ;
			List<double>^ adfM;
			
		

			//shared code for all constructors
			void baseInit(String^ SHPname,ShapeType shpType);

			/// <summary>
			/// CreateObject() creates object which could be pased to the SHPWriteObject() function.
			/// </summary>
			/// <returns>IntPtr for the object</returns>
		protected:
			SHPObject* CreateObject();


		public: 
			SHPObjectData(String^ SHPname,ShapeType shpType);
			SHPObjectData(String^ SHPname,ShapeType shpType, Matrix3D^ matrix3d);
			/// <summary>
			/// Adds point into data object for creation of the object. Must be called AFTER addPart() function!!!
			/// </summary>
			/// <param name="X"> X coordinate of the point</param>
			/// <param name="Y">Y  coordinate of the point</param>
			/// <param name="Z">Z  coordinate of the point</param>
			/// <param name="M">measure of the point</param>
			virtual void addPointToSHP(double X, double Y, double Z);
			virtual void addPointToSHP(double X, double Y, double Z, double M);
			/// <summary>
			/// addPart function must be called before the addPoint function. It is based on number of points in the object, and result is index of 
			/// begining of the part of the object. Must be called once at least. At the end of the part, you must call endPart() method to close the part. 
			/// Othervise some software might has problems with reading this features, because due to format specifitation it should be end-to-end.
			/// </summary>
			/// <param name="paPartType">type of the part (PartType.OuterRing, InnerRing, TriangleStrip, ...)</param>
			virtual void addPart(PartType paPartType);
			virtual void addPart();
			/// <summary>
			/// This function closes the part. It should be called at the end of every part.
			/// Othervise some software might has problems with reading this features, 
			/// because due to format specifitation it should be end-to-end.
			/// </summary>
			virtual void endPart();
			/// <summary>
			/// This method writes actual parameters of ObjectData
			/// </summary>
			virtual void writeParams();
			virtual void clearData();
			virtual void writeData();
			virtual int getID() {return this->nShapeId;}
			~SHPObjectData();
			!SHPObjectData();

			//implementation of the IXbimMeshGeometry class enables it to be receiver of the geometry
			virtual void setTransformMatrix(Matrix3D^ matrix3d);
			virtual void BeginTessellate(GLenum type);
			virtual void EndTessellate();
			virtual void TessellateError(GLenum errNum);
			virtual void AddVertex(IntPtr index);
			virtual void AddPoint(double X, double Y, double Z);
			virtual void AddNormal(double X, double Y, double Z);
			virtual void AddTriangleIndices(int a, int b, int c);
			virtual void AddChild(IXbimMeshGeometry^ child);
			virtual void Clear();
			virtual int  PositionCount();
			virtual void Freeze();
			virtual void InitFromStream(Stream^ strm);

		private:
			void InstanceCleanup()
			{   
				int temp = System::Threading::Interlocked::Exchange((int)(void*)shpHandle, 0);
				if(temp!=0)
				{
					if (shpHandle)
					{
						delete shpHandle;
						shpHandle=0;
						System::GC::SuppressFinalize(this);
					}
				}
			}

		};

		//======================================DBF handling========================================




		public ref class DBFObjectData: IShapefileDBFWriter
		{
		private:
			DBFHandle dbfHandle;
			List<DBFFieldType>^ dbfFieldTypeEnum;
			List<String^>^ dbfFieldName;
			List<int>^ fieldWidth;
			List<int>^ fieldDecimals;
			int shpID;
			bool locked;
			//this is cleared after every writting into the DBF file (writeRow() method)
			List<Object^>^ values;

		public:
			DBFObjectData(String^ SHPname);
			//virtual void addFields(List<DBFFieldType>^ dbfFieldTypeEnum, List<String^>^ dbfFieldName, List<int>^ fieldWidth, List<int>^ fieldDecimals);
			//virtual void addField(DBFFieldType dbfFieldType, String^ dbfFieldName, int fieldWidth, int fieldDecimals);
			//virtual void addValues(List<Object^>^ values);
			virtual void addValue(Object^ value, DBFFieldType dbfFieldType, String^ dbfFieldName, int fieldWidth, int fieldDecimals);
			virtual void writeParams();
			virtual void clearData();
			virtual void writeRow();
			virtual int getID() {return this->shpID;}
			~DBFObjectData();
			!DBFObjectData();

			void InstanceCleanup()
			{   
				int temp = System::Threading::Interlocked::Exchange((int)(void*)dbfHandle, 0);
				if(temp!=0)
				{
					if (dbfHandle)
					{
						delete dbfHandle;
						dbfHandle=0;
						System::GC::SuppressFinalize(this);
					}
				}
			}
		};
	}
}