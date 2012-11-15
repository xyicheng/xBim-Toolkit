using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using Xbim.XbimExtensions;
using Xbim.IO;
using Xbim.DynamicGrouping;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Extensions;
using System.Xml;
using Xbim.XbimExtensions.Transactions;
using System.ComponentModel;
using System.Collections.Specialized;
using Xbim.SceneJSWebViewer.ObjectDataProviders;
using Xbim.Ifc2x3.MaterialResource;
using System.Diagnostics;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.ModelGeometry;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.SceneJSWebViewer
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                //get any xbim files in the /model directory of this web app, and display them as options for the user to load
                String path = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + "models";
                DirectoryInfo di = new DirectoryInfo(path);

                FileInfo[] fis = di.GetFiles("*.xbim");

                LiteralControl litcon = new LiteralControl();
                litcon.Text += "<ul>";
                //Setup the links to our models
                foreach (FileInfo fi in fis)
                {
                    //fi.Name;
                    litcon.Text += "<li><a href=\"#\" OnClick=DynamicLoad('"+fi.Name+"');>"+fi.Name+"</a></li>";
                }
                litcon.Text+= "</ul>";
                //this.menu.Controls.Add(litcon);
            }
        }

        #region Preprocessing before upload
                
        private static void CreateLayeredElementTypes(XbimModel model)
        {
            XbimReadWriteTransaction trans = model.BeginTransaction("Create layered element types.");
            //get elements without element type
            IEnumerable<IfcBuildingElement> elements = model.Instances.Where<IfcBuildingElement>(el =>
                el.GetDefiningType() == null &&
                el.GetMaterialLayerSetUsage(model) != null);

            //create groups of elements with the same material layer set usage (it means they have the same element type indirectly)
            IEnumerable<IGrouping<IfcMaterialLayerSet, IfcBuildingElement>> groups = elements.GroupBy(el => el.GetMaterialLayerSetUsage(model).ForLayerSet);

            foreach (IGrouping<IfcMaterialLayerSet, IfcBuildingElement> group in groups)
            {
                IfcMaterialLayerSet layerSet = group.Key;
                IEnumerable<IGrouping<Type, IfcBuildingElement>> typeGroups = group.ToList().GroupBy(el => el.GetType());

                foreach (IGrouping<Type, IfcBuildingElement> typeGroup in typeGroups)
                {
                    List<IfcBuildingElement> elementsToProcess = typeGroup.ToList();
                    IfcTypeProduct type = GetNewElementType(elementsToProcess.FirstOrDefault(), model);
                    if (type == null)
                    {
#if DEBUG
                        throw new Exception("No type for the element!");
#else
                    continue;
#endif
                    }
                    //material layer set
                    type.SetMaterial(layerSet);

                    //move properties which should be type properties
                    MoveTypeProperties(elementsToProcess.FirstOrDefault(), type);

                    //set element type name
                    string typeName = layerSet.LayerSetName;
                    string[] parts = (string.IsNullOrEmpty(typeName)) ? null : typeName.Split(':');
                    if (parts != null && parts.Length > 1) typeName = parts[1];
                    //create default name
                    if (string.IsNullOrEmpty(typeName))
                    {
                        typeName = type.GlobalId; //to be sure that it is unique for now
                    }

                    //assign name to the type object
                    type.Name = typeName;

                    //assign element type to the instances
                    foreach (var element in elementsToProcess)
                    {
                        element.SetDefiningType(type, model);

                        //process any aggregated elements (for example roof slabs)
                        if (element.IsDecomposedBy.FirstOrDefault() != null)
                        {
                            //add decomposing elements to "to process" list 
                            foreach (var rel in element.IsDecomposedBy)
                            {
                                foreach (var el in rel.RelatedObjects)
                                {
                                    IfcBuildingElement buildelem = el as IfcBuildingElement;
                                    if (buildelem != null && buildelem.GetDefiningType() == null) buildelem.SetDefiningType(type, model);
                                }
                            }
                        }
                    }
                }
            }

            //commit transaction
            trans.Commit();
        }

        private static void MoveTypeProperties(IfcBuildingElement element, IfcTypeProduct type)
        {
            IEnumerable<IfcRelDefinesByProperties> rels = element.IsDefinedByProperties.Where(r => IsTypeSetName(r.RelatingPropertyDefinition));
            foreach (IfcRelDefinesByProperties rel in rels)
            {
                rel.RelatedObjects.Remove_Reversible(element);
                type.AddPropertySet(rel.RelatingPropertyDefinition);
            }
        }

        private static bool IsTypeSetName(IfcPropertySetDefinition def)
        {
            if (def == null) return false;

            string name = def.Name;
            if (name == null) return false;

            return name.ToLower().Contains("_type_");
        }

        private static IfcTypeProduct GetNewElementType(IfcElement element, IModel model)
        {
            if (element is IfcBuildingElementProxy) return model.Instances.New<IfcBuildingElementProxyType>();
            if (element is IfcCovering) return model.Instances.New<IfcCoveringType>();
            if (element is IfcBeam) return model.Instances.New<IfcBeamType>();
            if (element is IfcColumn) return model.Instances.New<IfcColumnType>();
            if (element is IfcCurtainWall) return model.Instances.New<IfcCurtainWallType>();
            if (element is IfcDoor) return model.Instances.New<IfcDoorStyle>();
            if (element is IfcMember) return model.Instances.New<IfcMemberType>();
            if (element is IfcRailing) return model.Instances.New<IfcRailingType>();
            if (element is IfcRampFlight) return model.Instances.New<IfcRampFlightType>();
            if (element is IfcWall) return model.Instances.New<IfcWallType>();
            if (element is IfcSlab) return model.Instances.New<IfcSlabType>(t => t.PredefinedType = (element as IfcSlab).PredefinedType ?? IfcSlabTypeEnum.NOTDEFINED);
            if (element is IfcStairFlight) return model.Instances.New<IfcStairFlightType>();
            if (element is IfcWindow) return model.Instances.New<IfcWindowStyle>();
            if (element is IfcPlate) return model.Instances.New<IfcPlateType>();
            if (element is IfcCovering) return model.Instances.New<IfcCoveringType>();
            if (element is IfcRoof) return model.Instances.New<IfcSlabType>(sl => sl.PredefinedType = IfcSlabTypeEnum.ROOF);

            return model.Instances.New<IfcBuildingElementProxyType>();
        }

        private static string GetFamilyName(IfcBuildingElement element)
        {
            string name = element.Name;
            if (String.IsNullOrEmpty(name)) return "";

            string[] parts = name.Split(':');
            if (parts.Length > 1) return parts[1];

            return "";
        }

        private static void CreateElementTypesByName(XbimModel model)
        {
            XbimReadWriteTransaction trans = model.BeginTransaction("Create element types by name.");
            //get elements without element type
            IEnumerable<IfcBuildingElement> elements = model.Instances.Where<IfcBuildingElement>(el =>
                el.GetDefiningType() == null);

            //create groups of elements with the same material layer set usage (it means they have the same element type indirectly)
            IEnumerable<IGrouping<string, IfcBuildingElement>> groups = elements.GroupBy(el => GetFamilyName(el));

            foreach (IGrouping<string, IfcBuildingElement> group in groups)
            {
                string name = group.Key;
                IEnumerable<IGrouping<Type, IfcBuildingElement>> typeGroups = group.ToList().GroupBy(el => el.GetType());

                foreach (IGrouping<Type, IfcBuildingElement> typeGroup in typeGroups)
                {
                    List<IfcBuildingElement> elementsToProcess = typeGroup.ToList();
                    IfcTypeProduct type = GetNewElementType(elementsToProcess.FirstOrDefault(), model);
                    if (type == null)
                    {
#if DEBUG
                        throw new Exception("No type for the element!");
#else
                    continue;
#endif
                    }

                    //set name to the type object
                    type.Name = name;

                    //assign element type to the instances
                    foreach (var element in elementsToProcess)
                    {
                        element.SetDefiningType(type, model);

                        //process any aggregated elements (for example roof slabs)
                        if (element.IsDecomposedBy.FirstOrDefault() != null)
                        {
                            //add decomposing elements to "to process" list 
                            foreach (var rel in element.IsDecomposedBy)
                            {
                                foreach (var el in rel.RelatedObjects)
                                {
                                    IfcBuildingElement buildelem = el as IfcBuildingElement;
                                    if (buildelem != null && buildelem.GetDefiningType() == null) buildelem.SetDefiningType(type, model);
                                }
                            }
                        }
                    }
                }
            }

            //commit transaction
            trans.Commit();
        }

        #endregion

        [System.Web.Services.WebMethod()]
        public static string AddClassification(string xbimFilename)
        {
            try
            {
                string dirName = "models\\";
                string file = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + dirName + xbimFilename;
                //string ifcFile = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + dirName + Path.ChangeExtension(xbimFilename, "ifc");
                XbimModel model = new XbimModel();
                model.Open(file);
                CreateLayeredElementTypes(model);
                CreateElementTypesByName(model);
                string result = Grouping(model);

                // create ifc file
                //model.Export(XbimStorageType.IFC, ifcFile);

                model.Close();
                model.Dispose();

                return result;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            
        }








        /*
        public static string ConvertToxBim(string ifcFilename)
        {
            string xbimFileName = Path.ChangeExtension(ifcFilename, "xbim");
            string xbimGeometryFileName = Path.ChangeExtension(ifcFilename, "xbimGC");
            using (XbimFileModelServer model = ParseModelFile(ifcFilename, xbimFileName))
            {
                GenerateGeometry(xbimGeometryFileName, model);
                model.Close();
            }

            return "success";
        }

        private static XbimFileModelServer ParseModelFile(string ifcFileName, string xbimFileName)
        {
            XbimFileModelServer model = new XbimFileModelServer();
            //create a callback for progress
            model.ImportIfc(ifcFileName, xbimFileName);

            return model;
        }

        private static void GenerateGeometry(string xbimGeometryFileName, XbimFileModelServer model)
        {
            //now convert the geometry

            IEnumerable<IfcProduct> toDraw = GetProducts(model);
            
            //XbimScene scene = new XbimScene(model, toDraw);

            //using (FileStream sceneStream = new FileStream(xbimGeometryFileName, FileMode.Create, FileAccess.ReadWrite))
            //{
            //    BinaryWriter bw = new BinaryWriter(sceneStream);
            //    scene.Graph.Write(bw, delegate(int percentProgress, object userState)
            //    {
                    
            //    });
            //    bw.Flush();
            //}
        }

        private static IEnumerable<IfcProduct> GetProducts(XbimFileModelServer model)
        {
            IEnumerable<IfcProduct> result = null;

            result = model.IfcProducts.Items;
            return result;
        }
        */














        //[System.Web.Services.WebMethod()]
        public static string Grouping(XbimModel model)
        {
            XbimReadWriteTransaction trans = model.BeginTransaction("Create grouping.");

            //List<Xbim.SceneJSWebViewer.XML.Group> results = new List<Xbim.SceneJSWebViewer.XML.Group>();
            string str = "";

            String fileName = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + "XML\\NRM clssification.xml";
            //string file = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + "models\\" + ifcFilename;
            String xmlRulesData = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + "XML\\NRM2IFC.xml";
                                 
            //enable users to use all overloaded constructors of the XmlDocument (string, XmlReader, fileName, ...)
            System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
            xmlDoc.Load(fileName);

            //IModel model = new XbimMemoryModel();
            //IfcInputStream input = new IfcInputStream(new FileStream(file, FileMode.Open, FileAccess.Read));
            //int errs = input.Load(model);






            GroupsFromXml groupCreator = new GroupsFromXml(model);

            //get root of the groups or create it if it does not exist
            IfcGroup group = groupCreator.GetExistingRootGroup(xmlDoc);
            if (group == null)
            {
                groupCreator.CreateGroups(xmlDoc);
                group = groupCreator.RootGroups.FirstOrDefault();
            }
            if (group != null)
            {
                if (!HasAnyElementsOfType<IfcTypeObject>(group))
                {
                    //perform grouping according to the specified rules
                    XmlDocument ruleDoc = new XmlDocument();
                    ruleDoc.Load(xmlRulesData);

                    GroupingByXml grouper = new GroupingByXml(model);
                    bool success = grouper.GroupElements(ruleDoc, group);


                    SimpleGroup simpleGroup = new SimpleGroup(group, null);
                    
                    // create html                    
                    if (simpleGroup != null && model != null)
                    {                        
                        //start tree structure
                        IEnumerable<SimpleGroup> subgroups = simpleGroup.Children;
                        if (subgroups.FirstOrDefault() != null)
                        {
                            str = str + "<ul class='GroupTree'>";
                            str = str + RenderGroups(subgroups);
                            str = str + "</ul>";
                        }                        
                    }
                }
                
            }


            trans.Commit();






                        
            /*
            //if (errs > 0) { Console.WriteLine("Errors while processing the model"); Console.Write(input.ErrorLog); Console.WriteLine(); }

            //Console.WriteLine("Creation of the groups from the XML file---------------------------------------------------");
            GroupsFromXml grCreation = new GroupsFromXml(model);
            grCreation.CreateGroups(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + "XML\\NRM clssification.xml");
            string grCreationErrs = grCreation.ErrorLog.ToString();
            if (!string.IsNullOrEmpty(grCreationErrs)) Console.Write(grCreationErrs);
            //Console.WriteLine();
            //Console.WriteLine("Group creation finished:");
            //check groups
            IEnumerable<IfcGroup> checkGroups = model.InstancesOfType<IfcGroup>();
            foreach (IfcGroup group in checkGroups)
            {
                int inGroup = group.GetGroupedObjects().Count();
                //Console.WriteLine("Group: " + group.Name + ", groups inside: " + inGroup);

            }


            //Console.WriteLine("Sorting objects in the model into the groups---------------------------------------------------");
            GroupingByXml gr = new GroupingByXml(model);
            gr.Load(xmlDoc);
            gr.PerformGrouping();
            //gr.GroupElements(fileName, null);
            //gr.GroupElements(xmlDoc, null);
            gr.ErrorLog.Flush();
            string errMsg = gr.ErrorLog.ToString();
            if (!string.IsNullOrEmpty(errMsg))
            {
                //Console.Write(errMsg);
                //Console.WriteLine();

                StreamWriter errLog = new StreamWriter("GroupingErr.log");
                errLog.Write(errMsg);
                errLog.Close();
                //Console.Write(errMsg);
            }
            //Console.WriteLine();

            //StreamWriter output = new StreamWriter("GroupingReport.log");

            
            IEnumerable<IfcGroup> groups = model.InstancesOfType<IfcGroup>();

            foreach (var group in groups)
            {
                Xbim.SceneJSWebViewer.XML.Group g = new Xbim.SceneJSWebViewer.XML.Group();
                string grName = group.Name;
                IfcRelAssignsToGroup rel = group.IsGroupedBy;
                if (rel == null) continue;
                IEnumerable<IfcObjectDefinition> objects = rel.RelatedObjects;
                //output.WriteLine("Group: " + grName);
                g.groupName = "Group: " + grName;
                g.groupItems = new List<XML.GroupItem>();
                foreach (var obj in objects)
                {
                    //output.WriteLine("\t GUID: " + obj.GlobalId + ", Name: " + obj.Name + ", Type: " + obj.GetType().Name);
                    Xbim.SceneJSWebViewer.XML.GroupItem gi = new XML.GroupItem();
                    gi.objGuid = "GUID: " + obj.GlobalId;
                    gi.objName = "Name: " + obj.Name;
                    gi.objTypeName = "Type: " + obj.GetType().Name;
                    g.groupItems.Add(gi);
                }
                results.Add(g);
            }
            */


            return str;
            //output.Close();
            //Console.WriteLine("Sorting objects into the groups finished.");
            //Console.ReadKey();
            
        }

        private static bool HasAnyElementsOfType<T>(IfcGroup group) where T : IfcObjectDefinition
        {
            IEnumerable<T> elements = group.GetGroupedObjects<T>();
            if (elements.FirstOrDefault() != null) return true;
            IEnumerable<IfcGroup> subGroups = group.GetGroupedObjects<IfcGroup>();
            foreach (var gr in subGroups)
            {
                if (HasAnyElementsOfType<T>(gr)) return true;
            }
            return false;
        }

        private static string RenderGroups(IEnumerable<SimpleGroup> groups)
        {
            string str = "";

            Random rand = new Random();
            if (groups == null || groups.FirstOrDefault() == null) return "";
            foreach (SimpleGroup group in groups)
            {
                //if (IsEmpty(group)) return;

                str = str + "<li class='ifc-group'>";
                string name = group.Name;
                string prefix = "(" + group.Description + ")";
                name = prefix + " " + name;
                string[] values = new string[] {  };
                str = str + WriteRow(name, "", "", values);
                IEnumerable<SimpleBuildingElementType> elements = group.BuildingElementsTypes;
                IEnumerable<SimpleGroup> subGroups = group.Children;
                if (elements.FirstOrDefault() != null || subGroups.FirstOrDefault() != null)
                {
                    str = str + "<ul>";
                    str = str + RenderElements(elements);
                    str = str + RenderGroups(subGroups);
                    str = str + "</ul>";
                }
                str = str + "</li>";
            }

            return str;
        }

        private static string RenderElements(IEnumerable<SimpleBuildingElementType> elements)
        {
            string str = "";

            Random rand = new Random();
            if (elements == null || elements.FirstOrDefault() == null) return "";
            foreach (SimpleBuildingElementType element in elements)
            {
                //skip element types with no elements inside
                //if (element.ElementsCarbonData.Count == 0) continue;

                string ifcTypeName = element.IfcType.GetType().Name;
                string title = ifcTypeName.Substring(3);
                long typeLabel = Math.Abs(element.IfcType.EntityLabel);
                str = str + "<li class='ifc-element-type'  title='" + title + "'>";
                str = str + "<input type='hidden' value='" + typeLabel + "' class='entityLabel'/>";
                str = str + "<input type='hidden' value='" + ifcTypeName + "' class='ifcTypeName'/>";
                string elementsLabels = null;
                string elementMeasures = null;
                //foreach (var item in element.ElementsCarbonData)
                //{
                //    long label = Math.Abs(item.Key.EntityLabel);
                //    if (elementsLabels == null) elementsLabels = label.ToString();
                //    else elementsLabels += ";" + label;

                //    string measure = "0";
                //    if (item.Value != null)
                //        measure = item.Value.Measure != null ? item.Value.Measure.ToString() : "0";
                //    if (elementMeasures == null) elementMeasures = measure;
                //    else elementMeasures += ";" + measure;
                //}
                str = str + "<input type='hidden' value='" + elementsLabels + "'  class='instanceEntityLabels'/>";
                str = str + "<input type='hidden' value='" + elementMeasures + "'  class='instanceMeasures'/>";

                string name = element.Name;
                if (string.IsNullOrEmpty(name))
                    name = "-";

                str = str + "<input type='hidden' value='" + name + "' class='typeName'/>";
                //str = str + "<input type='hidden' value='" + element.CarbonData.EmbodiedCO2e != null ? element.CarbonData.EmbodiedCO2e.Max : null + "' class='carbonMax'/>";

                //string carbonValueMax = String.Format("{0:0}", element.CarbonData.EmbodiedCO2e.Max ?? 0);
                //string carbonValueMin = String.Format("{0:0}", element.CarbonData.EmbodiedCO2e.Min ?? 0);
                //string percent = String.Format("{0:0}%", element.CarbonData.EmbodiedCO2e.Max / _projectMaxCarbon * 100);
                string[] values = new string[] {  };
                str = str + WriteRow(name, "", "", values);
                str = str + "</li>";

                
            }

            return str;
        }

        private static string WriteRow(string leftCaption, string captionCss, string rowCss, string[] values)
        {
            string str = "";

            str = str + "<span class='" + rowCss + " icim-data-row' style='display:block'>";
            str = str + WriteCells(values);
            str = str + WriteLeftCaption(leftCaption, captionCss);
            str = str + "</span>";

            return str;
        }

        private static string WriteCells(string[] values)
        {
            string str = "";

            str = str + "<span style='float:right' class='icim-data-cell'>";
            foreach (string value in values)
            {
                string content = value;
                if (String.IsNullOrEmpty(content))
                    content = "&nbsp;";
                str = str + WriteCell(content);
            }
            str = str + "</span>";

            return str;
        }

        private static string WriteLeftCaption(string content, string cssClass)
        {
            string str = "";

            str = str + "<span class='" + cssClass + "'>";
            if (String.IsNullOrEmpty(content))
                content = "&nbsp;";
            str = str + content;
            str = str + "</span>";

            return str;
        }

        private static string WriteCell(string content)
        {
            string str = "";

            str = str + "<span class='' style='display:table-cell; width:120px;'>";
            str = str + content;
            str = str + "</span>";

            return str;
        }
    }




    
}
