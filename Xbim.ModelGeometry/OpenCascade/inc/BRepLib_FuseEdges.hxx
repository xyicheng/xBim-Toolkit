// This file is generated by WOK (CPPExt).
// Please do not edit this file; modify original file instead.
// The copyright and license terms as defined for the original file apply to 
// this header file considered to be the "object code" form of the original source.

#ifndef _BRepLib_FuseEdges_HeaderFile
#define _BRepLib_FuseEdges_HeaderFile

#ifndef _Standard_HeaderFile
#include <Standard.hxx>
#endif
#ifndef _Standard_DefineAlloc_HeaderFile
#include <Standard_DefineAlloc.hxx>
#endif
#ifndef _Standard_Macro_HeaderFile
#include <Standard_Macro.hxx>
#endif

#ifndef _TopoDS_Shape_HeaderFile
#include <TopoDS_Shape.hxx>
#endif
#ifndef _Standard_Boolean_HeaderFile
#include <Standard_Boolean.hxx>
#endif
#ifndef _TopTools_IndexedDataMapOfShapeListOfShape_HeaderFile
#include <TopTools_IndexedDataMapOfShapeListOfShape.hxx>
#endif
#ifndef _TopTools_DataMapOfIntegerListOfShape_HeaderFile
#include <TopTools_DataMapOfIntegerListOfShape.hxx>
#endif
#ifndef _TopTools_DataMapOfIntegerShape_HeaderFile
#include <TopTools_DataMapOfIntegerShape.hxx>
#endif
#ifndef _TopTools_DataMapOfShapeShape_HeaderFile
#include <TopTools_DataMapOfShapeShape.hxx>
#endif
#ifndef _Standard_Integer_HeaderFile
#include <Standard_Integer.hxx>
#endif
#ifndef _TopTools_IndexedMapOfShape_HeaderFile
#include <TopTools_IndexedMapOfShape.hxx>
#endif
#ifndef _TopAbs_ShapeEnum_HeaderFile
#include <TopAbs_ShapeEnum.hxx>
#endif
class Standard_ConstructionError;
class Standard_NullObject;
class TopoDS_Shape;
class TopTools_IndexedMapOfShape;
class TopTools_DataMapOfIntegerListOfShape;
class TopTools_DataMapOfIntegerShape;
class TopTools_DataMapOfShapeShape;
class TopTools_IndexedDataMapOfShapeListOfShape;
class TopTools_MapOfShape;
class TopTools_ListOfShape;
class TopoDS_Vertex;
class TopoDS_Edge;


//! This class can detect  vertices in a face that can <br>
//!          be considered useless and then perform the fuse of <br>
//!          the  edges and remove  the  useless vertices.  By <br>
//!          useles vertices,  we mean : <br>
//!            * vertices that  have  exactly two connex edges <br>
//!            * the edges connex to the vertex must have <br>
//!              exactly the same 2 connex faces . <br>
//!            * The edges connex to the vertex must have the <br>
//!               same geometric support. <br>
class BRepLib_FuseEdges  {
public:

  DEFINE_STANDARD_ALLOC

  //! Initialise members  and build  construction of map <br>
//!          of ancestors. <br>
  Standard_EXPORT   BRepLib_FuseEdges(const TopoDS_Shape& theShape,const Standard_Boolean PerformNow = Standard_False);
  //! set edges to avoid being fused <br>
  Standard_EXPORT     void AvoidEdges(const TopTools_IndexedMapOfShape& theMapEdg) ;
  //! set mode to enable concatenation G1 BSpline edges in one <br>
//!  End  Modified  by  IFV  19.04.07 <br>
  Standard_EXPORT     void SetConcatBSpl(const Standard_Boolean theConcatBSpl = Standard_True) ;
  //! returns  all the list of edges to be fused <br>
//!          each list of the map represent a set of connex edges <br>
//!          that can be fused. <br>
  Standard_EXPORT     void Edges(TopTools_DataMapOfIntegerListOfShape& theMapLstEdg) ;
  //! returns all the fused edges. each integer entry in <br>
//!           the   map  corresponds  to  the  integer   in the <br>
//!           DataMapOfIntegerListOfShape  we    get in  method <br>
//!          Edges.   That is to say, to  the list  of edges in <br>
//!          theMapLstEdg(i) corresponds the resulting edge theMapEdge(i) <br>
//! <br>
  Standard_EXPORT     void ResultEdges(TopTools_DataMapOfIntegerShape& theMapEdg) ;
  //! returns the map of modified faces. <br>
  Standard_EXPORT     void Faces(TopTools_DataMapOfShapeShape& theMapFac) ;
  //! returns myShape modified with the list of internal <br>
//!          edges removed from it. <br>
  Standard_EXPORT     TopoDS_Shape& Shape() ;
  //! returns the number of vertices candidate to be removed <br>
  Standard_EXPORT    const Standard_Integer NbVertices() ;
  //! Using  map of list of connex  edges, fuse each list to <br>
//!           one edge and then update myShape <br>
  Standard_EXPORT     void Perform() ;





protected:





private:

  //! build a map of shapes and ancestors, like <br>
//!          TopExp.MapShapesAndAncestors, but we remove duplicate <br>
//!          shapes in list of shapes. <br>
  Standard_EXPORT     void BuildAncestors(const TopoDS_Shape& S,const TopAbs_ShapeEnum TS,const TopAbs_ShapeEnum TA,TopTools_IndexedDataMapOfShapeListOfShape& M) const;
  //! Build the all the lists of edges that are to be fused <br>
  Standard_EXPORT     void BuildListEdges() ;
  //! Build result   fused edges according  to  the list <br>
//!          builtin BuildLisEdges <br>
  Standard_EXPORT     void BuildListResultEdges() ;
  
  Standard_EXPORT     void BuildListConnexEdge(const TopoDS_Shape& theEdge,TopTools_MapOfShape& theMapUniq,TopTools_ListOfShape& theLstEdg) ;
  
  Standard_EXPORT     Standard_Boolean NextConnexEdge(const TopoDS_Vertex& theVertex,const TopoDS_Shape& theEdge,TopoDS_Shape& theEdgeConnex) const;
  
  Standard_EXPORT     Standard_Boolean SameSupport(const TopoDS_Edge& E1,const TopoDS_Edge& E2) const;
  
  Standard_EXPORT     Standard_Boolean UpdatePCurve(const TopoDS_Edge& theOldEdge,TopoDS_Edge& theNewEdge,const TopTools_ListOfShape& theLstEdg) const;


TopoDS_Shape myShape;
Standard_Boolean myShapeDone;
Standard_Boolean myEdgesDone;
Standard_Boolean myResultEdgesDone;
TopTools_IndexedDataMapOfShapeListOfShape myMapVerLstEdg;
TopTools_IndexedDataMapOfShapeListOfShape myMapEdgLstFac;
TopTools_DataMapOfIntegerListOfShape myMapLstEdg;
TopTools_DataMapOfIntegerShape myMapEdg;
TopTools_DataMapOfShapeShape myMapFaces;
Standard_Integer myNbConnexEdge;
TopTools_IndexedMapOfShape myAvoidEdg;
Standard_Boolean myConcatBSpl;


};





// other Inline functions and methods (like "C++: function call" methods)


#endif
