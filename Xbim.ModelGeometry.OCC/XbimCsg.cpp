#include "StdAfx.h"
#include "XbimCsg.h"
#include <carve/csg_triangulator.hpp>
using namespace System;
using namespace System::Threading;
using namespace Xbim::Common::Exceptions;
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			XbimCsg::XbimCsg(double precision)
			{
				_csg = new carve::csg::CSG(precision);
				
			}

			void XbimCsg::InstanceCleanup()
			{  
				IntPtr temp = System::Threading::Interlocked::Exchange(IntPtr(_csg), IntPtr(0));
				if(temp!=IntPtr(0))
				{
					delete _csg;
					_csg=nullptr;
					System::GC::SuppressFinalize(this);
				}
			}
			//Adds all the geometries together
			XbimPolyhedron^ XbimCsg::Combine(XbimPolyhedron^ a, XbimPolyhedron^ b)
			{
				try
				{
					meshset_t* cut =  _csg->compute( a->MeshSet,b->MeshSet,carve::csg::CSG::ALL,NULL,carve::csg::CSG::CLASSIFY_NORMAL);
					GC::KeepAlive(a);
					GC::KeepAlive(b);
					GC::KeepAlive(this);
					if(cut!=nullptr)
						return gcnew XbimPolyhedron(cut,a->RepresentationLabel,a->SurfaceStyleLabel);
					else
						throw gcnew XbimGeometryException("XbimCsg::Combine did not result in a valid shape");		
				}
				catch (carve::exception ce) 
				{
					throw gcnew XbimGeometryException("XbimCsg::Combine failed due to Boolean Operation Exception");						
				}
				catch (System::Runtime::InteropServices::SEHException^ ) //these should never happen, raise an error
				{
					throw gcnew XbimGeometryException("XbimCsg::Combine failed due to Interop Exception");
				}
			}
				//Unions both the geometries together
			XbimPolyhedron^ XbimCsg::Union(XbimPolyhedron^ a, XbimPolyhedron^ b)
			{
				try
				{
					meshset_t* join =  _csg->compute( a->MeshSet,b->MeshSet,carve::csg::CSG::UNION,NULL,carve::csg::CSG::CLASSIFY_NORMAL);
					GC::KeepAlive(a);
					GC::KeepAlive(b);
					GC::KeepAlive(this);
					if(join!=nullptr)
						return gcnew XbimPolyhedron(join, a->RepresentationLabel, a->SurfaceStyleLabel);
					else
						throw gcnew XbimGeometryException("XbimCsg::Union did not result in a valid shape");		
				}
				catch (carve::exception ce) 
				{

					throw gcnew XbimGeometryException("XbimCsg::Union failed due to Boolean Operation Exception");						
				}
				catch (System::Runtime::InteropServices::SEHException^ ) //these should never happen, raise an error
				{
					throw gcnew XbimGeometryException("XbimCsg::Union failed due to Interop Exception");
				}
				catch(...)
				{
					throw gcnew XbimGeometryException("XbimCsg::Union failed due to Unexpected Exception");
				}
			}
			XbimPolyhedron^ XbimCsg::Subtract(XbimPolyhedron^ a, XbimPolyhedron^ b)
			{
				try
				{
					
					meshset_t* cut =  _csg->compute( a->MeshSet,b->MeshSet,carve::csg::CSG::A_MINUS_B,NULL,carve::csg::CSG::CLASSIFY_NORMAL);
					GC::KeepAlive(a);
					GC::KeepAlive(b);
					GC::KeepAlive(this);
					if(cut!=nullptr)
						return gcnew XbimPolyhedron(cut, a->RepresentationLabel,a->SurfaceStyleLabel);
					else
						throw gcnew XbimGeometryException("XbimCsg::Subtract did not result in a valid shape");		
				}
				catch (carve::exception ce) 
				{

					String^ err = gcnew String(ce.str().c_str());
					a->WritePly("a",true);
					b->WritePly("b",true);
					throw gcnew XbimGeometryException("XbimCsg::Subtract error, " + err);						
				}
				catch (System::Runtime::InteropServices::SEHException^ ex) //these should never happen, raise an error
				{
					throw gcnew XbimGeometryException("XbimCsg::Subtract error, " + ex->Message);
				}
				catch(...)
				{
					throw gcnew XbimGeometryException("XbimCsg::Subtract, Unexpected Error");
				}
			}

			//in a or b, but not both
			XbimPolyhedron^ XbimCsg::SymetricDifference(XbimPolyhedron^ a, XbimPolyhedron^ b)
			{
				try
				{
					meshset_t* cut =  _csg->compute( a->MeshSet,b->MeshSet,carve::csg::CSG::SYMMETRIC_DIFFERENCE,NULL,carve::csg::CSG::CLASSIFY_NORMAL);
					GC::KeepAlive(a);
					GC::KeepAlive(b);
					GC::KeepAlive(this);
					if(cut!=nullptr)
						return gcnew XbimPolyhedron(cut, a->RepresentationLabel,a->SurfaceStyleLabel);
					else
						throw gcnew XbimGeometryException("XbimCsg::SymetricDifference did not result in a valid shape");		
				}
				catch (carve::exception ce) 
				{
					throw gcnew XbimGeometryException("XbimCsg::SymetricDifference failed due to Boolean Operation Exception");						
				}
				catch (System::Runtime::InteropServices::SEHException^ ) //these should never happen, raise an error
				{
					throw gcnew XbimGeometryException("XbimCsg::SymetricDifference failed due to Interop Exception");
				}
				catch(...)
				{
					throw gcnew XbimGeometryException("XbimCsg::SymetricDifference failed due to Unexpected Exception");
				}
			}

			XbimPolyhedron^ XbimCsg::Intersection(XbimPolyhedron^ a, XbimPolyhedron^ b)
			{
				try
				{
						
					meshset_t* cut =  _csg->compute( a->MeshSet,b->MeshSet,carve::csg::CSG::INTERSECTION,NULL,carve::csg::CSG::CLASSIFY_NORMAL);
					GC::KeepAlive(a);
					GC::KeepAlive(b);
					GC::KeepAlive(this);
					if(cut!=nullptr)
						return gcnew XbimPolyhedron(cut, a->RepresentationLabel,a->SurfaceStyleLabel);
					else
						throw gcnew XbimGeometryException("XbimCsg::Intersection did not result in a valid shape");		
				}
				catch (carve::exception ce) 
				{
					String^ err = gcnew String(ce.str().c_str());
					throw gcnew XbimGeometryException("XbimCsg::Intersection error, " + err);						
				}
				catch (System::Runtime::InteropServices::SEHException^ ex) //these should never happen, raise an error
				{
					throw gcnew XbimGeometryException("XbimCsg::Intersection error, " + ex->Message);
				}
				catch(...)
				{
					throw gcnew XbimGeometryException("XbimCsg::Intersection, Unexpected Error");
				}
			}
		}
	}
}