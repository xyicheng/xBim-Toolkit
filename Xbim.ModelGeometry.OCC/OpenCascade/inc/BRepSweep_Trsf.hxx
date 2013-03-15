// This file is generated by WOK (CPPExt).
// Please do not edit this file; modify original file instead.
// The copyright and license terms as defined for the original file apply to 
// this header file considered to be the "object code" form of the original source.

#ifndef _BRepSweep_Trsf_HeaderFile
#define _BRepSweep_Trsf_HeaderFile

#ifndef _Standard_HeaderFile
#include <Standard.hxx>
#endif
#ifndef _Standard_DefineAlloc_HeaderFile
#include <Standard_DefineAlloc.hxx>
#endif
#ifndef _Standard_Macro_HeaderFile
#include <Standard_Macro.hxx>
#endif

#ifndef _TopLoc_Location_HeaderFile
#include <TopLoc_Location.hxx>
#endif
#ifndef _Standard_Boolean_HeaderFile
#include <Standard_Boolean.hxx>
#endif
#ifndef _BRepSweep_NumLinearRegularSweep_HeaderFile
#include <BRepSweep_NumLinearRegularSweep.hxx>
#endif
#ifndef _TopAbs_Orientation_HeaderFile
#include <TopAbs_Orientation.hxx>
#endif
class BRep_Builder;
class TopoDS_Shape;
class Sweep_NumShape;
class TopLoc_Location;


//! This class is inherited from NumLinearRegularSweep <br>
//!          to  implement the  simple   swept primitives built <br>
//!          moving a Shape with a Trsf.  It  often is possible <br>
//!          to  build  the constructed subshapes  by  a simple <br>
//!          move of the  generating subshapes (shared topology <br>
//!          and geometry).   So two  ways of construction  are <br>
//!          proposed : <br>
//! <br>
class BRepSweep_Trsf  : public BRepSweep_NumLinearRegularSweep {
public:

