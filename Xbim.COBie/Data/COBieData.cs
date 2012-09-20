using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.UtilityResource;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.Ifc.ActorResource;
using Xbim.COBie.Rows;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.HVACDomain;
using Xbim.Ifc.ElectricalDomain;
using Xbim.Ifc.SharedComponentElements;
using Xbim.Ifc.StructuralElementsDomain;
using Xbim.Ifc.SharedBldgServiceElements;


namespace Xbim.COBie.Data
{
    /// <summary>
    /// Base class for the input of data into the Excel worksheets
    /// </summary>
    public abstract class COBieData<T> where T : COBieRow
    {

        protected const string DEFAULT_STRING = Constants.DEFAULT_STRING;
        protected const string DEFAULT_NUMERIC = Constants.DEFAULT_NUMERIC;

        protected COBieContext Context { get; set; }
        private COBieProgress _progressStatus;
        private static Dictionary<long, string> _eMails = new Dictionary<long, string>();

        protected COBieData()
        { }

        
        public COBieData(COBieContext context)
        {
            Context = context;
            _progressStatus = new COBieProgress(context);
        }

        protected IModel Model
        {
            get
            {
                return Context.Model;
            }
        }

        protected COBieProgress ProgressIndicator
        {
            get
            {
                return _progressStatus;
            }
        }
        
        

        /// <summary>
        /// Common exclude PropertySingleValue name containing any of the strings 
        /// </summary>
        public  List<string> _commonAttExcludes = new List<string>() {"Zone Base Offset", "Upper Limit",  
                            "Line Pattern", "Symbol","Window Inset", 
                            "Radius", "Phase Created","Phase", "Outside Radius", 
                            "Outside Diameter", "Omniclass", "Offset", "Mark",
                            "Recepticles", "Limit Offset", "Lighting Calculation Workplan",
                            "Size", "Level", "Host", "Hot Water Radius", 
                            "Half Oval", "AssetAccountingType", "Description",
                            "Name", "Classification Description", "Classification Code",
                            "Category Description","Category Code", "Uniclass Description",
                            "Uniclass Code", "Assembly Description", "Assembly Code",
                            "Omniclass Title", "Omniclass Number", "MethodOfMeasurement"};

        private List<Type> _componentExcludedTypes = new List<Type>{  typeof(IfcWall),
                                                        typeof(IfcAnnotation),
                                                        typeof(IfcWallStandardCase),
                                                        typeof(IfcSlab),
                                                        typeof(IfcBeam),
                                                        typeof(IfcSpace),
                                                        typeof(IfcBuildingStorey),
                                                        typeof(IfcBuilding),
                                                        typeof(IfcSite),
                                                        typeof(IfcProject),
                                                        typeof(IfcColumn),
                                                        typeof(IfcMember),
                                                        typeof(IfcPlate),
                                                        typeof(IfcRailing),
                                                        typeof(IfcStairFlight),
                                                        typeof(IfcCurtainWall),
                                                        typeof(IfcRampFlight),
                                                        typeof(IfcVirtualElement),
                                                        typeof(IfcFeatureElement),
                                                        typeof(IfcFastener),
                                                        typeof(IfcMechanicalFastener),
                                                        typeof(IfcElementAssembly),
                                                        typeof(IfcBuildingElementPart),
                                                        typeof(IfcReinforcingBar),
                                                        typeof(IfcReinforcingMesh),
                                                        typeof(IfcTendon),
                                                        typeof(IfcTendonAnchor),
                                                        typeof(IfcFooting),
                                                        typeof(IfcPile),
                                                        typeof(IfcRamp),
                                                        typeof(IfcRoof),
                                                        typeof(IfcStair),
                                                        typeof(IfcFlowFitting),
                                                        typeof(IfcFlowSegment),
                                                        typeof(IfcDistributionPort), 
                                                        typeof(IfcFeatureElementAddition), 
                                                        typeof(IfcProjectionElement), 
                                                        //typeof(IfcColumnStandardCase), //IFC2x Edition 4.
                                                        //typeof(IfcMemberStandardCase), //IFC2x Edition 4.
                                                        //typeof(IfcPlateStandardCase), //IFC2x Edition 4.
                                                        //typeof(IfcSlabElementedCase), //IFC2x Edition 4.
                                                        //typeof(IfcSlabElementedCase), //IFC2x Edition 4.
                                                        //typeof(IfcWallElementedCase), //IFC2x Edition 4.
                                                        //typeof(IfcCableCarrierSegment), //IFC2x Edition 4.
                                                        //typeof(IfcCableSegment), //IFC2x Edition 4.
                                                        //typeof(IfcDuctSegment), //IFC2x Edition 4.
                                                        //typeof(IfcPipeSegment), //IFC2x Edition 4.
                                                        };

