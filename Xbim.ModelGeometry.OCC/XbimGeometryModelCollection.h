#pragma once
#include "XbimGeometryModel.h"
#include "XbimGeometryModel.h"
#include "TopoDS_Compound.hxx"
#include "XbimBoundingBox.h"
#include <BRep_Builder.hxx>
#include <BRepGProp.hxx>
#include <GProp_GProps.hxx> 
using namespace System::Collections::Generic;
using namespace  Xbim::Ifc2x3::Extensions;
using namespace Xbim::Common::Exceptions;
using namespace Xbim::Common::Geometry;
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			public ref class XbimGeometryModelCollection : XbimGeometryModel, IEnumerable<XbimGeometryModel^>
			{
				TopoDS_Compound* pCompound;
				List<XbimGeometryModel^>^ shapes;
				bool _hasCurvedEdges;
				XbimMatrix3D _transform;
			public:

				XbimGeometryModelCollection(bool hasCurvedEdges)
				{

					shapes = gcnew List<XbimGeometryModel^>();

					_hasCurvedEdges = hasCurvedEdges;
				};

				XbimGeometryModelCollection(IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, ConcurrentDictionary<int,Object^>^ maps)
				{
					shapes = gcnew List<XbimGeometryModel^>();

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
						_transform= XbimMatrix3D::Multiply( CartesianTransformationOperatorExtensions::ToMatrix3D(transform, maps),_transform);
				};


				XbimGeometryModelCollection(const TopoDS_Compound & pComp, bool hasCurves,bool isMap)
				{

					shapes = gcnew List<XbimGeometryModel^>();
					_hasCurvedEdges = hasCurves;


				};
				XbimGeometryModelCollection(const TopoDS_Compound & pComp, List<XbimGeometryModel^>^ features, bool hasCurves,bool isMap)
				{
					shapes = gcnew List<XbimGeometryModel^>(features);
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
					shapes=nullptr;
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

				virtual property XbimMatrix3D Transform
				{
					XbimMatrix3D get() override
					{
						return _transform;
					}
				}
				// IEnumerable<XbimGeometryModel^> Members

				virtual property XbimLocation ^ Location 
				{
					XbimLocation ^ get() override
					{
						//return gcnew XbimLocation(pCompound->Location());
						throw gcnew NotImplementedException("Location needs to be implemented");
					}
					void set(XbimLocation ^ location) override
					{
						//pCompound->Location(*(location->Handle));;
						throw gcnew NotImplementedException("Location needs to be implemented");
					}
				};

				virtual property double Volume
				{
					double get() override
					{
						double volume = 0;
						for each(XbimGeometryModel^ geom in shapes)
						{
							volume += geom->Volume;
						}
						return volume;
					}
				}


				virtual XbimRect3D GetBoundingBox() override
				{

					XbimRect3D bb = XbimRect3D::Empty;
					for each(XbimGeometryModel^ geom in shapes)
					{
						if(bb.IsEmpty)
							bb = geom->GetBoundingBox();
						else
							bb.Union(geom->GetBoundingBox());

					}
					return bb;
				};

				virtual IEnumerator<XbimGeometryModel^>^ GetEnumerator()
				{

					return shapes->GetEnumerator();
				}
				virtual System::Collections::IEnumerator^ GetEnumerator2() sealed = System::Collections::IEnumerable::GetEnumerator
				{
					return shapes->GetEnumerator();
				}

				virtual property bool HasCurvedEdges
				{
					virtual bool get() override
					{
						if(_hasCurvedEdges) return true;
						for each(XbimGeometryModel^ gm in this) //if any not other return false
						{
							if(gm->HasCurvedEdges) return true;
						}
						return false;
					}
				}


				void Add(XbimGeometryModel^ shape)
				{
					shapes->Add(shape);
					if(pCompound)
					{
						delete pCompound;
						pCompound=0;
					}
				}

				XbimGeometryModel^ Solidify();


				/*Interfaces*/
				virtual property TopoDS_Shape* Handle
				{
					//

					TopoDS_Shape* get() override
					{
						if(!pCompound)
						{
							BRep_Builder b;
							pCompound = new TopoDS_Compound();;
							b.MakeCompound(*pCompound);
							for each(XbimGeometryModel^ shape in shapes)
							{
								if(!shape->Handle->IsNull())
									b.Add(*pCompound, *(shape->Handle));
							}
						}
						return pCompound;
					};
				}
				virtual XbimGeometryModel^ Cut(XbimGeometryModel^ shape) override;
				virtual XbimGeometryModel^ Union(XbimGeometryModel^ shape) override;
				virtual XbimGeometryModel^ Intersection(XbimGeometryModel^ shape) override;
				virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection) override;
				virtual XbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement) override;
				virtual void Move(TopLoc_Location location) override;
			};
		}
	}
}