#pragma once
#include "XbimGeometryModel.h"

#include "TopoDS_Compound.hxx"
#include "XbimBoundingBox.h"
#include <BRep_Builder.hxx>
#include <BRepGProp.hxx>
#include <GProp_GProps.hxx> 
using namespace System::Collections::Generic;
namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimGeometryModelCollection : IXbimGeometryModel, IEnumerable<IXbimGeometryModel^>
		{
			TopoDS_Compound* pCompound;
			List<IXbimGeometryModel^>^ shapes;
			bool _hasCurvedEdges;
		public:
			
			XbimGeometryModelCollection()
			{
				/*pCompound = new TopoDS_Compound();
				BRep_Builder b;
				b.MakeCompound(*pCompound);*/
				shapes = gcnew List<IXbimGeometryModel^>();
			
			};

			XbimGeometryModelCollection(const TopoDS_Compound & pComp, bool hasCurves)
			{
				/*pCompound = new TopoDS_Compound();
				*pCompound = pComp;*/
				shapes = gcnew List<IXbimGeometryModel^>();
				_hasCurvedEdges = hasCurves;
			
			};
			XbimGeometryModelCollection(const TopoDS_Compound & pComp, List<IXbimGeometryModel^>^ features, bool hasCurves)
			{
				/*pCompound = new TopoDS_Compound();
				*pCompound = pComp;*/
				shapes = gcnew List<IXbimGeometryModel^>(features);
			    _hasCurvedEdges = hasCurves;
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
			virtual XbimTriangulatedModelStream^ Mesh(bool withNormals, double deflection, Matrix3D transform);
			virtual XbimTriangulatedModelStream^ Mesh(bool withNormals, double deflection);
			virtual XbimTriangulatedModelStream^ Mesh(bool withNormals);
			virtual XbimTriangulatedModelStream^ Mesh();
			virtual IXbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement);
			
		};

	}
}