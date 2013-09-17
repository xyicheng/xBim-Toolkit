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
using namespace Xbim::IO;
using namespace Xbim::Ifc2x3::PresentationAppearanceResource;
using namespace System::Linq;
using namespace  System::Threading;
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			static int CompareBoundinBoxSize(XbimGeometryModel^ a, XbimGeometryModel^ b)
			{
			      /*float aLen = a->GetBoundingBox().Length();
				  float bLen = b->GetBoundingBox().Length();*/
				double aVol = a->Volume;
				double bVol = b->Volume;
				//  return aLen.CompareTo(bLen);
				return bVol.CompareTo(aVol);
			}

			public ref class XbimGeometryModelCollection : public XbimGeometryModel, IEnumerable<XbimGeometryModel^>
			{
			protected:
				List<XbimGeometryModel^>^ shapes;
				void Init();
			public:
				XbimGeometryModelCollection(void);
				XbimGeometryModelCollection(bool hasCurvedEdges, int representationLabel, int surfaceStyleLabel);
				XbimGeometryModelCollection(int representationLabel, int surfaceStyleLabel);
				XbimGeometryModelCollection(IfcRepresentationItem^ representationItem);
				XbimGeometryModelCollection(IfcRepresentation^ representation);
				XbimGeometryModelCollection(const TopoDS_Shape&  shape, bool hasCurves,int representationLabel, int surfaceStyleLabel );
				
#if USE_CARVE
				//virtual XbimPolyhedron^ ToPolyHedron(double deflection, double precision,double precisionMax) override;
				
#endif
				virtual property bool IsValid
				{
					bool get() override
					{
						return shapes->Count > 0;
					}
				}
			

				~XbimGeometryModelCollection()
				{
					InstanceCleanup();
				}

				!XbimGeometryModelCollection()
				{
					InstanceCleanup();
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

				virtual void Move(TopLoc_Location location) override;
			
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


				void Add(XbimGeometryModel^ shape)
				{
					shapes->Add(shape);
					if(shape->HasCurvedEdges) _hasCurvedEdges=true;
					if(nativeHandle)
					{
						delete nativeHandle;
						nativeHandle=nullptr;
					}
				}

			
				/*Interfaces*/
				//returns a handle to a compound of all the shapes in this collecton, nb they are not sewn
				virtual property TopoDS_Shape* Handle
				{
					//

					TopoDS_Shape* get() override
					{

						Monitor::Enter(this);
						try
						{
							if(nativeHandle == nullptr)
							{
								BRep_Builder b;
								nativeHandle = new TopoDS_Compound();
								b.MakeCompound(*((TopoDS_Compound*)nativeHandle));
								for each(XbimGeometryModel^ shape in shapes)
								{
									if(!shape->Handle->IsNull())
									{
										b.Add(*((TopoDS_Compound*)nativeHandle), *(shape->Handle));
										if(shape->HasCurvedEdges) _hasCurvedEdges=true;
									}
								}
							}
						}
						finally
						{
							Monitor::Exit(this);
						}

						return nativeHandle;
					};
				}

				virtual XbimTriangulatedModelCollection^ Mesh(double deflection) override;
				virtual XbimGeometryModel^ CopyTo(IfcAxis2Placement^ placement) override;
				
				virtual void ToSolid(double precision, double maxPrecision) override;
				virtual property int Count
				{
					int get() {return shapes->Count;};
				}

				//returns the first in the collection or nulllptr if the collection is empty
				virtual property XbimGeometryModel^ FirstOrDefault
				{
					XbimGeometryModel^ get() {return Enumerable::FirstOrDefault<XbimGeometryModel^>(shapes);};
				}

				
				void Remove(XbimGeometryModel^ shape)
				{
					shapes->Remove(shape);
					if(nativeHandle)
					{
						delete nativeHandle;
						nativeHandle=nullptr;
					}
				}
				void Replace(int idx,XbimGeometryModel^ shape)
				{
					shapes[idx]=shape;
					if(nativeHandle)
					{
						delete nativeHandle;
						nativeHandle=nullptr;
					}
				}
				void Insert(int idx, XbimGeometryModel^ shape)
				{
					shapes->Insert(idx, shape);
					if(nativeHandle)
					{
						delete nativeHandle;
						nativeHandle=nullptr;
					}
				}
				XbimGeometryModel^ Shape(int idx)
				{
					return shapes[idx];
				}
				//sorts with largest element first
				void SortDescending()
				{
					shapes->Sort(gcnew Comparison<XbimGeometryModel^>(CompareBoundinBoxSize));
				}
			};
		}
	}
}