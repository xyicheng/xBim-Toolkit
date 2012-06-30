#include "StdAfx.h"
#include "XbimScene.h"
#include "IXbimGeometryModel.h"
#include "XbimGeometryModel.h"
using namespace System::IO;
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
				Logger->Error("Failed to Reopen Scene");
				return false;
			}
		}

		XbimScene::XbimScene(String ^ ifcFileName,String ^ xBimFileName,String ^ xBimGeometryFileName, bool removeIfcGeometry, ProcessModel ^ processingDelegate)
		{
			ImportIfc(ifcFileName, xBimFileName, xBimGeometryFileName, removeIfcGeometry, processingDelegate);
		}


		/*Imports an Ifc file and creates an Xbim file, geometry is optionally removed*/
		XbimScene::XbimScene(String ^ ifcFileName,String ^ xBimFileName,String ^ xBimGeometryFileName, bool removeIfcGeometry)
		{
			ImportIfc(ifcFileName, xBimFileName, xBimGeometryFileName, removeIfcGeometry, nullptr);
		}

		void XbimScene::ImportIfc(String ^ ifcFileName,String ^ xBimFileName,String ^ xBimGeometryFileName, bool removeIfcGeometry, 
			ProcessModel ^ processingDelegate)
		{
			Initialise();

			Logger->InfoFormat("Importing IFC model {0}.", ifcFileName);
			
			XbimFileModelServer^ model = gcnew XbimFileModelServer();
			_maps = gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>();
			try
			{
				String^ tmpFileName = Path::GetTempFileName();
				//create a binary xbim file

				if(removeIfcGeometry)
					tmpFileName = model->ImportIfc(ifcFileName, tmpFileName);
				else
					tmpFileName = model->ImportIfc(ifcFileName);

				Logger->DebugFormat("Ifc parsed and generated XBIM file, {0}", tmpFileName);
				_graph = gcnew TransformGraph(model, this);
				//add everything with a representation
				_graph->AddProducts(model->IfcProducts->Items);
				Logger->Debug("Geometry Created. Saving GC file..."); 
				_sceneStreamFileName = xBimGeometryFileName;
				_sceneStream = gcnew FileStream(_sceneStreamFileName, FileMode::Create, FileAccess::ReadWrite);
				BinaryWriter^ bw = gcnew BinaryWriter(_sceneStream);
				{
					_graph->Write(bw);
					bw->Flush();
					Close();
					ReOpen();
				}
				Logger->DebugFormat("Geometry persisted to {0}", _sceneStreamFileName);

				if(removeIfcGeometry)
				{
					Logger->Debug("Removing Geometry");
					if(processingDelegate != nullptr)
					{
						processingDelegate->Invoke(model);
					}
					model->ExtractSemantic(xBimFileName);

				}
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
					IXbimGeometryModel^ geomModel = XbimGeometryModel::CreateFrom(product, _maps, false);
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
