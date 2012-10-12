using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.ActorResource;
using Xbim.Ifc2x3.UtilityResource;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions.DataProviders;
using System.IO;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.Extensions;
using System.CodeDom.Compiler;
using Xbim.XbimExtensions.SelectTypes;
using System.Collections;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;
using System.IO.Compression;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.GeometryResource;
using System.Linq.Expressions;
using System.Diagnostics;
using Xbim.XbimExtensions.Transactions.Extensions;
using Xbim.Common.Logging;
using Xbim.IO.Parser;
using Xbim.Common;
using Xbim.Common.Exceptions;
namespace Xbim.IO
{
    /// <summary>
    /// General Model class for memory based model suport
    /// </summary>
    public class XbimModel : IModel, IDisposable
    {

        #region Fields

      

        #region Logging Fields

        protected readonly static ILogger Logger = LoggerFactory.GetLogger();

        #endregion

        #region Model state fields
        
        protected IfcPersistedInstanceCache Cached;
        protected UndoRedoSession undoRedoSession;
        protected IIfcFileHeader header;
        private bool disposed = false;
        private XbimModelFactors _modelFactors;
        private XbimInstanceCollection instances;

        #endregion
        
        #endregion

        public XbimModel()
        {
            Cached = new IfcPersistedInstanceCache(this);
            instances = new XbimInstanceCollection(this.Cached);
        }
        public string DatabaseName
        {
            get { return Cached.DatabaseName; }
        }

        public IXbimInstanceCollection Instances
        {
            get { return instances; }
            
        }

