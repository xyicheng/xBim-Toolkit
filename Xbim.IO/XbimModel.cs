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
namespace Xbim.IO
{
    /// <summary>
    /// General Model class for memory based model suport
    /// </summary>
    public class XbimModel : IModel, IDisposable
    {

        #region Fields  

        #region Static fields

        #endregion
        
        #region OwnerHistory Fields


        [NonSerialized]
        private IfcOwnerHistory _ownerHistoryDeleteObject;

        [NonSerialized]
        private IfcOwnerHistory _ownerHistoryAddObject;

        [NonSerialized]
        private IfcOwnerHistory _ownerHistoryModifyObject;

        [NonSerialized]
        private IfcPersonAndOrganization _defaultOwningUser;

        [NonSerialized]
        private IfcApplication _defaultOwningApplication;
        #endregion

        #region Logging Fields
        
        protected readonly static ILogger Logger = LoggerFactory.GetLogger();

        #endregion
        
        #region Model state fields
        [NonSerialized]
        protected IfcPersistedInstanceCache Cached;
       
        [NonSerialized]
        protected UndoRedoSession undoRedoSession;

        #endregion

        protected IIfcFileHeader header;
        private string _databaseName;

        private XbimStorageType _storageType;
        private bool disposed = false;

        public string DatabaseName
        {
            get { return _databaseName; }
        }
        

        #endregion
        #region IModel implementation
        
        #endregion
        private XbimModelFactors _modelFactors;

