#pragma once
#include "XbimGeometryModel.h"
#include "IXbimGeometryModel.h"
#include "TopoDS_Compound.hxx"
#include "XbimBoundingBox.h"
#include <BRep_Builder.hxx>
#include <BRepGProp.hxx>
#include <GProp_GProps.hxx> 
using namespace System::Collections::Generic;
using namespace  Xbim::Ifc2x3::Extensions;
using namespace Xbim::Common::Exceptions;
namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimGeometryModelCollection : IXbimGeometryModel, IEnumerable<IXbimGeometryModel^>
		{
			TopoDS_Compound* pCompound;
			List<IXbimGeometryModel^>^ shapes;
			bool _hasCurvedEdges;
			bool _isMap;
			Int32 _representationLabel;
			Int32 _surfaceStyleLabel;
			Matrix3D _transform;
		public:
			
			XbimGeometryModelCollection(bool isMap, bool hasCurvedEdges)
			{

				shapes = gcnew List<IXbimGeometryModel^>();
				_isMap=isMap;
				_hasCurvedEdges = hasCurvedEdges;
			};
			
			XbimGeometryModelCollection(IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, ConcurrentDictionary<int,Object^>^ maps)
			{
				shapes = gcnew List<IXbimGeometryModel^>();
				_isMap=true;
				_hasCurvedEdges = false;

				if(origin !=nullptr)
				{
					if(dynamic_cast<IfcAxis2Placement3D^>(origin))
						_transform = Axis2Placement3DExtensions::ToMatrix3D((IfcAxis2Placement3D^)origin,maps);
					else if(dynamic_cast<IfcAxis2Placement2D^>(origin))
						_transform = Axis2Placement2DExtensions::ToMatrix3D((IfcAxis2Placement2D^)origin,maps);
					else
						throw gcnew XbimGeometryException("Invalid IfcAxis2Placement argument");

				}

			if(transform!=nullptr)
				_transform= Matrix3D::Multiply( CartesianTransformationOperatorExtensions::ToMatrix3D(transform, maps),_transform);
			};


			XbimGeometryModelCollection(const TopoDS_Compound & pComp, bool hasCurves,bool isMap)
			{

				shapes = gcnew List<IXbimGeometryModel^>();
				_hasCurvedEdges = hasCurves;
				_isMap=isMap;
			
			};
			XbimGeometryModelCollection(const TopoDS_Compound & pComp, List<IXbimGeometryModel^>^ features, bool hasCurves,bool isMap)
			{
				shapes = gcnew List<IXbimGeometryModel^>(features);
			    _hasCurvedEdges = hasCurves;
				_isMap=isMap;
			};

			~XbimGeometryModelCollection()
			{
				InstanceCleanup();
			}

			!XbimGeometryModelCollection()
			{
				InstanceCleanup();
			}
			void InstanceCleanup()
			{   
				int temp = System::Threading::Interlocked::Exchange((int)(void*)pCompound, 0);
				if(temp!=0)
				{
					if (pCompound)
					{
						delete pCompound;
						pCompound=0;
						System::GC::SuppressFinalize(this);
					}
				}
			}

			property Matrix3D Transform
			{
				Matrix3D get()
				{
					return _transform;
				}
			}
			// IEnumerable<IXbimGeometryModel^> Members
			
virtual property XbimLocation ^ Location 
			{
				XbimLocation ^ get()
				{
					//return gcnew XbimLocation(pCompound->Location());
					throw gcnew NotImplementedException("Location needs to be implemented");
				}
				void set(XbimLocation ^ location)
				{
					//pCompound->Location(*(location->Handle));;
					throw gcnew NotImplementedException("Location needs to be implemented");
				}
			};

			virtual property double Volume
			{
				double get()
				{
					/*GProp_GProps System;
					BRepGProp::VolumeProperties(*pCompound, System, Standard_True);
					return System.Mass();*/
					double volume = 0;
					for each(IXbimGeometryModel^ geom in shapes)
						volume+=geom->Volume;
					return volume;
				}
			}
			
			virtual property Int32 RepresentationLabel
			{
				Int32 get(){return _representationLabel; }
				void set(Int32 value){ _representationLabel=value; }
			}

			virtual property Int32 SurfaceStyleLabel
			{
				Int32 get(){return _surfaceStyleLabel; }
				void set(Int32 value){ _surfaceStyleLabel=value; }
			}

			virtual XbimBoundingBox^ GetBoundingBox(bool precise)
			{
				
				XbimBoundingBox^ bb = nullptr;
				for each(IXbimGeometryModel^ geom in shapes)
				{
					if(bb == nullptr)
						bb = geom->GetBoundingBox(precise);
					else
						bb->Add(geom->GetBoundingBox(precise));

				}
				return bb;
			};

			virtual IEnumerator<IXbimGeometryModel^>^ GetEnumerator()
			{

				return shapes->GetEnumerator();
			}
			virtual System::Collections::IEnumerator^ GetEnumerator2() sealed = System::Collections::IEnumerable::GetEnumerator
			{
				return shapes->GetEnumerator();
			}

			virtual property bool HasCurvedEdges
			{
				virtual bool get()
				{
					if(_hasCurvedEdges) return true;
					for each(IXbimGeometryModel^ gm in this) //if any not other return false
					{
						if(gm->HasCurvedEdges) return true;
					}
					return false;
				}
			}

			property bool IsMap
			{
				bool get()
				{
					return _isMap;
				}
			}
			void Add(IXbimGeometryModel^ shape)
			{
				shapes->Add(shape);
				if(pCompound)
				{
					delete pCompound;
					pCompound=0;
				}
			}

			IXbimGeometryModel^ Solidify();
			

			/*Interfaces*/
			virtual property TopoDS_Shape* Handle
			{
				//
				
				TopoDS_Shape* get()
				{
					if(!pCompound)
					{
						BRep_Builder b;
						pCompound = new TopoDS_Compound();;
						b.MakeCompound(*pCompound);
						for each(IXbimGeometryModel^ shape in shapes)
							b.Add(*pCompound, *(shape->Handle));
					}
					return pCompound;
				};
			}
			virtual IXbimGeometryModel^ Cut(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ Union(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ Intersection(IXbimGeometryModel^ shape);
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection, Matrix3D transform);
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection);
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals);
			virtual List<XbimTriangulatedModel^>^Mesh();
			virtual IXbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement);
			virtual void Move(TopLoc_Location location);
		};

	}
}