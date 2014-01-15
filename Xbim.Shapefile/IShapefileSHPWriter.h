using namespace System::Windows::Media::Media3D;

namespace Xbim 
{
	namespace Shapefile
	{
		/// <summary>
		/// Shape type enumeration
		/// </summary>
		public enum class ShapeType
		{
			/// <summary>Shape with no geometric data</summary>
			NullShape = 0,
			/// <summary>2D point</summary>
			Point = 1,
			/// <summary>2D polyline</summary>
			PolyLine = 3,
			/// <summary>2D polygon</summary>
			Polygon = 5,
			/// <summary>Set of 2D points</summary>
			MultiPoint = 8,
			/// <summary>3D point</summary>
			PointZ = 11,
			/// <summary>3D polyline</summary>
			PolyLineZ = 13,
			/// <summary>3D polygon</summary>
			PolygonZ = 15,
			/// <summary>Set of 3D points</summary>
			MultiPointZ = 18,
			/// <summary>3D point with measure</summary>
			PointM = 21,
			/// <summary>3D polyline with measure</summary>
			PolyLineM = 23,
			/// <summary>3D polygon with measure</summary>
			PolygonM = 25,
			/// <summary>Set of 3d points with measures</summary>
			MultiPointM = 28,
			/// <summary>Collection of surface patches</summary>
			MultiPatch = 31
		};

		/// <summary>
		/// Part type enumeration - everything but ShapeType.MultiPatch just uses PartType.Ring.
		/// </summary>
		public enum class PartType
		{
			/// <summary>
			/// Linked strip of triangles, where every vertex (after the first two) completes a new triangle.
			/// A new triangle is always formed by connecting the new vertex with its two immediate predecessors.
			/// </summary>
			TriangleStrip = 0,
			/// <summary>
			/// A linked fan of triangles, where every vertex (after the first two) completes a new triangle.
			/// A new triangle is always formed by connecting the new vertex with its immediate predecessor 
			/// and the first vertex of the part.
			/// </summary>
			TriangleFan = 1,
			/// <summary>The outer ring of a polygon</summary>
			OuterRing = 2,
			/// <summary>The first ring of a polygon</summary>
			InnerRing = 3,
			/// <summary>The outer ring of a polygon of an unspecified type</summary>
			FirstRing = 4,
			/// <summary>A ring of a polygon of an unspecified type</summary>
			Ring = 5
		};

		public interface class IShapefileSHPWriter
		{
			void addPointToSHP(double X, double Y, double Z);
			void addPointToSHP(double X, double Y, double Z, double M);
			void addPart(PartType paPartType);
			void addPart();
			void endPart();
			void writeParams();
			void clearData();
			void writeData();
			int getID();

			void setTransformMatrix(Matrix3D^ matrix3d);
		};

	}
}