        /// <summary>
        /// List of component class types to exclude from selection
        /// </summary>
        public List<Type> ComponentExcludeTypes
        {
            get { return _componentExcludedTypes; }
        }

        //TODO: after IfcRampType and IfcStairType are implemented then add to excludedTypes list
        private List<Type> _typeObjectExcludedTypes = new List<Type>{  typeof(IfcTypeProduct),
                                                            typeof(IfcElementType),
                                                            typeof(IfcBeamType),
                                                            typeof(IfcColumnType),
                                                            typeof(IfcCurtainWallType),
                                                            typeof(IfcMemberType),
                                                            typeof(IfcPlateType),
                                                            typeof(IfcRailingType),
                                                            typeof(IfcRampFlightType),
                                                            typeof(IfcSlabType),
                                                            typeof(IfcStairFlightType),
                                                            typeof(IfcWallType),
                                                            typeof(IfcDuctFittingType ),
                                                            typeof(IfcJunctionBoxType ),
                                                            typeof(IfcPipeFittingType),
                                                            typeof(IfcCableCarrierSegmentType),
                                                            typeof(IfcCableSegmentType),
                                                            typeof(IfcDuctSegmentType),
                                                            typeof(IfcPipeSegmentType),
                                                            typeof(IfcFastenerType),
                                                            typeof(IfcSpaceType),
                                                            //typeof(Xbim.Ifc.SharedBldgElements.IfcRampType), //IFC2x Edition 4.
                                                            //typeof(IfcStairType), //IFC2x Edition 4.
                                                             };
        /// <summary>
        /// List of type object class types to exclude from selection
        /// </summary>
        public List<Type> TypeObjectExcludeTypes
        {
            get { return _typeObjectExcludedTypes; }
        }

        /// <summary>
        /// field to hold the application name
        /// </summary>
        private  IfcApplication app = null;
        /// <summary>
        /// The application name
        /// </summary>
        public IfcApplication ifcApplication
        {
            get
            {
                if (app == null)  app = Model.InstancesOfType<IfcApplication>().FirstOrDefault(); 
                return app; 
            }
        }
        

        //list or classes to exclude if the related object of the IfcRelAggregates is one of these types
        private List<Type> _assemblyExcludeTypes = new List<Type>{  typeof(IfcSite),
                                                        typeof(IfcProject),
                                                        typeof(IfcBuilding),
                                                        typeof(IfcBuildingStorey)
                                                        };

        /// <summary>
        /// List of type object class types to exclude from selection
        /// </summary>
        public List<Type> AssemblyExcludeTypes
        {
            get 
            {
                List<Type> temp = new List<Type>();
                temp.AddRange(_assemblyExcludeTypes);
                //Assemblies should only be shown from the components and type sheets so add there exclusions 
                temp.AddRange(_typeObjectExcludedTypes); 
                temp.AddRange(_componentExcludedTypes);
                return temp;
            }
        }

        

        #region Methods

        public abstract COBieSheet<T> Fill();

        /// <summary>
        /// Extract the Created On date from the passed entity
        /// </summary>
        /// <param name="rootEntity">Entity to extract the Create On date</param>
        /// <returns></returns>
        protected string GetCreatedOnDateAsFmtString(IfcOwnerHistory ownerHistory)
        {
            const string strFormat = "yyyy-MM-dd HH:mm:ss";

            int createdOnTStamp = (int)ownerHistory.CreationDate;
            if (createdOnTStamp <= 0)
            {
                DateTime defaultDate = new DateTime(1900, 12, 31, 23, 59, 59);//1900-12-31T23:59:59
                return defaultDate.ToString(strFormat); //we have to return a date to comply. so now is used
            }
            else
            {
                //to remove trailing decimal seconds use a set format string as "o" option is to long.

                //We have a day light saving problem with the comparison with other COBie Export Programs. if we convert to local time we get a match
                //but if the time stamp is Coordinated Universal Time (UTC), then daylight time should be ignored. see http://msdn.microsoft.com/en-us/library/bb546099.aspx
                //IfcTimeStamp.ToDateTime(CreatedOnTStamp).ToLocalTime()...; //test to see if corrects 1 hour difference, and yes it did, but should we?

                return IfcTimeStamp.ToDateTime(createdOnTStamp).ToString(strFormat);
            }

        }

        
        

