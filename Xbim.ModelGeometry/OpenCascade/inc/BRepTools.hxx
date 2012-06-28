// This file is generated by WOK (CPPExt).
// Please do not edit this file; modify original file instead.
// The copyright and license terms as defined for the original file apply to 
// this header file considered to be the "object code" form of the original source.

#ifndef _BRepTools_HeaderFile
#define _BRepTools_HeaderFile

#ifndef _Standard_HeaderFile
#include <Standard.hxx>
#endif
#ifndef _Standard_Macro_HeaderFile
#include <Standard_Macro.hxx>
#endif

#ifndef _Standard_Real_HeaderFile
#include <Standard_Real.hxx>
#endif
#ifndef _Standard_Boolean_HeaderFile
#include <Standard_Boolean.hxx>
#endif
#ifndef _Standard_OStream_HeaderFile
#include <Standard_OStream.hxx>
#endif
#ifndef _Handle_Message_ProgressIndicator_HeaderFile
#include <Handle_Message_ProgressIndicator.hxx>
#endif
#ifndef _Standard_IStream_HeaderFile
#include <Standard_IStream.hxx>
#endif
#ifndef _Standard_CString_HeaderFile
#include <Standard_CString.hxx>
#endif
class TopoDS_Face;
class TopoDS_Wire;
class TopoDS_Edge;
class Bnd_Box2d;
class TopoDS_Vertex;
class TopoDS_Shell;
class TopoDS_Solid;
class TopoDS_CompSolid;
class TopoDS_Compound;
class TopoDS_Shape;
class TopTools_IndexedMapOfShape;
class Message_ProgressIndicator;
class BRep_Builder;
class BRepTools_WireExplorer;
class BRepTools_Modification;
class BRepTools_Modifier;
class BRepTools_TrsfModification;
class BRepTools_NurbsConvertModification;
class BRepTools_GTrsfModification;
class BRepTools_Substitution;
class BRepTools_Quilt;
class BRepTools_ShapeSet;
class BRepTools_ReShape;
class BRepTools_MapOfVertexPnt2d;
class BRepTools_DataMapNodeOfMapOfVertexPnt2d;
class BRepTools_DataMapIteratorOfMapOfVertexPnt2d;


//! The BRepTools package provides  utilities for BRep <br>
//!          data structures. <br>
//! <br>
//!          * WireExplorer : A tool to explore the topology of <br>
//!          a wire in the order of the edges. <br>
//! <br>
//!          * ShapeSet :  Tools used for  dumping, writing and <br>
//!          reading. <br>
//! <br>
//!          * UVBounds : Methods to compute the  limits of the <br>
//!          boundary  of a  face,  a wire or   an edge in  the <br>
//!          parametric space of a face. <br>
//! <br>
//!          *  Update : Methods  to call when   a topology has <br>
//!          been created to compute all missing data. <br>
//! <br>
//!          * UpdateFaceUVPoints  :  Method to  update  the UV <br>
//!          points stored   with  the edges   on a face.  This <br>
//!          method ensure that connected  edges  have the same <br>
//!          UV point on their common extremity. <br>
//! <br>
//!          * Compare : Method to compare two vertices. <br>
//! <br>
//!          * Compare : Method to compare two edges. <br>
//! <br>
//!          * OuterWire : A method to find the outer wire of a <br>
//!          face. <br>
//! <br>
//!          * OuterShell : A method to find the outer shell of <br>
//!          a solid. <br>
//! <br>
//!          * Map3DEdges : A method to map all the 3D Edges of <br>
//!          a Shape. <br>
//! <br>
//!          * Dump : A method to dump a BRep object. <br>
//! <br>
class BRepTools  {
public:

  void* operator new(size_t,void* anAddress) 
  {
    return anAddress;
  }
  void* operator new(size_t size) 
  {
    return Standard::Allocate(size); 
  }
  void  operator delete(void *anAddress) 
  {
    if (anAddress) Standard::Free((Standard_Address&)anAddress); 
  }

