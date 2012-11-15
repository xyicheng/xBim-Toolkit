// This file is generated by WOK (CPPExt).
// Please do not edit this file; modify original file instead.
// The copyright and license terms as defined for the original file apply to 
// this header file considered to be the "object code" form of the original source.

#ifndef _BRepBuilderAPI_MakePolygon_HeaderFile
#define _BRepBuilderAPI_MakePolygon_HeaderFile

#ifndef _Standard_HeaderFile
#include <Standard.hxx>
#endif
#ifndef _Standard_DefineAlloc_HeaderFile
#include <Standard_DefineAlloc.hxx>
#endif
#ifndef _Standard_Macro_HeaderFile
#include <Standard_Macro.hxx>
#endif

#ifndef _BRepLib_MakePolygon_HeaderFile
#include <BRepLib_MakePolygon.hxx>
#endif
#ifndef _BRepBuilderAPI_MakeShape_HeaderFile
#include <BRepBuilderAPI_MakeShape.hxx>
#endif
#ifndef _Standard_Boolean_HeaderFile
#include <Standard_Boolean.hxx>
#endif
class StdFail_NotDone;
class gp_Pnt;
class TopoDS_Vertex;
class TopoDS_Edge;
class TopoDS_Wire;


//! Describes functions to build polygonal wires. A <br>
//! polygonal wire can be built from any number of points <br>
//! or vertices, and consists of a sequence of connected <br>
//! rectilinear edges. <br>
//!          When a point or vertex is added to the  polygon if <br>
//!          it is identic  to the previous  point no  edge  is <br>
//!          built. The method added can be used to test it. <br>
//! Construction of a Polygonal Wire <br>
//! You can construct: <br>
//!   -   a complete polygonal wire by defining all its points <br>
//!   or vertices (limited to four), or <br>
//! -   an empty polygonal wire and add its points or <br>
//!   vertices in sequence (unlimited number). <br>
//! A MakePolygon object provides a framework for: <br>
//! -   initializing the construction of a polygonal wire, <br>
//! -   adding points or vertices to the polygonal wire under construction, and <br>
//! -   consulting the result. <br>
class BRepBuilderAPI_MakePolygon  : public BRepBuilderAPI_MakeShape {
public:

  DEFINE_STANDARD_ALLOC

  //! Initializes an empty polygonal wire, to which points or <br>
//! vertices are added using the Add function. <br>
//! As soon as the polygonal wire under construction <br>
//! contains vertices, it can be consulted using the Wire function. <br>
  Standard_EXPORT   BRepBuilderAPI_MakePolygon();
  
  Standard_EXPORT   BRepBuilderAPI_MakePolygon(const gp_Pnt& P1,const gp_Pnt& P2);
  
  Standard_EXPORT   BRepBuilderAPI_MakePolygon(const gp_Pnt& P1,const gp_Pnt& P2,const gp_Pnt& P3,const Standard_Boolean Close = Standard_False);
  //! Constructs a polygonal wire from 2, 3 or 4 points. Vertices are <br>
//! automatically created on the given points. The polygonal wire is <br>
//! closed if Close is true; otherwise it is open. Further vertices can <br>
//! be added using the Add function. The polygonal wire under <br>
//! construction can be consulted at any time by using the Wire function. <br>
//! Example <br>
//! //an open polygon from four points <br>
//! TopoDS_Wire W = BRepBuilderAPI_MakePolygon(P1,P2,P3,P4); <br>
//! Warning: The process is equivalent to: <br>
//!   - initializing an empty polygonal wire, <br>
//!   - and adding the given points in sequence. <br>
//! Consequently, be careful when using this function: if the <br>
//! sequence of points p1 - p2 - p1 is found among the arguments of the <br>
//! constructor, you will create a polygonal wire with two <br>
//! consecutive coincident edges. <br>
  Standard_EXPORT   BRepBuilderAPI_MakePolygon(const gp_Pnt& P1,const gp_Pnt& P2,const gp_Pnt& P3,const gp_Pnt& P4,const Standard_Boolean Close = Standard_False);
  
  Standard_EXPORT   BRepBuilderAPI_MakePolygon(const TopoDS_Vertex& V1,const TopoDS_Vertex& V2);
  
