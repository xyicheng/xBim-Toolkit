#include "StdAfx.h"
#include "XbimScene.h"
#include "IXbimGeometryModel.h"
#include "XbimGeometryModel.h"
using namespace System::IO;
using namespace System::Linq;

using namespace Xbim::ModelGeometry::Scene;
using namespace Xbim::Common::Exceptions;
using namespace Xbim::XbimExtensions;
using namespace Xbim::Common;

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

		void XbimScene::ConvertGeometry(XbimModel^ model, IEnumerable<IfcProduct^>^ toConvert, ReportProgressDelegate^ progDelegate)
		{
			TransformGraph^ graph = gcnew TransformGraph(model);
			//create a new dictionary to hold maps
			Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps = gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>();
			//add everything that may have a representation
			graph->AddProducts(toConvert); //load the products as we will be accessing their geometry

			XbimGeometryTable^ geomTable = model->GetGeometryTable();


			int tally = 0;
			int percentageParsed=0;
			int total = Enumerable::Count(toConvert);
			XbimLazyDBTransaction transaction = geomTable->BeginLazyTransaction();
			try
			{
				for each(TransformNode^ node in graph->ProductNodes->Values) //go over every node that represents a product
				{
					IfcProduct^ product = node->Product;
					try
					{
						XbimLOD lod = XbimLOD::LOD_Unspecified;
						IXbimGeometryModel^ geomModel = XbimGeometryModel::CreateFrom(product, maps, false, lod);
						if (geomModel != nullptr)  //it has no geometry
						{

							XbimTriangulatedModelCollection^ tm = geomModel->Mesh(true);
							XbimBoundingBox^ bb = geomModel->GetBoundingBox(true);
							node->BoundingBox = bb->GetRect3D();
							array<Byte>^ matrix = Matrix3DExtensions::ToArray(node->WorldMatrix(), true);
							UInt16 typeId = IPersistIfcEntityExtensions::TypeId(product);
							geomTable->AddGeometry(product->EntityLabel, XbimGeometryType::BoundingBox, typeId, matrix, bb->ToArray(), 0, 0 ) ;
							int subPart = 0;
							for each(array<Byte>^ b in tm)
							{
								geomTable->AddGeometry(product->EntityLabel, XbimGeometryType::TriangulatedMesh, typeId, matrix, b , geomModel->RepresentationLabel, subPart) ;
								subPart++;
							}
							tally++;
							if(progDelegate!=nullptr)
							{
								int newPercentage = Convert::ToInt32((double)tally / total * 100.0);
								if (newPercentage > percentageParsed)
								{
									percentageParsed = newPercentage;
									progDelegate(percentageParsed, "Converted");
								}
							}

							if (tally % 100 == (100 - 1))
							{
								transaction.Commit();
								transaction.Begin();
							}
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
				transaction.Commit();
			}
			finally
			{
				model->FreeGeometryTable(geomTable);
			}



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
					_graph->Write(bw, nullptr);
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
			XbimModelFactors^ mf = ((IPersistIfcEntity^)product)->ModelOf->GetModelFactors;
			if(product!=nullptr) //there is no product at this node
			{
				try
				{
					IXbimGeometryModel^ geomModel = XbimGeometryModel::CreateFrom(product, _maps, false, _lod);
					if (geomModel != nullptr)  //it has no geometry
					{
						XbimTriangulatedModelCollection^ tm = geomModel->Mesh(true);
						XbimBoundingBox^ bb = geomModel->GetBoundingBox(true);
						node->BoundingBox = bb->GetRect3D();
						return gcnew XbimTriangulatedModelStream(tm);
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