        /// <summary>
        /// Extract the email address lists for the owner of the IfcOwnerHistory passed
        /// </summary>
        /// <param name="ifcOwnerHistory">Entity to extract the email addresses for</param>
        /// <returns>string of comma delimited addresses</returns>
        protected string GetTelecomTelephoneNumber(IfcPersonAndOrganization ifcPersonAndOrganization)
        {
            string telephoneNo = "";
            IfcOrganization ifcOrganization = ifcPersonAndOrganization.TheOrganization;
            IfcPerson ifcPerson = ifcPersonAndOrganization.ThePerson;
                
            if (ifcPerson.Addresses != null)
            {
                telephoneNo = ifcPerson.Addresses.TelecomAddresses.Select(address => address.TelephoneNumbers).Where(item => item != null).SelectMany(em => em).Where(em => !string.IsNullOrEmpty(em)).FirstOrDefault();

                if (string.IsNullOrEmpty(telephoneNo))
                {
                    if (ifcOrganization.Addresses != null)
                    {
                        telephoneNo = ifcOrganization.Addresses.TelecomAddresses.Select(address => address.TelephoneNumbers).Where(item => item != null).SelectMany(em => em).Where(em => !string.IsNullOrEmpty(em)).FirstOrDefault();
                    }
                } 
            }
            
            //if still no email lets make one up
            if (string.IsNullOrEmpty(telephoneNo)) telephoneNo = DEFAULT_STRING;
            

            return telephoneNo;
        }

        /// <summary>
        /// Clear the email dictionary for next file
        /// </summary>
        public void ClearEMails()
        {
            _eMails.Clear();
        }

        /// <summary>
        /// Extract the email address lists for the owner of the IfcOwnerHistory passed
        /// </summary>
        /// <param name="ifcOwnerHistory">Entity to extract the email addresses for</param>
        /// <returns>string of comma delimited addresses</returns>
        protected string GetTelecomEmailAddress(IfcOwnerHistory ifcOwnerHistory)
        {
            if (ifcOwnerHistory != null)
            {
                IfcPerson ifcPerson = ifcOwnerHistory.OwningUser.ThePerson;
                if (_eMails.ContainsKey(ifcPerson.EntityLabel))
                {
                    return _eMails[ifcPerson.EntityLabel];
                }
                else
                {
                    IfcOrganization ifcOrganization = ifcOwnerHistory.OwningUser.TheOrganization;
                    return GetEmail(ifcOrganization, ifcPerson);
                }
            }
            else
                return DEFAULT_STRING;
        }
        /// <summary>
        /// Extract the email address lists for the owner of the IfcOwnerHistory passed
        /// </summary>
        /// <param name="ifcOwnerHistory">Entity to extract the email addresses for</param>
        /// <returns>string of comma delimited addresses</returns>
        protected string GetTelecomEmailAddress(IfcPersonAndOrganization ifcPersonAndOrganization)
        {
            if (ifcPersonAndOrganization != null)
            {
                IfcPerson ifcPerson = ifcPersonAndOrganization.ThePerson;
                if (_eMails.ContainsKey(ifcPerson.EntityLabel))
                {
                    return _eMails[ifcPerson.EntityLabel];
                }
                else
                {
                    IfcOrganization ifcOrganization = ifcPersonAndOrganization.TheOrganization;
                    return GetEmail(ifcOrganization, ifcPerson);
                }
            }
            else
                return DEFAULT_STRING;
        }

        /// <summary>
        /// Get email address from IfcPerson 
        /// </summary>
        /// <param name="ifcOrganization"></param>
        /// <param name="ifcPerson"></param>
        /// <returns></returns>
        private static string GetEmail( IfcOrganization ifcOrganization, IfcPerson ifcPerson)
        {
            string email = "";
            IEnumerable<IfcLabel> emails = Enumerable.Empty<IfcLabel>();
            if (ifcPerson.Addresses != null)
            {
                emails = ifcPerson.Addresses.TelecomAddresses.Select(address => address.ElectronicMailAddresses).Where(item => item != null).SelectMany(em => em).Where(em => !string.IsNullOrEmpty(em));
                if ((emails == null) || (emails.Count() == 0))
                {
                    if (ifcOrganization.Addresses != null)
                    {
                        emails = ifcOrganization.Addresses.TelecomAddresses.Select(address => address.ElectronicMailAddresses).Where(item => item != null).SelectMany(em => em).Where(em => !string.IsNullOrEmpty(em));
                    }
                }
                
            }

            //if still no email lets make one up
            if ((emails != null) && (emails.Count() > 0))
            {
                email = string.Join(" : ", emails);
            }
            else
            {
                email += (string.IsNullOrEmpty(ifcPerson.GivenName.ToString())) ? "unknown" : ifcPerson.GivenName.ToString();
                email += (string.IsNullOrEmpty(ifcPerson.FamilyName.ToString())) ? "unknown" : ifcPerson.FamilyName.ToString();
                email += "@";
                email += (string.IsNullOrEmpty(ifcOrganization.Name.ToString())) ? "unknown" : ifcOrganization.Name.ToString();
            }
            //save to the email directory for quick retrieval
            _eMails.Add(ifcPerson.EntityLabel, email);

            return email;
        }

