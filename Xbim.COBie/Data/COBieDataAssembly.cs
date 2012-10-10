using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.COBie.Rows;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ExternalReferenceResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.MaterialResource;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Assembly tab.
    /// </summary>
    public class COBieDataAssembly : COBieData<COBieAssemblyRow>
    {
        /// <summary>
        /// Data Assembly constructor
        /// </summary>
        /// <param name="model">The context of the model being generated</param>
        public COBieDataAssembly(COBieContext context) : base(context)
        { }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Assembly sheet
        /// </summary>
        /// <returns>COBieSheet<COBieAssemblyRow></returns>
        public override COBieSheet<COBieAssemblyRow> Fill()
        {
            ProgressIndicator.ReportMessage("Starting Assemblies...");
            //Create new sheet
            COBieSheet<COBieAssemblyRow> assemblies = new COBieSheet<COBieAssemblyRow>(Constants.WORKSHEET_ASSEMBLY);

            // get ifcRelAggregates objects from IFC file what are not in the excludedTypes type list
            IEnumerable<IfcRelAggregates> ifcRelAggregates = Model.InstancesOfType<IfcRelAggregates>();
            IEnumerable<IfcRelNests> ifcRelNests = Model.InstancesOfType<IfcRelNests>(); 
            IEnumerable<IfcRelAssociatesMaterial> ifcRelAssociatesMaterials = Model.InstancesOfType<IfcRelAssociatesMaterial>(); 

            IEnumerable<IfcRelDecomposes> relAll = (from ra in ifcRelAggregates
                                                    where ((ra.RelatingObject is IfcProduct) || (ra.RelatingObject is IfcTypeObject)) && !Context.AssemblyExcludeTypes.Contains(ra.RelatingObject.GetType())
                                                      select ra as IfcRelDecomposes).Union
                                                      (from rn in ifcRelNests
                                                       where ((rn.RelatingObject is IfcProduct) || (rn.RelatingObject is IfcTypeObject)) && !Context.AssemblyExcludeTypes.Contains(rn.RelatingObject.GetType())
                                                      select rn as IfcRelDecomposes);

            //filter ifcRelAssociatesMaterials list to relating objects held in relAll
            ifcRelAssociatesMaterials = from ifcram in ifcRelAssociatesMaterials
                      from ifcrs in relAll
                      where ifcram.RelatedObjects.Contains(ifcrs.RelatingObject) && ifcram.RelatingMaterial is IfcMaterialLayerSet
                      select ifcram;


            ProgressIndicator.Initialise("Creating Assemblies", relAll.Count());
            
            int childColumnLength = 0;
            foreach (IfcRelDecomposes ra in relAll)
            {
                ProgressIndicator.IncrementAndUpdate();
                COBieAssemblyRow assembly = new COBieAssemblyRow(assemblies);
                
                                           
                if (string.IsNullOrEmpty(ra.Name))
                {
                    if (!string.IsNullOrEmpty(ra.RelatingObject.Name))
                    {
                        assembly.Name = ra.RelatingObject.Name.ToString();
                        // try and get name from material layer set
                        if (string.IsNullOrEmpty(assembly.Name))
                        {
                            //get the IfcMaterialLayerSet associated with the relating object attached to the IfcRelDecomposes
                            IEnumerable<IfcMaterialLayerSet> ifcMaterialLayerSets = (from ifcram in ifcRelAssociatesMaterials
                                                                        where ifcram.RelatedObjects.Contains(ra.RelatingObject)
                                                                        select ifcram.RelatingMaterial).OfType<IfcMaterialLayerSet>();
                            string name = ifcMaterialLayerSets.FirstOrDefault().LayerSetName;
                            assembly.Name = (string.IsNullOrEmpty(name)) ? DEFAULT_STRING : name;
                        }
                    }
                    else
                        assembly.Name = DEFAULT_STRING;
                }
                else 
                    assembly.Name = ra.Name.ToString();

                
                assembly.CreatedBy = GetTelecomEmailAddress(ra.OwnerHistory);
                assembly.CreatedOn = GetCreatedOnDateAsFmtString(ra.OwnerHistory);

                assembly.SheetName = GetSheetByObjectType(ra.RelatingObject);
                assembly.ParentName = ra.RelatingObject.Name;
               
                
                assembly.AssemblyType = "Fixed"; //as Responsibility matrix instruction
                assembly.ExtSystem = GetExternalSystem(ra);
                assembly.ExtObject = ra.RelatingObject.GetType().Name;
                assembly.ExtIdentifier = string.IsNullOrEmpty(ra.GlobalId) ? DEFAULT_STRING : ra.GlobalId.ToString();
                
                assembly.Description = GetAssemblyDescription(ra);

                //get the assembly child names of objects that make up assembly
                if (childColumnLength == 0)  childColumnLength = assembly["ChildNames"].CobieCol.ColumnLength;                
                ChildNamesList childNames = GetChildNamesList(ra, childColumnLength);
                if (childNames.Count > 0)
                {
                    COBieAssemblyRow assemblyCont = null;
                    int index = 0;
                    foreach (string childStr in childNames)
	                {
                        if (index == 0)
                        {
                            assembly.ChildNames = childStr;
                            assemblies.Rows.Add(assembly);
                        }
                        else
                        {
                            assemblyCont = new COBieAssemblyRow(assemblies);
                            assemblyCont.Name = assembly.Name + " : continued " + index.ToString();
                            assemblyCont.CreatedBy = assembly.CreatedBy;
                            assemblyCont.CreatedOn = assembly.CreatedOn;
                            assemblyCont.SheetName = assembly.SheetName;
                            assemblyCont.ParentName = assembly.ParentName;
                            assemblyCont.AssemblyType = assembly.AssemblyType;
                            assemblyCont.ExtSystem = assembly.ExtSystem;
                            assemblyCont.ExtObject = assembly.ExtObject;
                            assemblyCont.ExtIdentifier = assembly.ExtIdentifier;
                            assemblyCont.Description = assembly.Description;
                            assemblyCont.ChildNames = childStr;
                            assemblies.Rows.Add(assemblyCont);
                        }
                        index = ++index;
	                }
                }
                else 
                    assemblies.Rows.Add(assembly);

                
            }
            ProgressIndicator.Finalise();
            return assemblies;
        }

        private string GetAssemblyDescription(IfcRelDecomposes ra)
        {
            if (ra != null)
            {
                if (!string.IsNullOrEmpty(ra.Description)) return ra.Description;
                else if (!string.IsNullOrEmpty(ra.Name)) return ra.Name;
                else if (!string.IsNullOrEmpty(ra.RelatingObject.Name)) return ra.RelatingObject.Name;
            }
            return Constants.DEFAULT_STRING;
        }
        /// <summary>
        /// Get list of child object names from relatedObjects property of a ifcProduct asset
        /// </summary>
        /// <param name="ra">IfcRelDecomposes relationship object</param>
        /// <returns>List of strings fixed to a string limit per string entry</returns>
        private ChildNamesList GetChildNamesList(IfcRelDecomposes ra, int fieldLength)
        {
            ChildNamesList childNamesFilter = new ChildNamesList();
            ChildNamesList childNames = new ChildNamesList();
            int strCount = 0;
            string fieldValue = "";
            //remove duplicates
            foreach (IfcObjectDefinition obj in ra.RelatedObjects)
            {
               
                if (!string.IsNullOrEmpty(obj.Name))
                {
                    if (!childNamesFilter.Contains(obj.Name))
                    {
                        childNamesFilter.Add(obj.Name);
                    }
                }
            }
            //build field length strings
            foreach (string str in childNamesFilter)
            {
                if (fieldValue == "")
                        strCount += str.Length;
                    else
                        strCount += str.Length + 3; //add 3 fro the " : "

                    if (strCount <= fieldLength)
                    {
                        fieldValue += " : " + str;
                    }
                    else
                    {
                        childNames.Add(fieldValue);
                        fieldValue = str; //start field value again with current value of the object name
                        strCount = str.Length; //reset strCount to the current value length
                    }
            }
            return childNames;
        }

        #endregion
    }

    public class ChildNamesList : List<string>{}
}