  Standard_EXPORT   BRepBuilderAPI_MakePolygon(const TopoDS_Vertex& V1,const TopoDS_Vertex& V2,const TopoDS_Vertex& V3,const Standard_Boolean Close = Standard_False);
  //! Constructs a polygonal wire from <br>
//! 2, 3 or 4 vertices. The polygonal wire is closed if Close is true; <br>
//! otherwise it is open (default value). Further vertices can be <br>
//! added using the Add function. The polygonal wire under <br>
//! construction can be consulted at any time by using the Wire function. <br>
//! Example <br>
//! //a closed triangle from three vertices <br>
//! TopoDS_Wire W = BRepBuilderAPI_MakePolygon(V1,V2,V3,Standard_True); <br>
//! Warning <br>
//! The process is equivalent to: <br>
//! -      initializing an empty polygonal wire, <br>
//! -      then adding the given points in sequence. <br>
//! So be careful, as when using this function, you could create a <br>
//! polygonal wire with two consecutive coincident edges if <br>
//! the sequence of vertices v1 - v2 - v1 is found among the <br>
//! constructor's arguments. <br>
  Standard_EXPORT   BRepBuilderAPI_MakePolygon(const TopoDS_Vertex& V1,const TopoDS_Vertex& V2,const TopoDS_Vertex& V3,const TopoDS_Vertex& V4,const Standard_Boolean Close = Standard_False);
  
  Standard_EXPORT     void Add(const gp_Pnt& P) ;
  
//! Adds the point P or the vertex V at the end of the <br>
//! polygonal wire under construction. A vertex is <br>
//! automatically created on the point P. <br>
//! Warning <br>
//! -   When P or V is coincident to the previous vertex, <br>
//!   no edge is built. The method Added can be used to <br>
//!   test for this. Neither P nor V is checked to verify <br>
//!   that it is coincident with another vertex than the last <br>
//!   one, of the polygonal wire under construction. It is <br>
//!   also possible to add vertices on a closed polygon <br>
//!   (built for example by using a constructor which <br>
//!   declares the polygon closed, or after the use of the Close function). <br>
//!  Consequently, be careful using this function: you might create: <br>
//! -      a polygonal wire with two consecutive coincident edges, or <br>
//! -      a non manifold polygonal wire. <br>
//! -      P or V is not checked to verify if it is <br>
//!    coincident with another vertex but the last one, of <br>
//!    the polygonal wire under construction. It is also <br>
//!    possible to add vertices on a closed polygon (built <br>
//!    for example by using a constructor which declares <br>
//!    the polygon closed, or after the use of the Close function). <br>
//! Consequently, be careful when using this function: you might create: <br>
//!   -   a polygonal wire with two consecutive coincident edges, or <br>
//!   -   a non-manifold polygonal wire. <br>
  Standard_EXPORT     void Add(const TopoDS_Vertex& V) ;
  //! Returns true if the last vertex added to the constructed <br>
//! polygonal wire is not coincident with the previous one. <br>
  Standard_EXPORT     Standard_Boolean Added() const;
  //! Closes the polygonal wire under construction. Note - this <br>
//! is equivalent to adding the first vertex to the polygonal <br>
//! wire under construction. <br>
  Standard_EXPORT     void Close() ;
  
  Standard_EXPORT    const TopoDS_Vertex& FirstVertex() const;
  //! Returns the first or the last vertex of the polygonal wire under construction. <br>
//! If the constructed polygonal wire is closed, the first and the last vertices are identical. <br>
  Standard_EXPORT    const TopoDS_Vertex& LastVertex() const;
  
//! Returns true if this algorithm contains a valid polygonal <br>
//! wire (i.e. if there is at least one edge). <br>
//! IsDone returns false if fewer than two vertices have <br>
//! been chained together by this construction algorithm. <br>
  Standard_EXPORT   virtual  Standard_Boolean IsDone() const;
  //! Returns the edge built between the last two points or <br>
//! vertices added to the constructed polygonal wire under construction. <br>
//! Warning <br>
//! If there is only one vertex in the polygonal wire, the result is a null edge. <br>
  Standard_EXPORT    const TopoDS_Edge& Edge() const;
Standard_EXPORT operator TopoDS_Edge() const;
  
//! Returns the constructed polygonal wire, or the already <br>
//! built part of the polygonal wire under construction. <br>
//! Exceptions <br>
//! StdFail_NotDone if the wire is not built, i.e. if fewer than <br>
//! two vertices have been chained together by this construction algorithm. <br>
  Standard_EXPORT    const TopoDS_Wire& Wire() const;
Standard_EXPORT operator TopoDS_Wire() const;





protected:





private:



BRepLib_MakePolygon myMakePolygon;


};





// other Inline functions and methods (like "C++: function call" methods)


#endif
