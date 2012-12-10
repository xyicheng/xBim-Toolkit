// This file is generated by WOK (CPPExt).
// Please do not edit this file; modify original file instead.
// The copyright and license terms as defined for the original file apply to 
// this header file considered to be the "object code" form of the original source.

#ifndef _TCollection_BasicMap_HeaderFile
#define _TCollection_BasicMap_HeaderFile

#ifndef _Standard_HeaderFile
#include <Standard.hxx>
#endif
#ifndef _Standard_DefineAlloc_HeaderFile
#include <Standard_DefineAlloc.hxx>
#endif
#ifndef _Standard_Macro_HeaderFile
#include <Standard_Macro.hxx>
#endif

#ifndef _Standard_Boolean_HeaderFile
#include <Standard_Boolean.hxx>
#endif
#ifndef _Standard_Integer_HeaderFile
#include <Standard_Integer.hxx>
#endif
#ifndef _Standard_Address_HeaderFile
#include <Standard_Address.hxx>
#endif
#ifndef _Standard_OStream_HeaderFile
#include <Standard_OStream.hxx>
#endif
class TCollection_BasicMapIterator;


//! Root  class of  all the maps,  provides utilitites <br>
//! for managing the buckets. <br>
//! Maps are dynamically extended data structures where <br>
//!  data is quickly accessed with a key. <br>
//! General properties of maps <br>
//! -   Map items may be (complex) non-unitary data; they <br>
//!   may be difficult to manage with an array. Moreover, the <br>
//!   map allows a data structure to be indexed by complex   data. <br>
//! -   The size of a map is dynamically extended. So a map <br>
//!   may be first dimensioned for a little number of items. <br>
//!   Maps avoid the use of large and quasi-empty arrays. <br>
//! -   The access to a map item is much faster than the one <br>
//!   to a sequence, a list, a queue or a stack item. <br>
//! -   The access time to a map item may be compared with <br>
//!   the one to an array item. First of all, it depends on the <br>
//!   size of the map. It also depends on the quality of a user <br>
//!   redefinable function (the hashing function) to find <br>
//!   quickly where the item is. <br>
//! -   The exploration of a map may be of better performance <br>
//!   than the exploration of an array because the size of the <br>
//!   map is adapted to the number of inserted items. <br>
//!   These properties explain why maps are commonly used as <br>
//! internal data structures for algorithms. <br>
//! Definitions <br>
//! -   A map is a data structure for which data is addressed   by keys. <br>
//! -   Once inserted in the map, a map item is referenced as   an entry of the map. <br>
//! -   Each entry of the map is addressed by a key. Two <br>
//!   different keys address two different entries of the map. <br>
//! -   The position of an entry in the map is called a bucket. <br>
//! -   A map is dimensioned by its number of buckets, i.e. the <br>
//!   maximum number of entries in the map. The <br>
//!   performance of a map is conditioned by the number of buckets. <br>
//! -   The hashing function transforms a key into a bucket <br>
//!   index. The number of values that can be computed by <br>
//!   the hashing function is equal to the number of buckets of the map. <br>
//! -   Both the hashing function and the equality test <br>
//!   between two keys are provided by a hasher object. <br>
//! -   A map may be explored by a map iterator. This <br>
//!   exploration provides only inserted entries in the map <br>
//!   (i.e. non empty buckets). <br>
//!   Collections' generic maps <br>
//! The Collections component provides numerous generic derived maps. <br>
//! -   These maps include automatic management of the <br>
//!   number of buckets: they are automatically resized when <br>
//!   the number of keys exceeds the number of buckets. If <br>
//!   you have a fair idea of the number of items in your map, <br>
//!   you can save on automatic resizing by specifying a <br>
//!   number of buckets at the time of construction, or by using <br>
//! a resizing function. This may be considered for crucial optimization issues. <br>
//! -   Keys, items and hashers are parameters of these generic derived maps. <br>
//! -   TCollection_MapHasher class describes the <br>
//!   functions required by any hasher which is to be used <br>
//!   with a map instantiated from the Collections component. <br>
//! -   An iterator class is automatically instantiated at the <br>
//!   time of instantiation of a map provided by the <br>
//!   Collections component if this map is to be explored <br>
//!   with an iterator. Note that some provided generic maps <br>
//!   are not to be explored with an iterator but with indexes   (indexed maps). <br>
class TCollection_BasicMap  {
public:

  DEFINE_STANDARD_ALLOC

  //! Returns the number of buckets in <me>. <br>
        Standard_Integer NbBuckets() const;
  //! Returns the number of keys already stored in <me>. <br>
//! <br>
        Standard_Integer Extent() const;
  //! Returns  True when the map  contains no keys. <br>
//! This is exactly Extent() == 0. <br>
        Standard_Boolean IsEmpty() const;
  //! Prints  on <S> usefull  statistics  about  the map <br>
//! <me>.  It  can be used  to test the quality of the hashcoding. <br>
  Standard_EXPORT     void Statistics(Standard_OStream& S) const;


friend class TCollection_BasicMapIterator;



protected:

  //! Initialize the map.  Single is  True when the  map <br>
//! uses only one table of buckets. <br>
//! <br>
//! One table  : Map, DataMap <br>
//! Two tables : DoubleMap, IndexedMap, IndexedDataMap <br>
  Standard_EXPORT   TCollection_BasicMap(const Standard_Integer NbBuckets,const Standard_Boolean single);
  //! Tries to resize  the Map with  NbBuckets.  Returns <br>
//! True if  possible, NewBuckts is  the  new nuber of <br>
//! buckets.   data1 and data2  are the new tables  of <br>
//! buckets where the data must be copied. <br>
  Standard_EXPORT     Standard_Boolean BeginResize(const Standard_Integer NbBuckets,Standard_Integer& NewBuckets,Standard_Address& data1,Standard_Address& data2) const;
  //! If  BeginResize was  succesfull  after copying the <br>
//! data to  data1  and data2 this methods  update the <br>
//! tables and destroys the old ones. <br>
  Standard_EXPORT     void EndResize(const Standard_Integer NbBuckets,const Standard_Integer NewBuckets,const Standard_Address data1,const Standard_Address data2) ;
  //! Returns   True  if resizing   the   map should  be <br>
//! considered. <br>
        Standard_Boolean Resizable() const;
  //! Decrement the  extent of the  map. <br>
        void Increment() ;
  //! Decrement the  extent of the  map. <br>
        void Decrement() ;
  //! Destroys the buckets. <br>
  Standard_EXPORT     void Destroy() ;


Standard_Address myData1;
Standard_Address myData2;


private:



Standard_Boolean isDouble;
Standard_Boolean mySaturated;
Standard_Integer myNbBuckets;
Standard_Integer mySize;


};


#include <TCollection_BasicMap.lxx>



// other Inline functions and methods (like "C++: function call" methods)


#endif
