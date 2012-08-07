#include "StdAfx.h"
#include "XbimScene.h"
#include "IXbimGeometryModel.h"
#include "XbimGeometryModel.h"
using namespace System::IO;
using namespace System::Linq;
using namespace Xbim::IO;
using namespace Xbim::ModelGeometry::Scene;
using namespace Xbim::Common::Exceptions;

namespace Xbim
{
	namespace ModelGeometry
	{	
		XbimScene::!XbimScene()
		{
		}

		XbimScene::~XbimScene()
		{
			_maps = gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>();
		}

		XbimScene::XbimScene(IModel^ model)
		{

			Initialise();
			Logger->Debug("Creating Geometry from IModel..."); 
			 _graph = gcnew TransformGraph(model, this);
			 _maps = gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>();
			 _graph->AddProducts(Enumerable::Cast<IfcProduct^>(model->IfcProducts));
		}

		XbimScene::XbimScene(IModel^ model, IEnumerable<IfcProduct^>^ toDraw )
		{
			Initialise();
			Logger->Debug("Creating Geometry from IModel..."); 
			 _graph = gcnew TransformGraph(model, this);
			 _maps = gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>();
			 _graph->AddProducts(toDraw);
			
		}
		
		void XbimScene::ConvertGeometry(XbimModel^ model)
		{
		TransformGraph^ graph = gcnew TransformGraph(model);
		//create a new dictionary to hold maps
		Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps = gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>();
		//add everything that may have a representation
		graph->AddProducts(model->InstancesOfType<IfcProduct^>(true)); //load the products as we will be accessing their geometry
		XbimGeometryTable^ geomTable = model->BeginGeometryUpdate();
		
		
		for each(TransformNode^ node in graph->ProductNodes->Values) //go over every node that represents a product
		{
			IfcProduct^ product = node->Product;
				try
				{
					XbimLOD lod = XbimLOD::LOD_Unspecified;
					IXbimGeometryModel^ geomModel = XbimGeometryModel::CreateFrom(product, maps, false, lod);
					if (geomModel != nullptr)  //it has no geometry
					{
						
						XbimTriangulatedModelStream^ tm = geomModel->Mesh(true);
						XbimBoundingBox^ bb = geomModel->GetBoundingBox(true);
						node->BoundingBox = bb->GetRect3D();
						Matrix3D matrix3d = node->WorldMatrix();
						geomTable->AddGeometry(product->EntityLabel,geomModel->RepresentationLabel, 1, matrix3d, tm->ToArray(),nullptr ) ;
					
					}
				}
				catch(Exception^ e)
				{
					String^ message = String::Format("Error Triangulating product geometry of entity {0} - {1}", 
						product->EntityLabel,
						product->ToString());
					Logger->Warn(message, e);
				}
		}
		model->EndGeometryUpdate(geomTable);

			//find the body context
			/*IEnumerable<IfcGeometricRepresentationContext^>^ contexts = model->InstancesOfType<IfcGeometricRepresentationContext^>();
			for each (IfcGeometricRepresentationContext^ context in contexts)
			{

			}*/
			//store all the bounding boxes
			//IEnumerable<IfcShapeRepresentation^>^ shapes = model->InstancesOfType<IfcShapeRepresentation^>();
			//for each(IfcShapeRepresentation^ shape in shapes)
			//{
			//	if(shape->RepresentationType.HasValue &&
			//		String::Compare(shape->RepresentationType,"BoundingBox",CompareOptions::IgnoreCase)==0) //add a body box
			//	{
			//		model->AddBoundingBox(IfcBoundingBox);
			//	} 
			//	//have a look for bodies or shells
			//	if( shape->RepresentationIdentifier.HasValue &&
			//													((String::Compare(shape->RepresentationIdentifier.Value, "body" , CompareOptions::IgnoreCase)==0)||
			//													String::Compare(shape->RepresentationIdentifier.Value, "facetation" , CompareOptions::IgnoreCase)==0))
			//	{

			//	}
			//}
		}


		void XbimScene::Close()
		{
			_sceneStream->Close();
			_graph->Close();
		}

		

		XbimScene::XbimScene(String ^ ifcFileName,String ^ xBimFileName,String ^ xBimGeometryFileName, ProcessModel ^ processingDelegate)
		{
			ImportIfc(ifcFileName, xBimFileName, xBimGeometryFileName, processingDelegate);
		}


		/*Imports an Ifc file and creates an Xbim file, geometry is optionally removed*/
		XbimScene::XbimScene(String ^ ifcFileName,String ^ xBimFileName,String ^ xBimGeometryFileName)
		{
			ImportIfc(ifcFileName, xBimFileName, xBimGeometryFileName, nullptr);
		}

		void XbimScene::ImportIfc(String ^ ifcFileName,String ^ xBimFileName,String ^ xBimGeometryFileName,  
			ProcessModel ^ processingDelegate)
		{
			Initialise();

			Logger->InfoFormat("Importing IFC model {0}.", ifcFileName);
			
			XbimModel^ model = gcnew XbimModel();
			_maps = gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>();
			try
			{
				
				model->CreateFrom( ifcFileName,xBimFileName, nullptr);

				Logger->DebugFormat("Ifc parsed and generated XBIM file, {0}", xBimFileName);
				_graph = gcnew TransformGraph(model, this);
				//add everything with a representation
				_graph->AddProducts(model->InstancesOfType<IfcProduct^>(true)); //load the products as we will be accessing their geometry
				Logger->Debug("Geometry Created. Saving GC file..."); 
				_sceneStreamFileName = xBimGeometryFileName;
				_sceneStream = gcnew FileStream(_sceneStreamFileName, FileMode::Create, FileAccess::ReadWrite);
				BinaryWriter^ bw = gcnew BinaryWriter(_sceneStream);
				{
					_graph->Write(bw);
					bw->Flush();
					
				}
				Logger->DebugFormat("Geometry persisted to {0}", _sceneStreamFileName);
				
				Logger->InfoFormat("Completed import of Ifc File {0}", ifcFileName);
			}
			catch(XbimGeometryException^ e)
			{
				String^ message = String::Format("A geometry error ocurred while importing Ifc File, {0}",e->Message);
				Logger->Error(message, e);
				throw;
			}
			catch(Exception^ e)
			{
				String^ message = String::Format("An error ocurred while importing Ifc File, {0}",e->Message);
				Logger->Error(message, e);
				throw gcnew XbimGeometryException(message, e);
			}
		}

		XbimSceneStream^ XbimScene::AsSceneStream()
		{
			return gcnew XbimSceneStream(_graph->Model, _sceneStreamFileName);
		}

		XbimTriangulatedModelStream^ XbimScene::Triangulate(TransformNode^ node)
		{

			IfcProduct^ product = node->Product;
			if(product!=nullptr) //there is no product at this node
			{
				try
				{
					IXbimGeometryModel^ geomModel = XbimGeometryModel::CreateFrom(product, _maps, false, _lod);
					if (geomModel != nullptr)  //it has no geometry
					{
						XbimTriangulatedModelStream^ tm = geomModel->Mesh(true);
						XbimBoundingBox^ bb = geomModel->GetBoundingBox(true);
						node->BoundingBox = bb->GetRect3D();
						return tm;
					}
				}
				catch(Exception^ e)
				{
					String^ message = String::Format("Error Triangulating product geometry of entity {0} - {1}", 
						product->EntityLabel,
						product->ToString());
					Logger->Warn(message, e);
				}
			}

			return XbimTriangulatedModelStream::Empty;
		}
	}
}