  //! Returns in UMin,  UMax, VMin,  VMax  the  bounding <br>
//!          values in the parametric space of F. <br>
  Standard_EXPORT   static  void UVBounds(const TopoDS_Face& F,Standard_Real& UMin,Standard_Real& UMax,Standard_Real& VMin,Standard_Real& VMax) ;
  //! Returns in UMin,  UMax, VMin,  VMax  the  bounding <br>
//!          values of the wire in the parametric space of F. <br>
  Standard_EXPORT   static  void UVBounds(const TopoDS_Face& F,const TopoDS_Wire& W,Standard_Real& UMin,Standard_Real& UMax,Standard_Real& VMin,Standard_Real& VMax) ;
  //! Returns in UMin,  UMax, VMin,  VMax  the  bounding <br>
//!          values of the edge in the parametric space of F. <br>
  Standard_EXPORT   static  void UVBounds(const TopoDS_Face& F,const TopoDS_Edge& E,Standard_Real& UMin,Standard_Real& UMax,Standard_Real& VMin,Standard_Real& VMax) ;
  //! Adds  to  the box <B>  the bounding values in  the <br>
//!          parametric space of F. <br>
  Standard_EXPORT   static  void AddUVBounds(const TopoDS_Face& F,Bnd_Box2d& B) ;
  //! Adds  to the box  <B>  the bounding  values of the <br>
//!          wire in the parametric space of F. <br>
  Standard_EXPORT   static  void AddUVBounds(const TopoDS_Face& F,const TopoDS_Wire& W,Bnd_Box2d& B) ;
  //! Adds to  the box <B>  the  bounding values  of the <br>
//!          edge in the parametric space of F. <br>
  Standard_EXPORT   static  void AddUVBounds(const TopoDS_Face& F,const TopoDS_Edge& E,Bnd_Box2d& B) ;
  //! Update a vertex (nothing is done) <br>
  Standard_EXPORT   static  void Update(const TopoDS_Vertex& V) ;
  //! Update an edge, compute 2d bounding boxes. <br>
  Standard_EXPORT   static  void Update(const TopoDS_Edge& E) ;
  //! Update a wire (nothing is done) <br>
  Standard_EXPORT   static  void Update(const TopoDS_Wire& W) ;
  //! Update a Face, update UV points. <br>
  Standard_EXPORT   static  void Update(const TopoDS_Face& F) ;
  //! Update a shell (nothing is done) <br>
  Standard_EXPORT   static  void Update(const TopoDS_Shell& S) ;
  //! Update a solid (nothing is done) <br>
  Standard_EXPORT   static  void Update(const TopoDS_Solid& S) ;
  //! Update a composite solid (nothing is done) <br>
  Standard_EXPORT   static  void Update(const TopoDS_CompSolid& C) ;
  //! Update a compound (nothing is done) <br>
  Standard_EXPORT   static  void Update(const TopoDS_Compound& C) ;
  //! Update a shape, call the corect update. <br>
  Standard_EXPORT   static  void Update(const TopoDS_Shape& S) ;
  //! For  all the edges  of the face  <F> reset  the UV <br>
//!          points to  ensure that  connected  faces  have the <br>
//!          same point at there common extremity. <br>
  Standard_EXPORT   static  void UpdateFaceUVPoints(const TopoDS_Face& F) ;
  //! Removes all the triangulations of the faces of <S> <br>
//!          and removes all polygons on triangulations of the <br>
//!          edges. <br>
  Standard_EXPORT   static  void Clean(const TopoDS_Shape& S) ;
  //! Removes all the pcurves of the edges of <S> that <br>
//!          refer to surfaces not belonging to any face of <S> <br>
  Standard_EXPORT   static  void RemoveUnusedPCurves(const TopoDS_Shape& S) ;
  //! verifies that each face from the shape <S> has got <br>
//!          a triangulation  with a  deflection <= deflec  and <br>
//!          the edges a discretisation on  this triangulation. <br>
  Standard_EXPORT   static  Standard_Boolean Triangulation(const TopoDS_Shape& S,const Standard_Real deflec) ;
  //! Returns  True if  the    distance between the  two <br>
//!          vertices is lower than their tolerance. <br>
  Standard_EXPORT   static  Standard_Boolean Compare(const TopoDS_Vertex& V1,const TopoDS_Vertex& V2) ;
  //! Returns  True if  the    distance between the  two <br>
//!          edges is lower than their tolerance. <br>
  Standard_EXPORT   static  Standard_Boolean Compare(const TopoDS_Edge& E1,const TopoDS_Edge& E2) ;
  //! Returns the outer most wire of <F>. Returns a Null <br>
//!          wire if <F> has no wires. <br>
  Standard_EXPORT   static  TopoDS_Wire OuterWire(const TopoDS_Face& F) ;
  //! Returns the outer most shell of <S>. Returns a Null <br>
//!          wire if <S> has no shells. <br>
  Standard_EXPORT   static  TopoDS_Shell OuterShell(const TopoDS_Solid& S) ;
  //! Stores in the map  <M> all the 3D topology edges <br>
//!          of <S>. <br>
  Standard_EXPORT   static  void Map3DEdges(const TopoDS_Shape& S,TopTools_IndexedMapOfShape& M) ;
  
  Standard_EXPORT   static  Standard_Boolean IsReallyClosed(const TopoDS_Edge& E,const TopoDS_Face& F) ;
  //! Dumps the topological structure and the geometry <br>
//!          of <Sh> on the stream <S>. <br>
  Standard_EXPORT   static  void Dump(const TopoDS_Shape& Sh,Standard_OStream& S) ;
  //! Writes <Sh> on <S> in an ASCII format. <br>
  Standard_EXPORT   static  void Write(const TopoDS_Shape& Sh,Standard_OStream& S,const Handle(Message_ProgressIndicator)& PR = NULL) ;
  //! Reads a Shape  from <S> in  returns it in  <Sh>. <br>
//!          <B> is used to build the shape. <br>
  Standard_EXPORT   static  void Read(TopoDS_Shape& Sh,Standard_IStream& S,const BRep_Builder& B,const Handle(Message_ProgressIndicator)& PR = NULL) ;
  //! Writes <Sh> in <File>. <br>
  Standard_EXPORT   static  Standard_Boolean Write(const TopoDS_Shape& Sh,const Standard_CString File,const Handle(Message_ProgressIndicator)& PR = NULL) ;
  //! Reads a Shape  from <File>,  returns it in  <Sh>. <br>
//!          <B> is used to build the shape. <br>
  Standard_EXPORT   static  Standard_Boolean Read(TopoDS_Shape& Sh,const Standard_CString File,const BRep_Builder& B,const Handle(Message_ProgressIndicator)& PR = NULL) ;





protected:





private:




friend class BRepTools_WireExplorer;
friend class BRepTools_Modification;
friend class BRepTools_Modifier;
friend class BRepTools_TrsfModification;
friend class BRepTools_NurbsConvertModification;
friend class BRepTools_GTrsfModification;
friend class BRepTools_Substitution;
friend class BRepTools_Quilt;
friend class BRepTools_ShapeSet;
friend class BRepTools_ReShape;
friend class BRepTools_MapOfVertexPnt2d;
friend class BRepTools_DataMapNodeOfMapOfVertexPnt2d;
friend class BRepTools_DataMapIteratorOfMapOfVertexPnt2d;

};





// other Inline functions and methods (like "C++: function call" methods)


#endif