        public XbimModelFactors GetModelFactors
        {
            get
            {
                if (_modelFactors == null)
                {
                    double angleToRadiansConversionFactor = 0.0174532925199433; //assume degrees
                    double lengthToMetresConversionFactor = 1; //assume metres

                    IfcUnitAssignment ua = InstancesOfType<IfcUnitAssignment>().FirstOrDefault();
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
        internal XbimGeometryTable GetGeometryTable()
        {
            return Cached.GetGeometryTable();
        }

        /// <summary>
        /// Returns the table to the cache for reuse
        /// </summary>
        /// <param name="table"></param>
        internal void FreeTable(XbimGeometryTable table)
        {
            Cached.FreeTable(table);
        }

        /// <summary>
        /// Returns the table to the cache for reuse
        /// </summary>
        /// <param name="table"></param>
        internal void FreeTable(XbimEntityTable table)
        {
            Cached.FreeTable(table);
        }

        //Loads the property data of an entity, if it is not already loaded
        public virtual int Activate(IPersistIfcEntity entity, bool write)
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
            IfcPerson person = New<IfcPerson>();

            IfcOrganization organization = New<IfcOrganization>();
            IfcPersonAndOrganization owninguser = New<IfcPersonAndOrganization>(po =>
            {
                po.TheOrganization = organization;
                po.ThePerson = person;
            });
            Transaction.AddPropertyChange<IfcPersonAndOrganization>(m => _defaultOwningUser = m, _defaultOwningUser, owninguser);
            IfcApplication app = New<IfcApplication>(a => a.ApplicationDeveloper = New<IfcOrganization>());
            Transaction.AddPropertyChange<IfcApplication>(m => _defaultOwningApplication = m, _defaultOwningApplication, app);
            IfcOwnerHistory oh = New<IfcOwnerHistory>();
            oh.OwningUser = _defaultOwningUser;
            oh.OwningApplication = _defaultOwningApplication;
            oh.ChangeAction = IfcChangeActionEnum.ADDED;
            Transaction.AddPropertyChange<IfcOwnerHistory>(m => _ownerHistoryAddObject = m, _ownerHistoryAddObject, oh);
            _defaultOwningUser = owninguser;
            _defaultOwningApplication = app;
            _ownerHistoryAddObject = oh;
            IfcOwnerHistory ohc = New<IfcOwnerHistory>();
            ohc.OwningUser = _defaultOwningUser;
            ohc.OwningApplication = _defaultOwningApplication;
            ohc.ChangeAction = IfcChangeActionEnum.MODIFIED;
            Transaction.AddPropertyChange<IfcOwnerHistory>(m => _ownerHistoryModifyObject = m, _ownerHistoryModifyObject, ohc);
            _defaultOwningUser = owninguser;
            _defaultOwningApplication = app;
            _ownerHistoryModifyObject = ohc;
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
        
        public Transaction BeginTransaction()
        {
            return this.BeginTransaction(null);
        }

        public Transaction BeginTransaction(string operationName)
        {
            Transaction txn = BeginEdit(operationName);
            //Debug.Assert(ToWrite.Count == 0);
            if (txn == null) txn = undoRedoSession.Begin(operationName);
            //txn.Finalised += TransactionFinalised;
            //txn.Reversed += TransactionReversed;
            return txn;
        }
 
        public IfcOwnerHistory OwnerHistoryModifyObject
        {
            get
            {
                return _ownerHistoryModifyObject;
            }
        }

        public IfcOwnerHistory OwnerHistoryAddObject
        {
            get
            {
                return _ownerHistoryAddObject;
            }
        }

        public IfcOwnerHistory OwnerHistoryDeleteObject
        {
            get
            {
                if (_ownerHistoryDeleteObject == null)
                {
                    _ownerHistoryDeleteObject = this.New<IfcOwnerHistory>();
                    _ownerHistoryDeleteObject.OwningUser = _defaultOwningUser;
                    _ownerHistoryDeleteObject.OwningApplication = _defaultOwningApplication;
                    _ownerHistoryDeleteObject.ChangeAction = IfcChangeActionEnum.DELETED;
                }
                return _ownerHistoryDeleteObject;
            }
        }

        

        public IfcApplication DefaultOwningApplication
        {
            get { return _defaultOwningApplication; }
        }

        public IfcPersonAndOrganization DefaultOwningUser
        {
            get { return _defaultOwningUser; }
        }
        #endregion

        #region IModel interface implementation
        /// <summary>
        ///   Returns all instances in the model of IfcType, IfcType may be an abstract Type
        /// </summary>
        /// <typeparam name = "TIfcType"></typeparam>
        /// <returns></returns>
        public IEnumerable<TIfcType> InstancesOfType<TIfcType>(bool activate = false) where TIfcType : IPersistIfcEntity
        {
            return Cached.OfType<TIfcType>(activate);
        }

        /// <summary>
        ///   Filters the Ifc Instances based on their Type and the predicate
        /// </summary>
        /// <typeparam name = "TIfcType">Ifc Type to filter</typeparam>
        /// <param name = "expression">function to execute</param>
        /// <returns></returns>
        public IEnumerable<TIfcType> InstancesWhere<TIfcType>(Expression<Func<TIfcType, bool>> expression) where TIfcType : IPersistIfcEntity
        {
            return Cached.Where(expression);
        }

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
        /// Returns true if the instance is in the current model
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public virtual bool ContainsInstance(IPersistIfcEntity instance)
        {
            return Cached.Contains(instance);
        }

        /// <summary>
        /// Returns true if the instance label is in the current model, 
        /// Use with care, does not check that the instance is in the current model, only the label exists
        /// </summary>
        /// <param name="entityLabel"></param>
        /// <returns></returns>
        public virtual bool ContainsEntityLabel(int entityLabel)
        {
            return Cached.Contains(entityLabel);
        }

        /// <summary>
        /// Returns an instance from the Model with the corresponding label
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public virtual IPersistIfcEntity GetInstance(int label)
        {
            return Cached.GetInstance(label);
        }

        /// <summary>
        /// Returns an instance from the Model with the corresponding label but does not keep a cache of it
        /// This is a dangerous call as duplicate instances of the sam eobject could happen
        /// Ony use when interating over the whole database dor export etc
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
        /// Returns the total number of Ifc Instances in the model
        /// </summary>
        public virtual long InstancesCount
        {
            get
            {
                return Cached.Count;
            }
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
        ///   Creates a new Ifc Persistent Instance, this is an undoable operation
        /// </summary>
        /// <typeparam name = "TIfcType"> The Ifc Type, this cannot be an abstract class. An exception will be thrown if the type is not a valid Ifc Type  </typeparam>
        public TIfcType New<TIfcType>() where TIfcType : IPersistIfcEntity, new()
        {
            Type t = typeof(TIfcType);
            int nextLabel = Cached.HighestLabel + 1;
            return (TIfcType)New(t, nextLabel);
        }
        /// <summary>
        ///   Creates and Instance of TIfcType and initializes the properties in accordance with the lambda expression
        ///   i.e. Person person = CreateInstance&gt;Person&lt;(p =&lt; { p.FamilyName = "Undefined"; p.GivenName = "Joe"; });
        /// </summary>
        /// <typeparam name = "TIfcType"></typeparam>
        /// <param name = "initPropertiesFunc"></param>
        /// <returns></returns>
        public TIfcType New<TIfcType>(InitProperties<TIfcType> initPropertiesFunc) where TIfcType : IPersistIfcEntity, new()
        {
            TIfcType instance = New<TIfcType>();
            initPropertiesFunc(instance);
            return instance;
        }

        /// <summary>
        /// Creates a new Model and populates with instances from the specified file, Ifc, IfcXML, IfcZip and Xbim are all supported.
        ///  All instances and changes are stored in memory, unless the storage file is Xbim, then a model server database is created
        ///  Typically use Xbim for medium to large files (Ifc files over 10MB) or where changes will be made
        /// </summary>
        /// <param name="importFrom">Name of the file containing the instances to import</param>
        /// /// <param name="xbimDbName">Name of the xbim file that will be created. 
        /// If null the contents are loaded into memory and are not persistent
        /// </param>
        /// <returns></returns>
        public bool CreateFrom(string importFrom, string xbimDbName = null, ReportProgressDelegate progDelegate = null)
        {
            if (Cached != null)
                Cached.Dispose(); //if the model is already open close it and release all resources
            Cached = new IfcPersistedInstanceCache(this);
            Init(xbimDbName);
            if (!Cached.CreateDatabase(xbimDbName)) //create the empty database
                return false;

            XbimStorageType toImportStorageType = StorageType(importFrom);
            switch (toImportStorageType)
            {
                case XbimStorageType.IFCXML:
                    Cached.ImportIfcXml(importFrom, progDelegate);
                    break;
                case XbimStorageType.IFC:
                    Cached.ImportIfc(importFrom, progDelegate);
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
        /// Creates a new empty model and sets the name of the file for changes to be saved to. 
        /// This file will not be created unless Save is called and the filename has been specified
        /// If the filename is nulll use SaveAs to save the file
        /// All instances and changes are stored in memory
        /// </summary>
        /// <param name="fileName">Name of the file that changes will be saved to, Ifc, IfcXML, IfcZip and Xbim.</param>
        public bool Create(string fileName)
        { 
            //we have nothing to store in so use an in memory cache
            if (Cached != null)
                Cached.Dispose(); //if the model is already open close it and release all resources
            
            Init(fileName);
            throw new NotImplementedException("Need to create this function");
            
        }

        /// <summary>
        /// Compacts an Xbim file to reduce size, database must be closed before this is called
        /// </summary>
        /// <returns></returns>
        public static  bool Compact(string sourceFileName, string targetFileName, out string errMsg)
        {
            return IfcPersistedInstanceCache.Compact(sourceFileName, targetFileName, out errMsg);
        }
        /// <summary>
        /// Initialises the model state, all instances, cached, written or to delete are dropped, the model is clean and empty.
        /// This operation is not reversable. If persisted is true an output file
        /// </summary>
        private void Init(string fileName)
        {

            undoRedoSession = null;
            header = null;
            _storageType = StorageType(fileName);
            if (_storageType == XbimStorageType.INVALID)
                _databaseName = null;
            else
                _databaseName = fileName;
            
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

       
        
        /// <summary>
        /// Creates and returns a new instance of Type t, sets the label to the specificed value.
        /// This is a reversabel operation
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        internal IPersistIfcEntity New(Type t, int label)
        {
            int nextLabel = Math.Abs(label);

            IPersistIfcEntity entity = Cached.CreateNew_Reversable(t, nextLabel);
            //Cached.SetHighestLabel_Reversable(nextLabel);
            //long highestLabel = Cached.HighestLabel;
            //Xbim.XbimExtensions.Transactions.Transaction.AddPropertyChange<long>(h => _highestLabel = h, highestLabel, nextLabel);
            //_highestLabel = Math.Max(nextLabel, _highestLabel);
            //entity.Bind(this, -nextLabel); //a negative handle determines that the attributes of this entity have not been loaded yet
            //Cached.Add_Reversible(nextLabel, entity);
            
            if (typeof(IfcRoot).IsAssignableFrom(t))
                ((IfcRoot)entity).OwnerHistory = OwnerHistoryAddObject;
            return entity;

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
            double total = InstancesCount;
            int idx = 0;
            int errors = 0;
            int percentage = -1;

            foreach (XbimInstanceHandle handle in InstanceHandles)
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
            Cached.Close();
            undoRedoSession = null;
            header = null;
            _storageType = XbimStorageType.INVALID;
            _databaseName = null;
        }

        /// <summary>
        /// Commits any changes to the models database and clears the object cache
        /// </summary>
        /// <returns></returns>
        public bool Commit()
        {
            try
            {
                Cached.Commit();
                return true;
            }
            catch (Exception) //handle errros maybe?
            {
                return false;
            } 
           
        }

        /// <summary>
        /// Returns true if the model can be saved to the specified filename
        /// </summary>
        public bool CanSave
        {
            get
            {
                
                if(string.IsNullOrEmpty(_databaseName)) return false; //no file name specified
                string fullPath = Path.GetFullPath(_databaseName);
                string backupName = Path.ChangeExtension(fullPath, Path.GetExtension(fullPath) + "Bak");

                try
                {
             
                switch (_storageType)
                {
                    case XbimStorageType.INVALID:
                        return false;
                    case XbimStorageType.IFCXML:
                    case XbimStorageType.IFC:
                    case XbimStorageType.IFCZIP: // we are going to create a new file regardless but take a backup
                        File.Delete(backupName);
                        if (File.Exists(backupName)) return false;
                        if (File.Exists(fullPath)) File.Copy(fullPath, backupName, true);
                        File.Create(fullPath);
                        return true;
                    case XbimStorageType.XBIM: //assume we can always update or create xbim files, no back up is provided
                        return true;
                    default:
                        return false;
                }
                }
                catch (Exception e)
                {
                    Logger.WarnFormat("Could not save file {0}\n{1}", fullPath, e.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns true if there are no changes to save in the model.
        /// </summary>
        public bool Saved
        {
            get
            {
                return Cached.Saved;
            }
        }

        #endregion


       


        public bool Open(string fileName, ReportProgressDelegate progDelegate = null)
        {
            try
            {
                if (Cached != null)
                    Cached.Dispose(); //if the model is already open close it and release all resources
                Cached = new IfcPersistedInstanceCache(this);
                XbimStorageType storageType = StorageType(fileName);
                
                XbimStorageType toImportStorageType = StorageType(fileName);
                switch (toImportStorageType)
                {
                    case XbimStorageType.IFCXML:
                        Cached.ImportIfcXml(fileName);
                        break;
                    case XbimStorageType.IFC:
                        Cached.ImportIfc(fileName);
                        break;
                    case XbimStorageType.IFCZIP:
                        Cached.ImportIfcZip(fileName);
                        break;
                    case XbimStorageType.XBIM:
                        Init(fileName);
                        Cached.Open(fileName); //opens the database
                        break;
                    case XbimStorageType.INVALID:
                    default:
                        return false;
                }
                _databaseName = fileName;
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
                _databaseName = outputFileName;
                _storageType = storageType;
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

        public IEnumerable<XbimInstanceHandle> InstanceHandles
        {
            get
            {
                return Cached.InstanceHandles;
            }
        }

        internal IEnumerable<XbimInstanceHandle> InstanceHandlesOfType<T>()
        {
            return Cached.InstanceHandlesOfType<T>();
        }

        // Extract first ifc file from zipped file and save in the same directory
        public string ExportZippedIfc(string inputIfcFile)
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
                  Cached.Dispose();
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



        internal XbimEntityTable GetEntityTable()
        {
            return Cached.GetEntityTable();
        }

        internal void Compact(XbimModel targetModel)
        {
            foreach (var prod in InstancesOfType<IfcProduct>())
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
