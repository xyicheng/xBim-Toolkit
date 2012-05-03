#include "StdAfx.h"
#include "XbimScene.h"
#include "IXbimGeometryModel.h"
#include "XbimGeometryModel.h"
using namespace System::IO;
using namespace Xbim::IO;
using namespace Xbim::ModelGeometry::Scene;
namespace Xbim
{
	namespace ModelGeometry
	{	
		XbimScene::!XbimScene()
		{
		}

		XbimScene::~XbimScene()
		{
		}


		XbimScene::XbimScene(IModel^ model)
		{
			Initialise();
			 _graph = gcnew TransformGraph(model, this);
			 _graph->AddProducts(model->IfcProducts->Items);
			
		}

		void XbimScene::Close()
		{
			_sceneStream->Close();
			_graph->Close();
		}

		bool XbimScene::ReOpen()
		{
			try
			{
				_sceneStream = gcnew FileStream(_sceneStreamFileName, FileMode::Open, FileAccess::Read);
				return _graph->ReOpen();
			}
			catch(...)
			{
				return false;
			}
		}

		XbimScene::XbimScene(String ^ ifcFileName,String ^ xBimFileName,String ^ xBimGeometryFileName, bool removeIfcGeometry, ProcessModel ^ processingDelegate)
		{
			Initialise();
			
			XbimFileModelServer^ model = gcnew XbimFileModelServer();
			try
			{
				String^ tmpFileName = Path::GetTempFileName();
				//create a binary file
				if(removeIfcGeometry)
					model->ImportIfc(ifcFileName, tmpFileName);
				else
					model->ImportIfc(ifcFileName);
				_graph = gcnew TransformGraph(model, this);
				//add everything with a representation
				_graph->AddProducts(model->IfcProducts->Items);
				_sceneStreamFileName = xBimGeometryFileName;
				_sceneStream = gcnew FileStream(_sceneStreamFileName, FileMode::Create, FileAccess::ReadWrite);
				BinaryWriter^ bw = gcnew BinaryWriter(_sceneStream);
				{
					_graph->Write(bw);
					bw->Flush();
					Close();
					ReOpen();
					
				}
				if(removeIfcGeometry)
				{
					if(processingDelegate != nullptr)
					{
						processingDelegate->Invoke(model);
					}
					model->ExtractSemantic(xBimFileName);

				}
				
			}
			catch(Exception^ e)
			{
				throw gcnew Exception(String::Format("Error importing Ifc File, {0}",e->Message), e);
			}
		}

		/*Imports an Ifc file and creates an Xbim file, geometry is optionally removed*/
		XbimScene::XbimScene(String ^ ifcFileName,String ^ xBimFileName,String ^ xBimGeometryFileName, bool removeIfcGeometry)
		{

			Initialise();
			
			XbimFileModelServer^ model = gcnew XbimFileModelServer();
			try
			{
				String^ tmpFileName = Path::GetTempFileName();
				//create a binary file
				if(removeIfcGeometry)
					model->ImportIfc(ifcFileName, tmpFileName, nullptr);
				else
					model->ImportIfc(ifcFileName);
				_graph = gcnew TransformGraph(model, this);
				//add everything with a representation
				_graph->AddProducts(model->IfcProducts->Items);
				_sceneStreamFileName = xBimGeometryFileName;
				_sceneStream = gcnew FileStream(_sceneStreamFileName, FileMode::Create, FileAccess::ReadWrite);
				BinaryWriter^ bw = gcnew BinaryWriter(_sceneStream);
				{
					_graph->Write(bw);
					bw->Flush();
					Close();
					ReOpen();
					
				}
				if(removeIfcGeometry)
				{
					model->ExtractSemantic(xBimFileName);

				}
				
			}
			catch(Exception^ e)
			{
				throw gcnew Exception(String::Format("Error importing Ifc File, {0}",e->Message));
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
					IXbimGeometryModel^ geomModel = XbimGeometryModel::CreateFrom(product, false);
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
					System::Diagnostics::Debug::WriteLine(String::Format("Error Triangulating product geometry of entity {0}", product->EntityLabel));
					System::Diagnostics::Debug::WriteLine(e->Message);
				}
			}

			return XbimTriangulatedModelStream::Empty;
		}
	}
}
