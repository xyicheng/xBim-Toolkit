#pragma once

#include <carve/carve.hpp>
#include <carve/collection_types.hpp>
#include "XbimLocation.h"
#include "XbimBoundingBox.h"
#include "IXbimMeshGeometry.h"
#include "XbimGeometryModel.h"
#include <TopoDS_Shape.hxx>
#include <TopoDS_Compound.hxx>
#include <TopTools_DataMapOfShapeInteger.hxx>
#include <TopTools_IndexedMapOfShape.hxx>
#include <functional>

#pragma unmanaged



namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{

	

			struct Double3D
			{
				double Dim1;
				double Dim2;
				double Dim3;
				double Precision2;	
				float rounding10P;
				
			    Double3D(double D1,	double D2,	double D3)
					:Dim1(D1),Dim2(D2),Dim3(D3), Precision2(1e-10), rounding10P(1.0F)
				{

				}

				Double3D(double D1,	double D2,	double D3, double precision, unsigned int roundPlaces)
					:Dim1(D1),Dim2(D2),Dim3(D3), Precision2(precision*precision), rounding10P(std::pow(10, (float)roundPlaces))
				{

				}

				void Round(float roundPlaces)
				{
					Dim1 = (long)(Dim1 * rounding10P) / rounding10P;
					Dim2 = (long)(Dim2 * rounding10P) / rounding10P;
					Dim3 = (long)(Dim3 * rounding10P) / rounding10P;
				}

				operator size_t () const
				{
					
					return long((Dim1 * rounding10P)+0.5)^
						   long((Dim2 * rounding10P)+0.5)^
						   long((Dim3 * rounding10P)+0.5); 
				}
				
				bool operator==(const Double3D& b) const
				{
					double d = 0, dd;
					double x1 = Dim1, y1 = Dim2, z1=Dim3, x2=b.Dim1, y2=b.Dim2, z2=b.Dim3;
					dd = x1; dd -= x2; dd *= dd; d += dd;
					dd = y1; dd -= y2; dd *= dd; d += dd;
					dd = z1; dd -= z2; dd *= dd; d += dd;

					return d <= Precision2;	
				}

				bool operator<(const Double3D& A) const
				{
					if (Dim1 != A.Dim1)
						return Dim1 < A.Dim1;
					if (Dim2 != A.Dim2)
						return Dim2 < A.Dim2;
					return Dim3 < A.Dim3;
				}


			};

			struct UIntegerPair {
				size_t PositionIndex;
				size_t NormalIndex;

				bool operator<(const UIntegerPair& A) const
				{
					if (PositionIndex != A.PositionIndex)
						return PositionIndex < A.PositionIndex;
					return NormalIndex < A.NormalIndex;
				}

				bool operator==(const UIntegerPair& A) const
				{
					return PositionIndex == A.PositionIndex && NormalIndex == A.NormalIndex;	
				}

				operator size_t() const
				{
					return (size_t)( PositionIndex ^ NormalIndex);
				}

			};
			struct PolygonInfo {
				GLenum GLType;
				int IndexCount;
			};

			typedef std::pair <Double3D,size_t> Double3DUInt_Pair;
			// Class to receive the calls that create the memory stream of the geometry cache files. (CB)
			//
			public class XbimTriangularMeshStreamer
			{
			public: 
				XbimTriangularMeshStreamer(int repLabel, int surfaceLabel);
				int RepresentationLabel;
				int SurfaceStyleLabel;
				void BeginFace(int NodesInFace);
				void EndFace();
				void BeginPolygon(GLenum type);
				void EndPolygon();
				void SetNormal(float x, float y, float z);
				size_t WritePoint(float x, float y, float z);
				void WriteTriangleIndex(size_t index);
				void info(char string);
				void info(int Number);
				size_t StreamSize();
				size_t StreamTo(unsigned char* pStream);
			private:


				size_t getUniquePoint(size_t pointIndex, size_t normalIndex);
				size_t sizeOptimised(size_t maxIndex);
				size_t _currentNormalIndex;
				size_t _facePointIndex;

				std::unordered_map<Double3D,size_t> _pointsMap;
				std::list<Double3D> _points;

				std::unordered_map<Double3D,size_t> _normalsMap;
				std::list<Double3D> _normals;

				std::unordered_map<UIntegerPair,size_t> _uniquePNMap;
				std::list<UIntegerPair> _uniquePN;	// unique point and normal combination

				std::list<PolygonInfo> _polygons;	// polygon informations
				std::list<size_t> _indices;	
				unsigned int _currentPolygonCount;
				size_t * _faceIndexMap;       // we're removing duplicates for the points; this array contains the mapping of non-optimised to optimised indices for a face
				bool _useFaceIndexMap;
				int WriteByte(unsigned char* pStream, unsigned int value);
				int WriteShort(unsigned char* pStream, unsigned int value);
				int WriteInt(unsigned char* pStream, unsigned int value);
				
			};
#pragma managed
		}
	}
}