  DEFINE_STANDARD_ALLOC

  
  Standard_EXPORT   virtual  void Delete() ;
Standard_EXPORT virtual ~BRepSweep_Trsf(){Delete() ; }
  //! ends  the  construction  of the   swept  primitive <br>
//!          calling the virtual geometric functions that can't <br>
//!          be called in the initialize. <br>
  Standard_EXPORT     void Init() ;
  //! function called to analize the way of construction <br>
//!          of the shapes generated by aGenS and aDirV. <br>
  Standard_EXPORT     Standard_Boolean Process(const TopoDS_Shape& aGenS,const Sweep_NumShape& aDirV) ;
  //! Builds the vertex addressed by [aGenV,aDirV], with its <br>
//!          geometric part, but without subcomponents. <br>
  Standard_EXPORT   virtual  TopoDS_Shape MakeEmptyVertex(const TopoDS_Shape& aGenV,const Sweep_NumShape& aDirV)  = 0;
  //! Builds the edge addressed by [aGenV,aDirE], with its <br>
//!          geometric part, but without subcomponents. <br>
  Standard_EXPORT   virtual  TopoDS_Shape MakeEmptyDirectingEdge(const TopoDS_Shape& aGenV,const Sweep_NumShape& aDirE)  = 0;
  //! Builds the edge addressed by [aGenE,aDirV], with its <br>
//!          geometric part, but without subcomponents. <br>
  Standard_EXPORT   virtual  TopoDS_Shape MakeEmptyGeneratingEdge(const TopoDS_Shape& aGenE,const Sweep_NumShape& aDirV)  = 0;
  //! Sets the  parameters of the new  vertex  on the new <br>
//!          face. The new face and  new vertex where generated <br>
//!          from aGenF, aGenV and aDirV . <br>
  Standard_EXPORT   virtual  void SetParameters(const TopoDS_Shape& aNewFace,TopoDS_Shape& aNewVertex,const TopoDS_Shape& aGenF,const TopoDS_Shape& aGenV,const Sweep_NumShape& aDirV)  = 0;
  //! Sets the  parameter of the new  vertex  on the new <br>
//!          edge. The new edge and  new vertex where generated <br>
//!          from aGenV aDirE, and aDirV. <br>
  Standard_EXPORT   virtual  void SetDirectingParameter(const TopoDS_Shape& aNewEdge,TopoDS_Shape& aNewVertex,const TopoDS_Shape& aGenV,const Sweep_NumShape& aDirE,const Sweep_NumShape& aDirV)  = 0;
  //! Sets the  parameter of the new  vertex  on the new <br>
//!          edge. The new edge and  new vertex where generated <br>
//!          from aGenE, aGenV and aDirV . <br>
  Standard_EXPORT   virtual  void SetGeneratingParameter(const TopoDS_Shape& aNewEdge,TopoDS_Shape& aNewVertex,const TopoDS_Shape& aGenE,const TopoDS_Shape& aGenV,const Sweep_NumShape& aDirV)  = 0;
  //! Builds  the face addressed  by [aGenS,aDirS], with <br>
//!          its geometric part, but without subcomponents. The <br>
//!          couple aGenS, aDirS can be  a "generating face and <br>
//!          a  directing vertex" or "a   generating edge and a <br>
//!          directing  edge". <br>
  Standard_EXPORT   virtual  TopoDS_Shape MakeEmptyFace(const TopoDS_Shape& aGenS,const Sweep_NumShape& aDirS)  = 0;
  //! Sets the PCurve for a new edge on a new face. The <br>
//!          new edge and  the  new face were generated  using <br>
//!          aGenF, aGenE and aDirV. <br>
  Standard_EXPORT   virtual  void SetPCurve(const TopoDS_Shape& aNewFace,TopoDS_Shape& aNewEdge,const TopoDS_Shape& aGenF,const TopoDS_Shape& aGenE,const Sweep_NumShape& aDirV,const TopAbs_Orientation orien)  = 0;
  //! Sets the PCurve for a new edge on a new face. The <br>
//!          new edge and  the  new face were generated  using <br>
//!          aGenE, aDirE and aDirV. <br>
  Standard_EXPORT   virtual  void SetGeneratingPCurve(const TopoDS_Shape& aNewFace,TopoDS_Shape& aNewEdge,const TopoDS_Shape& aGenE,const Sweep_NumShape& aDirE,const Sweep_NumShape& aDirV,const TopAbs_Orientation orien)  = 0;
  //! Sets the PCurve for a new edge on a new face. The <br>
//!          new edge and  the  new face were generated  using <br>
//!          aGenE, aDirE and aGenV. <br>
  Standard_EXPORT   virtual  void SetDirectingPCurve(const TopoDS_Shape& aNewFace,TopoDS_Shape& aNewEdge,const TopoDS_Shape& aGenE,const TopoDS_Shape& aGenV,const Sweep_NumShape& aDirE,const TopAbs_Orientation orien)  = 0;
  //! Returns   true   if  aNewSubShape    (addressed by <br>
//!          aSubGenS  and aDirS)  must  be added  in aNewShape <br>
//!          (addressed by aGenS and aDirS). <br>
  Standard_EXPORT   virtual  Standard_Boolean GGDShapeIsToAdd(const TopoDS_Shape& aNewShape,const TopoDS_Shape& aNewSubShape,const TopoDS_Shape& aGenS,const TopoDS_Shape& aSubGenS,const Sweep_NumShape& aDirS) const = 0;
  //! Returns   true   if  aNewSubShape    (addressed by <br>
//!          aGenS  and aSubDirS)  must  be added  in aNewShape <br>
//!          (addressed by aGenS and aDirS). <br>
  Standard_EXPORT   virtual  Standard_Boolean GDDShapeIsToAdd(const TopoDS_Shape& aNewShape,const TopoDS_Shape& aNewSubShape,const TopoDS_Shape& aGenS,const Sweep_NumShape& aDirS,const Sweep_NumShape& aSubDirS) const = 0;
  //! In  some  particular  cases  the   topology  of  a <br>
//!          generated  face must be  composed  of  independant <br>
//!          closed wires,  in this case  this function returns <br>
//!          true. <br>
  Standard_EXPORT   virtual  Standard_Boolean SeparatedWires(const TopoDS_Shape& aNewShape,const TopoDS_Shape& aNewSubShape,const TopoDS_Shape& aGenS,const TopoDS_Shape& aSubGenS,const Sweep_NumShape& aDirS) const = 0;
  //! Returns true   if aDirS   and aGenS  addresses   a <br>
//!          resulting Shape. In some  specific cases the shape <br>
//!          can  be    geometrically   inexsistant,  then this <br>
//!          function returns false. <br>
  Standard_EXPORT   virtual  Standard_Boolean HasShape(const TopoDS_Shape& aGenS,const Sweep_NumShape& aDirS) const = 0;
  //! Returns  true if  the geometry   of  aGenS is  not <br>
//!          modified by the trsf of the BRepSweep Trsf. <br>
  Standard_EXPORT   virtual  Standard_Boolean IsInvariant(const TopoDS_Shape& aGenS) const = 0;
  //! Called to propagate the continuity of  every vertex <br>
//!          between two edges of the  generating wire  aGenS on <br>
//!          the generated edge and faces. <br>
  Standard_EXPORT     void SetContinuity(const TopoDS_Shape& aGenS,const Sweep_NumShape& aDirS) ;





protected:

  //! Initialize  the Trsf BrepSweep, if  aCopy  is true <br>
//!          the  basis elements  are    shared  as   often  as <br>
//!          possible, else everything is copied. <br>
//! <br>
  Standard_EXPORT   BRepSweep_Trsf(const BRep_Builder& aBuilder,const TopoDS_Shape& aGenShape,const Sweep_NumShape& aDirWire,const TopLoc_Location& aLocation,const Standard_Boolean aCopy);


TopLoc_Location myLocation;
Standard_Boolean myCopy;


private:





};





// other Inline functions and methods (like "C++: function call" methods)


#endif