        /// <summary>
        /// Converts string to formatted string and if it fails then passes 0 back, mainly to check that we always have a number returned
        /// </summary>
        /// <param name="num">string to convert</param>
        /// <returns>string converted to a formatted string using "F2" as formatter</returns>
        protected string ConvertNumberOrDefault(string num)
        {
            double temp;

            if (double.TryParse(num, out temp))
            {
                return temp.ToString("F2"); // two decimal places
            }
            else
            {
                return DEFAULT_NUMERIC; 
            }

        }

        /// <summary>
        /// Get Category method
        /// </summary>
        /// <param name="obj">Object to try and extract method from</param>
        /// <returns></returns>
        public string GetCategory(IfcObject obj)
        {
            //Try by relationship first
            IfcRelAssociatesClassification ifcRAC = obj.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
            if (ifcRAC != null)
            {
                IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                return ifcCR.Name;
            }
            //Try by PropertySet as fallback
            var query = from pSet in obj.PropertySets
                        from props in pSet.HasProperties
                        where props.Name.ToString() == "OmniClass Table 13 Category" || props.Name.ToString() == "Category Code"
                        select props.ToString().TrimEnd();
            string val = query.FirstOrDefault();

            if (!String.IsNullOrEmpty(val))
            {
                return val;
            }
            return Constants.DEFAULT_STRING;
        }
        /// <summary>
        /// Get Category method for property sets
        /// </summary>
        /// <param name="propSet">IfcPropertySet</param>
        /// <returns>Category as string </returns>
        protected string GetCategory(IfcPropertySet propSet)
        {
            IEnumerable<IfcClassificationReference> cats = from IRAC in propSet.HasAssociations
                                                           where IRAC is IfcRelAssociatesClassification
                                                           && ((IfcRelAssociatesClassification)IRAC).RelatingClassification is IfcClassificationReference
                                                           select ((IfcRelAssociatesClassification)IRAC).RelatingClassification as IfcClassificationReference;
            IfcClassificationReference cat = cats.FirstOrDefault();
            if (cat != null)
            {
                return cat.Name.ToString();
            }
            //Try by PropertySet as fallback
            var query = from props in propSet.HasProperties
                        where props.Name.ToString() == "OmniClass Table 13 Category" || props.Name.ToString() == "Category Code"
                        select props.ToString().TrimEnd();
            string val = query.FirstOrDefault();

            if (!String.IsNullOrEmpty(val))
            {
                return val;
            }
            return Constants.DEFAULT_STRING;
        }   

        /// <summary>
        /// Determined the sheet the IfcRoot will have come from using the object type
        /// </summary>
        /// <param name="ifcRoot">object which inherits from IfcRoot </param>
        /// <returns>string holding sheet name</returns>
        public string GetSheetByObjectType(IfcRoot ifcRoot)
        {
            string value = DEFAULT_STRING;
            if (ifcRoot is IfcTypeObject) value = "Type";
            else if (ifcRoot is IfcRelAggregates) value = "Component";
            else if (ifcRoot is IfcRelContainedInSpatialStructure) value = "Component";
            else if (ifcRoot is IfcElement) value = "Component";
            //more sheets as tests date becomes available
            return value;
        }
        
       
        /// <summary>
        /// Get the associated Type for a IfcObject, so a Door can be of type "Door Type A"
        /// </summary>
        /// <param name="obj">IfcObject to get associated type information from</param>
        /// <returns>string holding the type information</returns>
        protected string GetTypeName(IfcObject obj)
        {
            var elType = obj.IsDefinedBy.OfType<IfcRelDefinesByType>().FirstOrDefault();
            return (elType != null) ? elType.RelatingType.Name.ToString() : DEFAULT_STRING;
        }

        #endregion

    }

    #region IComparer Classes
    /// <summary>
    /// ICompare class for IfcLabels, used to order by 
    /// </summary>
    public class CompareIfcLabel : IComparer<IfcLabel?>
    {
        public int Compare(IfcLabel? x, IfcLabel? y)
        {
            return string.Compare(x.ToString(), y.ToString(), true); //ignore case set to true
        }
    }

    /// <summary>
    /// ICompare class for String, used to order by 
    /// </summary>
    public class CompareString : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return string.Compare(x, y, true); //ignore case set to true
        }
    }

 

    #endregion
}