        public XbimModelFactors GetModelFactors
        {
            get
            {
                if (_modelFactors == null)
                {
                    double angleToRadiansConversionFactor = 0.0174532925199433; //assume degrees
                    double lengthToMetresConversionFactor = 1; //assume metres

                    IfcUnitAssignment ua = Instances.OfType<IfcUnitAssignment>().FirstOrDefault();
                    if (ua != null)
                    {

                        foreach (var unit in ua.Units)
                        {
                            double value = 1.0;
                            IfcConversionBasedUnit cbUnit = unit as IfcConversionBasedUnit;
                            IfcSIUnit siUnit = unit as IfcSIUnit;
                            if (cbUnit != null)
                            {
                                IfcMeasureWithUnit mu = cbUnit.ConversionFactor;
                                if (mu.UnitComponent is IfcSIUnit)
                                    siUnit = (IfcSIUnit)mu.UnitComponent;
                                ExpressType et = ((ExpressType)mu.ValueComponent);

                                if (et.UnderlyingSystemType == typeof(double))
                                    value *= (double)et.Value;
                                else if (et.UnderlyingSystemType == typeof(int))
                                    value *= (double)((int)et.Value);
                                else if (et.UnderlyingSystemType == typeof(long))
                                    value *= (double)((long)et.Value);
                            }
                            if (siUnit != null)
                            {
                                value *= siUnit.Power();
                                switch (siUnit.UnitType)
                                {
                                    case IfcUnitEnum.LENGTHUNIT:

                                        lengthToMetresConversionFactor = value;
                                        break;
                                    case IfcUnitEnum.PLANEANGLEUNIT:
                                        angleToRadiansConversionFactor = value;
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }

                    _modelFactors = new XbimModelFactors(angleToRadiansConversionFactor, lengthToMetresConversionFactor);
                }
                return _modelFactors;
            }
        }
        /// <summary>
        /// Starts a transaction to allow bulk updates on the geometry table, FreeGeometry Table should be called when no longer required
        /// </summary>
        /// <returns></returns>
        internal XbimGeometryCursor GetGeometryTable()
        {
            return Cached.GetGeometryTable();
        }

        /// <summary>
        /// Returns the table to the cache for reuse
        /// </summary>
        /// <param name="table"></param>
        internal void FreeTable(XbimGeometryCursor table)
        {
            Cached.FreeTable(table);
        }

        /// <summary>
        /// Returns the table to the cache for reuse
        /// </summary>
        /// <param name="table"></param>
        internal void FreeTable(XbimEntityCursor table)
        {
            Cached.FreeTable(table);
        }

        //Loads the property data of an entity, if it is not already loaded
        int IModel.Activate(IPersistIfcEntity entity, bool write)
        {
          
            if (write) //we want to activate for reading
            {
                if (!Transaction.IsRollingBack)
                   Cached.AddModified(entity);
            }
            else //we want to read so load from db if necessary
            {
                Cached.Activate(entity);
            }
            return Math.Abs(entity.EntityLabel);
        }

        #region Transaction support
        
       /// <summary>
       /// Set up the owner history objects for add, delete and modify operations
       /// </summary>
        private void InitialiseDefaultOwnership()
        {
            //IfcPerson person = New<IfcPerson>();

            //IfcOrganization organization = New<IfcOrganization>();
            //IfcPersonAndOrganization owninguser = New<IfcPersonAndOrganization>(po =>
            //{
            //    po.TheOrganization = organization;
            //    po.ThePerson = person;
            //});
            //Transaction.AddPropertyChange<IfcPersonAndOrganization>(m => _defaultOwningUser = m, _defaultOwningUser, owninguser);
            //IfcApplication app = New<IfcApplication>(a => a.ApplicationDeveloper = New<IfcOrganization>());
            //Transaction.AddPropertyChange<IfcApplication>(m => _defaultOwningApplication = m, _defaultOwningApplication, app);
            //IfcOwnerHistory oh = New<IfcOwnerHistory>();
            //oh.OwningUser = _defaultOwningUser;
            //oh.OwningApplication = _defaultOwningApplication;
            //oh.ChangeAction = IfcChangeActionEnum.ADDED;
            //Transaction.AddPropertyChange<IfcOwnerHistory>(m => _ownerHistoryAddObject = m, _ownerHistoryAddObject, oh);
            //_defaultOwningUser = owninguser;
            //_defaultOwningApplication = app;
            //_ownerHistoryAddObject = oh;
            //IfcOwnerHistory ohc = New<IfcOwnerHistory>();
            //ohc.OwningUser = _defaultOwningUser;
            //ohc.OwningApplication = _defaultOwningApplication;
            //ohc.ChangeAction = IfcChangeActionEnum.MODIFIED;
            //Transaction.AddPropertyChange<IfcOwnerHistory>(m => _ownerHistoryModifyObject = m, _ownerHistoryModifyObject, ohc);
            //_defaultOwningUser = owninguser;
            //_defaultOwningApplication = app;
            //_ownerHistoryModifyObject = ohc;
        }

        protected Transaction BeginEdit(string operationName)
        {
            if (undoRedoSession == null)
            {
                undoRedoSession = new UndoRedoSession();
                Transaction txn = undoRedoSession.Begin(operationName);
                InitialiseDefaultOwnership();
                return txn;
            }
            else return null;
        }

        public XbimReadWriteTransaction BeginTransaction()
        {
            return this.BeginTransaction(null);
        }

        public XbimReadWriteTransaction BeginTransaction(string operationName)
        {
           
            return new XbimReadWriteTransaction(Cached);
            //Transaction txn = BeginEdit(operationName);
            ////Debug.Assert(ToWrite.Count == 0);
            //if (txn == null) txn = undoRedoSession.Begin(operationName);
            ////txn.Finalised += TransactionFinalised;
            ////txn.Reversed += TransactionReversed;
            //return txn;
        }
 
        public IfcOwnerHistory OwnerHistoryModifyObject
        {
            get
            {
                return instances.OwnerHistoryModifyObject;
            }
        }

        public IfcOwnerHistory OwnerHistoryAddObject
        {
            get
            {
                return instances.OwnerHistoryAddObject;
            }
        }

        public IfcOwnerHistory OwnerHistoryDeleteObject
        {
            get
            {
                return instances.OwnerHistoryDeleteObject;
            }
        }

        

        public IfcApplication DefaultOwningApplication
        {
            get { return instances.DefaultOwningApplication; }
        }

        public IfcPersonAndOrganization DefaultOwningUser
        {
            get { return instances.DefaultOwningUser; }
        }
        #endregion

        #region IModel interface implementation
        

  
        /// <summary>
        /// Registers an entity for deletion
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public void Delete(IPersistIfcEntity instance)
        {
            Cached.Delete_Reversable(instance);
        }

       
       


        /// <summary>
        /// Returns an instance from the Model with the corresponding label but does not keep a cache of it
        /// This is a dangerous call as duplicate instances of the same object could happen
        /// Ony use when interating over the whole database for export etc
        /// The properties of the object are also loaded to improve performance
        /// If the instance is in the cache it is returned
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        internal IPersistIfcEntity GetInstanceVolatile(int label)
        {
            return Cached.GetInstance(label, true, true);
        }


       
        /// <summary>
        /// Returns the total number of Geometry objects in the model
        /// </summary>
        public virtual long GeometriesCount
        {
            get
            {
                return Cached.GeometriesCount();
            }
        }



        /// <summary>
        /// Creates a new Model and populates with instances from the specified file, Ifc, IfcXML, IfcZip and Xbim are all supported.
        /// </summary>
        /// <param name="importFrom">Name of the file containing the instances to import</param>
        /// /// <param name="xbimDbName">Name of the xbim file that will be created. 
        /// If null the contents are loaded into memory and are not persistent
        /// </param>
        /// <returns></returns>
        public bool CreateFrom(string importFrom, string xbimDbName = null, ReportProgressDelegate progDelegate = null)
        {
            Close();
            string fullPath = Path.GetFullPath(importFrom);
            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
                throw new DirectoryNotFoundException(Path.GetDirectoryName(importFrom) + " directory was not found");
            if (!File.Exists(fullPath))
                throw new FileNotFoundException(fullPath + " file was not found");
            if (string.IsNullOrWhiteSpace(xbimDbName))
                xbimDbName = Path.ChangeExtension(importFrom, "xBIM");
            
            XbimStorageType toImportStorageType = StorageType(importFrom);
            switch (toImportStorageType)
            {
                case XbimStorageType.IFCXML:
                    Cached.ImportIfcXml(xbimDbName, importFrom, progDelegate);
                    break;
                case XbimStorageType.IFC:
                    Cached.ImportIfc(xbimDbName, importFrom, progDelegate);
                    break;
                case XbimStorageType.IFCZIP:
                    Cached.ImportIfcZip(importFrom, progDelegate);
                    break;
                case XbimStorageType.XBIM:
                    Cached.ImportXbim(importFrom, progDelegate);
                    break;
                case XbimStorageType.INVALID:
                default:
                    return false;
            }
            return true;
        }
        


        /// <summary>
        /// Compacts an Xbim file to reduce size, database must be closed before this is called
        /// </summary>
        /// <returns></returns>
        public static bool Compact(string sourceFileName, string targetFileName, out string errMsg)
        {

            errMsg = "";
            try
            {
                XbimModel sourceModel = new XbimModel();
                sourceModel.Open(sourceFileName, XbimDBAccess.Exclusive);
                XbimModel targetModel = new XbimModel();
                if (!targetModel.Create(targetFileName))
                    throw new XbimException("Error Creating database " + targetFileName);
                targetModel.Open(sourceFileName, XbimDBAccess.Exclusive);
                sourceModel.Compact(targetModel);
                return false;
            }
            catch (Exception e)
            {
                errMsg = e.Message;
                File.Delete(targetFileName);
                return false;
            }

        }

       /// <summary>
        ///  Creates a new Xbim Database 
       /// </summary>
       /// <param name="dbFileName">Name of the Xbim file</param>
       /// <returns></returns>
        public bool Create(string dbFileName)
        {
            try
            {
                Close();
                Cached.CreateDatabase(dbFileName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }


        #endregion


        public byte[] GetEntityBinaryData(IPersistIfcEntity entity)
        {
           
            if (entity.Activated ) //we have it in memory but not written to store yet
            { 
                MemoryStream entityStream = new MemoryStream(4096);
                BinaryWriter entityWriter = new BinaryWriter(entityStream);
                entity.WriteEntity(entityWriter);
                return entityStream.ToArray();
            }
            else //it is in a persisted cache but hasn't been loaded yet
            { 
                return Cached.GetEntityBinaryData(entity);
            }
        }

       
        


       


        public IIfcFileHeader Header
        {

            get { return header; }
            set { header = value; }
        }

       
        #region Validation

        public string Validate(ValidationFlags validateFlags)
        {
            StringBuilder sb = new StringBuilder();
            TextWriter tw = new StringWriter(sb);
            Validate(tw, null, validateFlags);
            return sb.ToString();
        }

        /// <summary>
        ///   Only executes the flagged validation routines
        /// </summary>
        /// <param name = "errStream"></param>
        /// <param name = "progressDelegate"></param>
        /// <param name = "validateFlags"></param>
        /// <returns></returns>
        public int Validate(TextWriter errStream, ReportProgressDelegate progressDelegate, ValidationFlags validateFlags)
        {
            IndentedTextWriter tw = new IndentedTextWriter(errStream, "    ");
            tw.Indent = 0;
            double total = Instances.Count;
            int idx = 0;
            int errors = 0;
            int percentage = -1;

            foreach (XbimInstanceHandle handle in instances.Handles())
            {
                idx++;
                IPersistIfcEntity ent = GetInstanceVolatile(handle.EntityLabel);
                errors += Validate(ent, tw, validateFlags);

                if (progressDelegate != null)
                {
                    int newPercentage = (int)(idx / total * 100.0);
                    if (newPercentage != percentage) progressDelegate(percentage, "");
                    percentage = newPercentage;
                }
            }
            return errors;
        }

        /// <summary>
        ///   Executes all validation routines and reports progress
        /// </summary>
        /// <param name = "errStream"></param>
        /// <param name = "progressDelegate"></param>
        /// <returns></returns>
        public int Validate(TextWriter errStream, ReportProgressDelegate progressDelegate)
        {
            return Validate(errStream, progressDelegate, ValidationFlags.All);
        }

        /// <summary>
        ///   Validates the all aspects of all model instances
        /// </summary>
        /// <param name = "errStream"></param>
        /// <returns></returns>
        public int Validate(TextWriter errStream)
        {
            return Validate(errStream, null, ValidationFlags.All);
        }

        public static int Validate(IPersistIfcEntity ent, IndentedTextWriter tw, ValidationFlags validateLevel)
        {
            if (validateLevel == ValidationFlags.None) return 0; //nothing to do
            IfcType ifcType = IfcMetaData.IfcType(ent);
            bool notIndented = true;
            int errors = 0;
            if (validateLevel == ValidationFlags.Properties || validateLevel == ValidationFlags.All)
            {
                foreach (IfcMetaProperty ifcProp in ifcType.IfcProperties.Values)
                {
                    string err = GetIfcSchemaError(ent, ifcProp);
                    if (!String.IsNullOrEmpty(err))
                    {
                        if (notIndented)
                        {
                            tw.WriteLine(string.Format("#{0} - {1}", ent.EntityLabel, ifcType.Type.Name));
                            tw.Indent++;
                            notIndented = false;
                        }
                        tw.WriteLine(err.Trim('\n'));
                        errors++;
                    }
                }
            }
            if (validateLevel == ValidationFlags.Inverses || validateLevel == ValidationFlags.All)
            {
                foreach (IfcMetaProperty ifcInv in ifcType.IfcInverses)
                {
                    string err = GetIfcSchemaError(ent, ifcInv);
                    if (!String.IsNullOrEmpty(err))
                    {
                        if (notIndented)
                        {
                            tw.WriteLine(string.Format("#{0} - {1}", ent.EntityLabel, ifcType.Type.Name));
                            tw.Indent++;
                            notIndented = false;
                        }
                        tw.WriteLine(err.Trim('\n'));
                        errors++;
                    }
                }
            }

            string str = ent.WhereRule();
            if (!String.IsNullOrEmpty(str))
            {
                if (notIndented)
                {
                    tw.WriteLine(string.Format("#{0} - {1}", ent.EntityLabel, ifcType.Type.Name));
                    tw.Indent++;
                    notIndented = false;
                }
                tw.WriteLine(str.Trim('\n'));
                errors++;
            }
            if (!notIndented) tw.Indent--;
            return errors;
        }

        private static string GetIfcSchemaError(IPersistIfc instance, IfcMetaProperty prop)
        {
            //IfcAttribute ifcAttr, object instance, object propVal, string propName

            IfcAttribute ifcAttr = prop.IfcAttribute;
            object propVal = prop.PropertyInfo.GetValue(instance, null);
            string propName = prop.PropertyInfo.Name;

            if (propVal is ExpressType)
            {
                string err = "";
                string val = ((ExpressType)propVal).ToPart21;
                if (ifcAttr.State == IfcAttributeState.Mandatory && val == "$")
                    err += string.Format("{0}.{1} is not optional", instance.GetType().Name, propName);
                err += ((IPersistIfc)propVal).WhereRule();
                if (!string.IsNullOrEmpty(err)) return err;
            }

            if (ifcAttr.State == IfcAttributeState.Mandatory && propVal == null)
                return string.Format("{0}.{1} is not optional", instance.GetType().Name, propName);
            if (ifcAttr.State == IfcAttributeState.Optional && propVal == null)
                //if it is null and optional then it is ok
                return null;
            if (ifcAttr.IfcType == IfcAttributeType.Set || ifcAttr.IfcType == IfcAttributeType.List ||
                ifcAttr.IfcType == IfcAttributeType.ListUnique)
            {
                if (ifcAttr.MinCardinality < 1 && ifcAttr.MaxCardinality < 0) //we don't care how many so don't check
                    return null;
                ICollection coll = propVal as ICollection;
                int count = 0;
                if (coll != null)
                    count = coll.Count;
                else
                {
                    IEnumerable en = (IEnumerable)propVal;

                    foreach (object item in en)
                    {
                        count++;
                        if (count >= ifcAttr.MinCardinality && ifcAttr.MaxCardinality == -1)
                            //we have met the requirements
                            break;
                        if (ifcAttr.MaxCardinality > -1 && count > ifcAttr.MaxCardinality) //we are out of bounds
                            break;
                    }
                }

                if (count < ifcAttr.MinCardinality)
                {
                    return string.Format("{0}.{1} must have at least {2} item(s). It has {3}", instance.GetType().Name,
                                         propName, ifcAttr.MinCardinality, count);
                }
                if (ifcAttr.MaxCardinality > -1 && count > ifcAttr.MaxCardinality)
                {
                    return string.Format("{0}.{1} must have no more than {2} item(s). It has at least {3}",
                                         instance.GetType().Name, propName, ifcAttr.MaxCardinality, count);
                }
            }
            return null;
        }

        #endregion


        #region Part 21 parse functions
        

        private IPersistIfc _part21Parser_EntityCreate(string className, long? label, bool headerEntity,
                                                     out int[] reqParams)
        {
            reqParams = null;
            if (headerEntity)
            {
                switch (className)
                {
                    case "FILE_DESCRIPTION":
                        return new FileDescription();
                    case "FILE_NAME":
                        return new FileName();
                    case "FILE_SCHEMA":
                        return new FileSchema();
                    default:
                        throw new ArgumentException(string.Format("Invalid Header entity type {0}", className));
                }
            }
            else
                return CreateInstance(className, label);
        }

        
        #endregion


        #region Ifc Schema Validation Methods

        public string WhereRule()
        {
            if (this.IfcProject == null)
                return "WR1 Model: A Model must have a valid Project attribute";
            return "";
        }

        #endregion


        #region General Model operations



        /// <summary>
        /// Closes the current model and releases all resources and instances
        /// </summary>
        public void Close()
        {
            this._modelFactors = null;
            this.undoRedoSession = null;
            this.header = null;
            Cached.Close();
        }

       

        #endregion




        /// <summary>
        /// Opens an Xbim model only, to open Ifc, IfcZip and IfcXML files use the CreatFrom method
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="accessMode"></param>
        /// <param name="progDelegate"></param>
        /// <returns>True if successful</returns>
        public bool Open(string fileName, XbimDBAccess accessMode = XbimDBAccess.Read, ReportProgressDelegate progDelegate = null)
        {
            try
            {
                Close();
                Cached.Open(fileName, accessMode); //opens the database
                return true;
            }
            catch (Exception e)
            {
                Logger.WarnFormat("Error opening file {0}\n{1}", fileName, e.Message);
                return false;
            }
        }


        public bool SaveAs(string outputFileName, XbimStorageType storageType, ReportProgressDelegate progress = null)
        {

            try
            {
                Cached.SaveAs(storageType, outputFileName, progress);
                return true;
            }
            catch (Exception e )
            {
                Logger.ErrorFormat("Failed to Save file as {0}\n{1}", outputFileName, e.Message);
                return false;
            }
        }


        public UndoRedoSession UndoRedo
        {
            get { return undoRedoSession; }
        }

        /// <summary>
        ///   Returns the number of instances of a specific type, NB does not include subtypes
        /// </summary>
        /// <param name = "t"></param>
        /// <returns></returns>
        public long InstancesOfTypeCount(Type t)
        {
           return Cached.InstancesOfTypeCount(t);
        }



        // Extract first ifc file from zipped file and save in the same directory
        internal string ExportZippedIfc(string inputIfcFile)
        {
            try
            {
                using (ZipInputStream zis = new ZipInputStream(File.OpenRead(inputIfcFile)))
                {
                    ZipEntry zs = zis.GetNextEntry();
                    while (zs != null)
                    {
                        String filePath = Path.GetDirectoryName(zs.Name);
                        String fileName = Path.GetFileName(zs.Name);
                        if (fileName.ToLower().EndsWith(".ifc"))
                        {
                            using (FileStream fs = File.Create(fileName))
                            {
                                int i = 2048;
                                byte[] b = new byte[i];
                                while (true)
                                {
                                    i = zis.Read(b, 0, b.Length);
                                    if (i > 0)
                                        fs.Write(b, 0, i);
                                    else
                                        break;
                                }
                            }
                            return fileName;
                        }
                    }

                }
            }
            catch (Exception e)
            {
                throw new Exception("Error creating Ifc File from ZIP = " + inputIfcFile, e);
            }
            return "";
        }

       
       


        #region Part21 File Writer support
        
        
        /// <summary>
        /// Writes a Part 21 Header
        /// </summary>
        /// <param name="tw"></param>
        private void WriteHeader(TextWriter tw)
        {
            //FileDescription fileDescriptor = new FileDescription("2;1");
            IIfcFileDescription fileDescriptor = Header.FileDescription;
            IIfcFileName fileName = Header.FileName;

            IIfcFileSchema fileSchema = new FileSchema("IFC2X3");
            StringBuilder header = new StringBuilder();
            header.AppendLine("ISO-10303-21;");
            header.AppendLine("HEADER;");
            //FILE_DESCRIPTION
            header.Append("FILE_DESCRIPTION((");
            int i = 0;
            foreach (string item in fileDescriptor.Description)
            {
                header.AppendFormat(@"{0}'{1}'", i == 0 ? "" : ",", item);
                i++;
            }
            header.AppendFormat(@"),'{0}');", fileDescriptor.ImplementationLevel);
            header.AppendLine();
            //FileName
            header.Append("FILE_NAME(");
            header.AppendFormat(@"'{0}'", fileName.Name);
            header.AppendFormat(@",'{0}'", fileName.TimeStamp);
            header.Append(",(");
            i = 0;
            foreach (string item in fileName.AuthorName)
            {
                header.AppendFormat(@"{0}'{1}'", i == 0 ? "" : ",", item);
                i++;
            }
            header.Append("),(");
            i = 0;
            foreach (string item in fileName.Organization)
            {
                header.AppendFormat(@"{0}'{1}'", i == 0 ? "" : ",", item);
                i++;
            }
            header.AppendFormat(@"),'{0}','{1}','{2}');", fileName.PreprocessorVersion, fileName.OriginatingSystem,
                                fileName.AuthorizationName);
            header.AppendLine();
            //FileSchema
            header.AppendFormat("FILE_SCHEMA(('{0}'));", fileSchema.Schemas.FirstOrDefault());
            header.AppendLine();
            header.AppendLine("ENDSEC;");
            header.AppendLine("DATA;");
            tw.Write(header.ToString());
        }

        /// <summary>
        /// Writes the Part 21 Footer
        /// </summary>
        /// <param name="tw"></param>
        private void WriteFooter(TextWriter tw)
        {
            tw.WriteLine("ENDSEC;");
            tw.WriteLine("END-ISO-10303-21;");
        }


        #endregion

        #region Helpers

        private XbimStorageType StorageType(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return XbimStorageType.INVALID;
            string ext = Path.GetExtension(fileName).ToLower();
            if (ext == ".xbim") return XbimStorageType.XBIM;
            else if (ext == ".ifc") return XbimStorageType.IFC;
            else if (ext == ".ifcxml") return XbimStorageType.IFCXML;
            else if (ext == ".zip" || ext == ".ifczip") return XbimStorageType.IFCZIP;
            else
                return XbimStorageType.INVALID;
        }

        #endregion





        /// <summary>
        ///   Creates an Ifc Persistent Instance from an entity name string and label, this is NOT an undoable operation
        /// </summary>
        /// <param name = "ifcEntityName">Ifc Entity Name i.e. IFCDOOR, IFCWALL, IFCWINDOW etc. Name must be in uppercase</param>
        /// <returns></returns>
        internal IPersistIfc CreateInstance(string ifcEntityName, long? label)
        {
            try
            {
                IfcType ifcType = IfcMetaData.IfcType(ifcEntityName);
                return CreateInstance(ifcType.Type, label);
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format("Error creating entity {0}, it is not a supported Xbim type, {1}", ifcEntityName, e.Message));
            }

        }
        /// <summary>
        /// Creates an Ifc Persistent Instance from an entity type and label, this is NOT an undoable operation
        /// </summary>
        /// <param name="ifcType"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        internal IPersistIfc CreateInstance(Type ifcType, long ?label)
        {

            throw new NotImplementedException("To do");
             //return instances.AddNew(this,ifcType,label.Value);

        }



        public void Print()
        {
            Cached.Print();
        }






        public IPersistIfcEntity IfcProject
        {
            get 
            {
                return Cached == null ? null : Cached.OfType<IfcProject>().FirstOrDefault(); 
            }
        }

        public IEnumerable<IPersistIfcEntity> IfcProducts
        {
            get { return Cached == null ? null : Cached.OfType<IfcProduct>(); }
        }

        IPersistIfcEntity IModel.OwnerHistoryAddObject
        {
            get { throw new NotImplementedException(); }
        }

        IPersistIfcEntity IModel.OwnerHistoryModifyObject
        {
            get { throw new NotImplementedException(); }
        }

        IPersistIfcEntity IModel.DefaultOwningApplication
        {
            get { throw new NotImplementedException(); }
        }

        IPersistIfcEntity IModel.DefaultOwningUser
        {
            get { throw new NotImplementedException(); }
        }

        public int Validate(TextWriter errStream, ReportProgressDelegate progressDelegate, ValidationFlags? validateFlags = null)
        {
            throw new NotImplementedException();
        }

        ~XbimModel()
        {
            Dispose(false);
        }
        
        public void Dispose()
        {
            Dispose(true);
            // Take yourself off the Finalization queue 
            // to prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }
        
        protected void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                  Close();
            }
            disposed = true;
        }



        internal XbimGeometryData GetGeometryData(IfcProduct product, XbimGeometryType geomType)
        {
            return Cached.GetGeometry(product, geomType);
        }
        public IDictionary<string,XbimViewDefinition> Views
        {
            get
            {
                Dictionary<string, XbimViewDefinition> views = new Dictionary<string, XbimViewDefinition>();
                views.Add("Default", new XbimViewDefinition());
                return views;
            }
        }

        public IEnumerable<XbimGeometryData> Shapes(XbimGeometryType ofType)
        {
            foreach (var shape in Cached.Shapes(ofType))
            {
                yield return shape;
            }
          
        }



        internal XbimEntityCursor GetEntityTable()
        {
            return Cached.GetEntityTable();
        }

        internal void Compact(XbimModel targetModel)
        {
            foreach (var prod in Instances.OfType<IfcProduct>())
            {
                targetModel.InsertCopy(prod);
            }
        }

        /// <summary>
        /// Inserts a deep copy of the toCopy object into this model
        /// All property values are copied to the maximum depth
        /// Objects are not duplicated, if repeated copies are to be performed use the version with the 
        /// mapping argument to ensure objects are not duplicated
        /// </summary>
        /// <param name="toCopy"></param>
        /// <returns></returns>
        public T InsertCopy<T>(T toCopy, bool includeInverses = false) where T : IPersistIfcEntity
        {
            return InsertCopy(toCopy, new XbimInstanceHandleMap(toCopy.ModelOf, this), includeInverses);
        }

        /// <summary>
        /// Inserts a deep copy of the toCopy object into this model
        /// All property values are copied to the maximum depth
        /// Inverse properties are not copied
        /// </summary>
        /// <param name="toCopy">Instance to copy</param>
        /// <param name="mappings">Supply a dictionary of mappings if repeat copy insertions are to be made</param>
        /// <returns></returns>
        public T InsertCopy<T>(T toCopy, XbimInstanceHandleMap mappings, bool includeInverses = false) where T : IPersistIfcEntity
        {
            return Cached.InsertCopy<T>(toCopy, mappings, includeInverses);
        }

      

      
    }
}
