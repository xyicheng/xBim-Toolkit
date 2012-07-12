#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    Model.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Windows.Markup;
using Xbim.Ifc.ActorResource;
using Xbim.Ifc.DateTimeResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.SharedBldgServiceElements;
using Xbim.Ifc.UtilityResource;
using Xbim.XbimExtensions.DataProviders;
using Xbim.XbimExtensions.Transactions;
using Xbim.XbimExtensions.Transactions.Extensions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters;
using System.Xml;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip;
using Xbim.XbimExtensions;
using ICSharpCode.SharpZipLib.Core;
using Xbim.Common.Logging;
using Xbim.XbimExtensions.Interfaces;
using Xbim.IO.Parser;


#endregion

namespace Xbim.IO

{

    [Serializable]
    public class XbimMemoryModel : IModel, ISupportChangeNotification, INotifyPropertyChanged, INotifyPropertyChanging
    {
        #region Fields
		private readonly ILogger Logger = LoggerFactory.GetLogger();
        private readonly IfcFileHeader _header = new IfcFileHeader();
        private IfcInstances _ifcInstances = new IfcInstances();

        /// <summary>
        ///   Default true, builds indices to improve access performance for retrieving instances, set to false to increase laod speed where access speed is unimportant
        /// </summary>
        public bool BuildIndices = true;

        private IfcFilterDictionary _parseFilter;

        #endregion

        #region Static methods

        #endregion

        #region Public Methods

        IEnumerable<IPersistIfcEntity> IModel.Instances
        {
            get { return _ifcInstances; }
        }

        public bool Delete(IPersistIfcEntity instance)
        {
            return _ifcInstances.Remove(instance);
        }

        public void Commit()
        {
            Transaction txn = Transaction.Current;
            if (txn != null) txn.Commit();
        }

        public void Rollback()
        {
            Transaction txn = Transaction.Current;
            if (txn != null) txn.Rollback();
        }


        public ICollection<IPersistIfcEntity> Instances
        {
            get { return _ifcInstances; }
        }

        public void SetInstances(IfcInstances instances)
        {
            _ifcInstances = instances;
        }

        /// <summary>
        ///   Returns true if the instance is contained in this model
        /// </summary>
        /// <param name = "instance">instance to locate</param>
        /// <returns></returns>
        public bool ContainsInstance(IPersistIfcEntity instance)
        {
            return _ifcInstances.Contains(instance);
        }

        public bool ContainsInstance(long entityLabel)
        {
            return _ifcInstances.ContainsInstance(entityLabel);
        }

        /// <summary>
        ///   Creates an Ifc Persistent Instance from an entity name string, this is NOT an undoable operation
        /// </summary>
        /// <param name = "ifcEntityName">Ifc Entity Name i.e. IFCDOOR, IFCWALL, IFCWINDOW etc. Name must be in uppercase</param>
        /// <returns></returns>
        internal IPersistIfc CreateInstance(string ifcEntityName, long? label)
        {
            try
            {
                IfcType ifcType = IfcInstances.IfcTypeLookup[ifcEntityName];
                return CreateInstance(ifcType, label);
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format("Error creating entity {0}, it is not a supported Xbim type, {1}", ifcEntityName, e.Message));
            }

        }

        internal IPersistIfc CreateInstance(IfcType ifcType, long? label)
        {
            try
            {
                IPersistIfc instance = (IPersistIfc)Activator.CreateInstance(ifcType.Type);
                IPersistIfcEntity persist = instance as IPersistIfcEntity;
                if (persist != null)
                {
                    Debug.Assert(label.HasValue);
                    _highestLabel = Math.Max(_highestLabel, label.Value);
                    persist.Bind(this, label.Value);
                    _ifcInstances.AddRaw(persist);
                }

                return instance;
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format("{0} is not a supported Xbim Type", ifcType.Type.Name),
                                            "ifcEntityName)", e);
            }
        }

        public IPersistIfcEntity AddNew(IfcType ifcType, long label)
        {
            Debug.Assert(typeof(IPersistIfcEntity).IsAssignableFrom(ifcType.Type), "Type mismacth: IPersistIfcEntity");
            return (IPersistIfcEntity)CreateInstance(ifcType, label);
        }

        /// <summary>
        ///   Creates an Ifc Persistent Instance, this is an undoable operation
        /// </summary>
        /// <typeparam name = "TIfcType"> The Ifc Type, this cannot be an abstract class. An exception will be thrown if the type is not a valid Ifc Type  </typeparam>
        public TIfcType New<TIfcType>() where TIfcType : IPersistIfcEntity, new()
        {
            TIfcType instance = new TIfcType();
            if (IfcInstances.IfcEntities.Contains(instance.GetType()))
            {
                instance.Bind(this, NextLabel());
                _ifcInstances.Add_Reversible(instance);
                IfcRoot rt = instance as IfcRoot;
                if (rt != null) rt.OwnerHistory = this.OwnerHistoryAddObject;

                return instance;
            }
            else
                throw new ArgumentException(string.Format("{0} is not a supported Ifc Type", instance.GetType().Name),
                                            "Model.New<CType>()");
        }

        private long _highestLabel;

        private long NextLabel()
        {
            _highestLabel++;

            return _highestLabel;
        }

        /// <summary>
        ///   Creates and Instance of TIfcType and initializes the properties in accordance with the lambda expression
        ///   i.e. Person person = CreateInstance&gt;Person&lt;(p =&lt; { p.FamilyName = "Undefined"; p.GivenName = "Joe"; });
        /// </summary>
        /// <typeparam name = "TIfcType"></typeparam>
        /// <param name = "initPropertiesFunc"></param>
        /// <returns></returns>
        public TIfcType New<TIfcType>(InitProperties<TIfcType> initPropertiesFunc)
            where TIfcType : IPersistIfcEntity, new()
        {
            TIfcType instance = new TIfcType();

            if (IfcInstances.IfcEntities.Contains(instance.GetType()))
            {
                instance.Bind(this, NextLabel());
                _ifcInstances.Add_Reversible(instance);
                IfcRoot rt = instance as IfcRoot;
                if (rt != null) rt.OwnerHistory = this.OwnerHistoryAddObject;
                initPropertiesFunc(instance);
                return instance;
            }
            else
                throw new ArgumentException(string.Format("{0} is not a supported Ifc Type", instance.GetType().Name),
                                            "Model.New<CType>()");
        }

        /// <summary>
        ///   Removes all instances in the model, this is an undoable operation
        /// </summary>
        public void ClearAllInstances()
        {
            _ifcInstances.Clear_Reversible();
        }


        /// <summary>
        ///   Removes all instances that are not referenced by any other instances in the model unless they are of a type specified in the forceRetentionOf list, this is an undoable operation
        /// </summary>
        /// <param name = "forceRetentionOf">Enumeration of Types whose instances will always be retained</param>
        public void PurgeInstances(IEnumerable<Type> forceRetentionOf)
        {
            IfcInstances retainedInstances = new IfcInstances();

            foreach (Type type in forceRetentionOf)
            {
                _ifcInstances.CopyTo(retainedInstances, type);
            }
            IfcInstances refInstances = new IfcInstances();
            foreach (IPersistIfcEntity instance in retainedInstances)
            {
                CopyReferences(refInstances, instance);
            }
            foreach (IPersistIfcEntity item in retainedInstances)
            {
                refInstances.Add(item);
            }
            _ifcInstances = refInstances;
            //  ModelManager.SetModelValue<IfcInstances>(this, ref _ifcInstances, newInstances, v => _ifcInstances = v);
        }

        /// <summary>
        ///   Removes all instances that are not referenced by any other instances in the model unless they are of a type Root,(Ifc Exchangeable instances), this is an undoable operation
        /// </summary>
        public void PurgeInstances()
        {
            PurgeInstances(new[] { typeof(IfcRoot) });
        }

        /// <summary>
        ///   Returns all instances in the model of IfcType, IfcType may be an abstract Type
        /// </summary>
        /// <typeparam name = "TIfcType"></typeparam>
        /// <returns></returns>
        public IEnumerable<TIfcType> InstancesOfType<TIfcType>() where TIfcType : IPersistIfcEntity
        {
            return _ifcInstances.OfType<TIfcType>();
        }

        /// <summary>
        ///   Filters the Ifc Instances based on their Type and the predicate
        /// </summary>
        /// <typeparam name = "TIfcType">Ifc Type to filter</typeparam>
        /// <param name = "expression">function to execute</param>
        /// <returns></returns>
        public IEnumerable<TIfcType> InstancesWhere<TIfcType>(Expression<Func<TIfcType, bool>> expression)
            where TIfcType : IPersistIfcEntity
        {
            return _ifcInstances.Where(expression);
        }

        //public IEnumerable<TIfcType> InstancesWhere<TIfcType>(Predicate<TIfcType> expr) where TIfcType : IPersistIfc
        //{
        //    return _ifcInstances.Where<TIfcType>(expr);
        //}
        /// <summary>
        ///   Returns IEnumerable of all instances that are not referenced through any hierarchy that contains instances of the types in forceRetentionOf
        /// </summary>
        /// <param name = "forceRetentionOf">Enumeration of Types whose instances will always be retained</param>
        public IEnumerable<IPersistIfcEntity> UnreferencedInstances(IEnumerable<Type> forceRetentionOf)
        {
            IfcInstances newInstances = new IfcInstances();
            foreach (Type type in forceRetentionOf)
            {
                _ifcInstances.CopyTo(newInstances, type);
            }
            foreach (IPersistIfcEntity instance in newInstances)
            {
                CopyReferences(newInstances, instance);
            }
            return _ifcInstances.Except(newInstances);
        }

        /// <summary>
        ///   Returns IEnumerable of all instances that are not referenced through any hierarchy that contains all root instances (Ifc Exchangeable Instances)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPersistIfcEntity> UnreferencedInstances()
        {
            return UnreferencedInstances(new[] { typeof(IfcRoot) });
        }

        #endregion

        #region Private Methods

        private void CopyReferences(IfcInstances newInstances, IPersistIfcEntity instance)
        {
            IfcType ifcType = IfcInstances.IfcEntities[instance.GetType()];

            foreach (IfcMetaProperty prop in ifcType.IfcProperties.Values)
            {
                object value = prop.PropertyInfo.GetValue(instance, null);

                if (value != null)
                {
                    IEnumerable<IPersistIfcEntity> collection = value as IEnumerable<IPersistIfcEntity>;
                    if (collection != null)
                    {
                        foreach (IPersistIfcEntity reference in collection)
                        {
                            CopyReferences(newInstances, reference);
                        }
                    }
                    else
                    {
                        ExpressType expressType = value as ExpressType;
                        IPersistIfcEntity persistType = value as IPersistIfcEntity;
                        if (expressType == null && persistType != null && !newInstances.Contains(persistType))
                        {
                            newInstances.Add(persistType);
                            CopyReferences(newInstances, persistType);
                        }
                    }
                }
            }
        }

        #endregion

        #region Serialized Members

        public IfcProject IfcProject
        {
            get { return _ifcInstances.OfType<IfcProject>().FirstOrDefault(); }
        }

        /// <summary>
        ///   Unique identifier for the model
        /// </summary>
        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        #endregion

        #region NonSerialized Members

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


        [NonSerialized]
        private UndoRedoSession _undoRedoSession;

        [NonSerialized]
        private P21toModelParser _part21Parser;

        [NonSerialized]
        private IfcCoordinatedUniversalTimeOffset _coordinatedUniversalTimeOffset;

        #endregion

        #region Constructors and Initialisation

        /// <summary>
        ///   creates an in Memory xbim model and initialises all defaults
        /// </summary>
        public XbimMemoryModel(bool useUndoRedo)
        {
            if (useUndoRedo)
                _undoRedoSession = new UndoRedoSession();
        }
        /// <summary>
        /// Creates an in memory Model with no UndoRedoSesion
        /// </summary>
        public XbimMemoryModel()
            : this(false)
        {

        }
        #endregion

        #region Properties

        public IfcObjects Objects
        {
            get { return new IfcObjects(this); }
        }

        public IfcProducts IfcProducts
        {
            get { return new IfcProducts(this); }
        }

        public IfcTypeObjects TypeObjects
        {
            get { return new IfcTypeObjects(this); }
        }

        public IfcTypeProducts TypeProducts
        {
            get { return new IfcTypeProducts(this); }
        }

        public IfcElements Elements
        {
            get { return new IfcElements(this); }
        }

        public IfcBuildingElements BuildingElements
        {
            get { return new IfcBuildingElements(this); }
        }


        public IEnumerable<IfcProduct> Structure
        {
            get
            {
                return
                    _ifcInstances.OfType<IfcBeam>().Cast<IfcProduct>().Concat<IfcProduct>(
                        _ifcInstances.OfType<IfcColumn>().Cast<IfcProduct>());
            }
        }

        public IEnumerable<IfcProduct> Services
        {
            get { return _ifcInstances.OfType<IfcDistributionFlowElement>().Cast<IfcProduct>(); }
        }


        public IfcCoordinatedUniversalTimeOffset CoordinatedUniversalTimeOffset
        {
            get
            {
                if (_coordinatedUniversalTimeOffset == null)
                {
                    _coordinatedUniversalTimeOffset = New<IfcCoordinatedUniversalTimeOffset>();
                    DateTimeOffset localTime = DateTimeOffset.Now;
                    _coordinatedUniversalTimeOffset.HourOffset = new IfcHourInDay(localTime.Offset.Hours);
                    _coordinatedUniversalTimeOffset.MinuteOffset = new IfcMinuteInHour(localTime.Offset.Minutes);
                    if (localTime.Offset.Hours < 0 || (localTime.Offset.Hours == 0 && localTime.Offset.Minutes < 0))
                        _coordinatedUniversalTimeOffset.Sense = IfcAheadOrBehind.BEHIND;
                    else
                        _coordinatedUniversalTimeOffset.Sense = IfcAheadOrBehind.AHEAD;
                }
                return _coordinatedUniversalTimeOffset;
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

        public IfcOwnerHistory OwnerHistoryModifyObject
        {
            get
            {
                return _ownerHistoryModifyObject;
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


        /// <summary>
        ///   Gets the reversible transaction holding all changes made in this document and that can be
        ///   used to undo and redo operations.
        /// </summary>
        public UndoRedoSession UndoRedoSession
        {
            get { return _undoRedoSession; }
        }


        public IEnumerable<IfcApplication> Applications
        {
            get { return _ifcInstances.OfType<IfcApplication>(); }
        }

        #endregion

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

        private IPersistIfc _part21Parser_EntityCreateWithFilter(string className, long? label, bool headerEntity,
                                                                 out int[] reqParams)
        {
            if (headerEntity)
            {
                reqParams = null;
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
            {
                try
                {
                    IfcType ifcInstancesIfcTypeLookup = IfcInstances.IfcTypeLookup[className];
                    reqParams = null;
                    if (_parseFilter.Contains(ifcInstancesIfcTypeLookup))
                    {
                        IfcFilter filter = _parseFilter[ifcInstancesIfcTypeLookup];
                        if (filter.PropertyIndices != null && filter.PropertyIndices.Length > 0)
                            reqParams = _parseFilter[ifcInstancesIfcTypeLookup].PropertyIndices;
                        return CreateInstance(ifcInstancesIfcTypeLookup, label);
                    }
                    else if (ifcInstancesIfcTypeLookup.Type.IsValueType)
                    {
                        return CreateInstance(ifcInstancesIfcTypeLookup, label);
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("Parse Error, Entity {0} could not be created", className), e);
                }
            }
        }

        public long InstancesCount
        {
            get { return _ifcInstances.Count; }
        }


        /// <summary>
        ///   Parses the part 21 file and returns trhe number of erors found, errorLog contains error details
        /// </summary>
        /// <param name = "inputStream"></param>
        /// <param name = "filter"></param>
        /// <param name = "errorLog"></param>
        /// <param name = "progressHandler"></param>
        /// <returns></returns>
        public int ParsePart21(Stream inputStream, FilterViewDefinition filter, TextWriter errorLog,
                               ReportProgressDelegate progressHandler)
        {

            int errorCount = 0;

            _part21Parser = new P21toModelParser(inputStream, errorLog);
            if (filter != null)
                _parseFilter = filter.GetFilter();
            else
                _parseFilter = null;
            CreateEntityEventHandler creator;
            if (_parseFilter == null)
                creator = _part21Parser_EntityCreate;
            else
                creator = _part21Parser_EntityCreateWithFilter;
            _part21Parser.EntityCreate += creator;
            if (progressHandler != null) _part21Parser.ProgressStatus += progressHandler;

            IndentedTextWriter tw = new IndentedTextWriter(errorLog, "    ");
            try
            {

                _part21Parser.Parse();
            }
            catch (Exception ex)
            {
                errorLog.WriteLine("General Parser error.");
                int indent = tw.Indent;
                while (ex != null)
                {
                    tw.Indent++;
                    errorLog.WriteLine(ex.Message);
                    ex = ex.InnerException;
                }
                tw.Indent = indent;
                errorCount++;
            }
            finally
            {
                _part21Parser.EntityCreate -= creator;
                if (progressHandler != null) _part21Parser.ProgressStatus -= progressHandler;

            }
            errorCount = _part21Parser.ErrorCount + errorCount;
            if (errorCount == 0 && BuildIndices)
                errorCount += _ifcInstances.BuildIndices(errorLog);
            return errorCount;
        }


        private void _part21Parser_ProgressStatus(string operation, int percentage)
        {
            Debug.WriteLine(operation + " " + percentage + "% complete");
        }

        #region Session Methods

        public Transaction BeginTransaction()
        {
            return this.BeginTransaction(null);
        }

        public Transaction BeginTransaction(string operationName)
        {
            Transaction txn = null;
            if (_undoRedoSession != null)
            {
                txn = _undoRedoSession.Begin(operationName);
            }
            else
            {
                // create new _undoRedoSession
                _undoRedoSession = new UndoRedoSession();
                txn = Transaction.Begin(operationName);
                InitialiseDefaultOwnership();
            }

            return txn;
        }

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

        #endregion

        #region Ifc Schema Validation Methods

        public string WhereRule()
        {
            if (IfcProject == null)
                return "WR1 Model: A Model must have a valid Project attribute";
            return "";
        }

        #endregion

        #region ISupportChangeNotification Members

        void ISupportChangeNotification.NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        [field: NonSerialized] //don't serialize events
        private event PropertyChangedEventHandler PropertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }

        void ISupportChangeNotification.NotifyPropertyChanging(string propertyName)
        {
            PropertyChangingEventHandler handler = PropertyChanging;
            if (handler != null)
            {
                handler(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        [field: NonSerialized] //don't serialize events
        private event PropertyChangingEventHandler PropertyChanging;

        event PropertyChangingEventHandler INotifyPropertyChanging.PropertyChanging
        {
            add { PropertyChanging += value; }
            remove { PropertyChanging -= value; }
        }

        #endregion

        #region Validation

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
            double total = _ifcInstances.Count;
            int idx = 0;
            int errors = 0;
            int percentage = -1;

            foreach (IPersistIfcEntity ent in _ifcInstances)
            {
                idx++;
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
            IfcType ifcType = IfcInstances.IfcEntities[ent];
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

        #region IModel Members

        public IEnumerable<IfcRoot> RootInstances
        {
            get { return _ifcInstances.OfType<IfcRoot>(); }
        }

#if SupportActivation

        public long Activate(IPersistIfcEntity entity, bool write)
        {
            IfcRoot root = entity as IfcRoot;
            //if (root != null && root.OwnerHistory != _ownerHistoryModifyObject) root.OwnerHistory = _ownerHistoryModifyObject;
            return entity.EntityLabel;
        }

        public IPersistIfcEntity GetInstance(long label)
        {
            return _ifcInstances.GetInstance(label);
        }


#endif

        #endregion

        #region IModel Members

        public IfcFileHeader Header
        {
            get { return _header; }
        }

        #endregion


        public bool ReOpen()
        {
            //nothing to do
            return true;
        }

        public void Close()
        {
            //nothing to do
        }




        public IEnumerable<Tuple<string, long>> ModelStatistics()
        {
            List<Tuple<string, long>> results = new List<Tuple<string, long>>();
            IfcType ifcType = IfcInstances.IfcEntities[typeof(IfcBuildingElement)];
            foreach (Type elemType in ifcType.NonAbstractSubTypes)
            {
                if (this._ifcInstances.ContainsKey(elemType))
                {
                    long cnt = this._ifcInstances[elemType].Count;
                    results.Add(Tuple.Create(elemType.Name, cnt));
                }
            }
            return results;
        }



        public string Validate(ValidationFlags validateFlags)
        {
            StringBuilder sb = new StringBuilder();
            TextWriter tw = new StringWriter(sb);
            Validate(tw, null, validateFlags);
            return sb.ToString();
        }

        // TODO: Review why these properties are here on the model.
        public IEnumerable<IfcWall> Walls
        {
            get { return InstancesOfType<IfcWall>(); }
        }

        public IEnumerable<IfcSlab> Slabs
        {
            get { return InstancesOfType<IfcSlab>(); }
        }

        public IEnumerable<IfcDoor> Doors
        {
            get { return InstancesOfType<IfcDoor>(); }
        }

        public IEnumerable<IfcRoof> Roofs
        {
            get { return InstancesOfType<IfcRoof>(); }
        }


        public UndoRedoSession UndoRedo
        {
            get { return _undoRedoSession; }
        }

        private void ExportIfc(string fileName, bool compress, bool isGZip = true)
        {
            StreamWriter ifcFile = null;
            FileStream fs = null;
            try
            {
                if (compress)
                {
                    if (isGZip)
                    {
                        fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                        GZipStream zip = new GZipStream(fs, CompressionMode.Compress);
                        ifcFile = new StreamWriter(zip);
                    }
                    else // if isGZip == false then use sharpziplib
                    {
                        string ext = "";
                        if (fileName.ToLower().EndsWith(".zip") == false || fileName.ToLower().EndsWith(".ifczip") == false) ext = ".ifczip";
                        fs = new FileStream(fileName + ext, FileMode.Create, FileAccess.Write);
                        ZipOutputStream zipStream = new ZipOutputStream(fs);
                        zipStream.SetLevel(3); //0-9, 9 being the highest level of compression
                        ZipEntry newEntry = new ZipEntry(fileName);
                        newEntry.DateTime = DateTime.Now;
                        zipStream.PutNextEntry(newEntry);

                        ifcFile = new StreamWriter(zipStream);
                    }
                }
                else
                {
                    ifcFile = new StreamWriter(fileName);
                }
                

                Part21FileWriter p21 = new Part21FileWriter(ifcFile);
                p21.WriteHeader(this);
                foreach (IPersistIfcEntity item in this.Instances)
                {
                    p21.Write(item);
                }
                p21.WriteFooter();
                p21.Close();
                
                
                ifcFile.Flush();
            }
            catch (Exception e)
            {
                throw new Exception("Error creating Ifc File = " + fileName, e);
            }
            finally
            {
                if (ifcFile != null) ifcFile.Close();
                if (fs != null) fs.Close();
            }
        }

        public void Export(XbimStorageType fileType, string outputFileName)
        {
            if (fileType.HasFlag(XbimStorageType.XBIM))
            {
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.AssemblyFormat = FormatterAssemblyStyle.Simple;
                    this.Header.FileDescription.EntityCount = this.Instances.Count();
                    Stream stream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None);
                    formatter.Serialize(stream, this);
                    formatter.Serialize(stream, this);
                    stream.Close();
                }
                catch (Exception ex)
                {
                    throw new Exception("Error exporting file: " + ex.Message);
                }
            }
            else if (fileType.HasFlag(XbimStorageType.IFC))
            {
                try
                {
                    ExportIfc(outputFileName, false);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error exporting file: " + ex.Message);
                }
            }
            else if (fileType.HasFlag(XbimStorageType.IFCXML))
            {
                FileStream xmlOutStream = null;
                try
                {
                    xmlOutStream = new FileStream(outputFileName, FileMode.Create, FileAccess.ReadWrite);
                    XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
                    using (XmlWriter xmlWriter = XmlWriter.Create(xmlOutStream, settings))
                    {
                        IfcXmlWriter writer = new IfcXmlWriter();
                        writer.Write(this, xmlWriter, null);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to write IfcXml file " + outputFileName, e);
                }
                finally
                {
                    if (xmlOutStream != null) xmlOutStream.Close();
                }
            }
            else if (fileType.HasFlag(XbimStorageType.IFCZIP))
            {
                try
                {
                    ExportIfc(outputFileName, true, true);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error exporting file: " + ex.Message);
                }
            }
        }

        public string Open(string inputFileName)
        {
            string outputFileName = Path.ChangeExtension(inputFileName, "xbim");

            XbimStorageType fileType = XbimStorageType.XBIM;
            string ext = Path.GetExtension(inputFileName).ToLower();
            if (ext == ".xbim") fileType = XbimStorageType.XBIM;
            else if (ext == ".ifc") fileType = XbimStorageType.IFC;
            else if (ext == ".ifcxml") fileType = XbimStorageType.IFCXML;
            else if (ext == ".zip" || ext == ".ifczip") fileType = XbimStorageType.IFCZIP;
            else
                throw new Exception("Invalid file type: " + ext);
            try
            {
                if (fileType.HasFlag(XbimStorageType.IFCZIP))
                {
                    // get the ifc file from zip
                    //using (Stream zipStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read))
                    //{
                    //    ZipInputStream zis = new ZipInputStream(zipStream);
                    //}
                    using (ZipInputStream zis = new ZipInputStream(File.OpenRead(inputFileName)))
                    {
                        ZipEntry zs = zis.GetNextEntry();
                        while (zs != null)
                        {
                            String fileName = Path.GetFileName(zs.Name);
                            if (fileName.ToLower().EndsWith(".ifc") || fileName.ToLower().EndsWith(".ifcxml"))
                            {
                                if (fileName.ToLower().EndsWith(".ifc"))
                                {
                                    ZipFile zf = new ZipFile(inputFileName);
                                    Stream entryStream = zf.GetInputStream(zs);
                                    using (IfcInputStream input = new IfcInputStream(entryStream))
                                    {
                                        if (input.Load(this) != 0)
                                            throw new Exception("Ifc file parsing errors\n" + input.ErrorLog.ToString());
                                    }                                    
                                    break;
                                }
                                else if (fileName.ToLower().EndsWith(".ifcxml"))
                                {
                                    ZipFile zf = new ZipFile(inputFileName);
                                    XmlReaderSettings settings = new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = false };
                                    Stream entryStream = zf.GetInputStream(zs);
                                    using (XmlReader xmlReader = XmlReader.Create(entryStream, settings))
                                    {
                                        IfcXmlReader reader = new IfcXmlReader();
                                        reader.Read(this, xmlReader);
                                    }
                                    break;
                                }                                                                
                            }
                        }

                    }
                }

                else if (fileType.HasFlag(XbimStorageType.IFCXML))
                {
                    // input to be xml file, output will be xbim file
                    XmlReaderSettings settings = new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = false };
                    Stream xmlInStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read);

                    using (XmlReader xmlReader = XmlReader.Create(xmlInStream, settings))
                    {
                        IfcXmlReader reader = new IfcXmlReader();
                        reader.Read(this, xmlReader);
                    }
                }
                else if (fileType.HasFlag(XbimStorageType.IFC))
                {
                    //attach it to the Ifc Stream Parser
                    using (IfcInputStream input = new IfcInputStream(
						new FileStream(inputFileName, FileMode.Open, FileAccess.Read)))
                    {
						if (input.Load(this) != 0)
						{
							Logger.WarnFormat("IFC file {0} failed to load.", inputFileName);
							throw new Exception("Ifc file parsing errors\n" + input.ErrorLog.ToString());
						}
                    }

                }

            }
            catch (Exception ex)
            {
                throw new Exception("Ifc file parsing errors\n" + inputFileName, ex);
            }

            return outputFileName;
        }


        public bool Save()
        {
            return true;
        }

        public bool SaveAs(string outputFileName)
        {
            return true;
        }

        public void Import(string inputFileName)
        {
            throw new NotImplementedException("Import functionality: not implemented yet");
        }



        #region IModel Members


        public string Open(string inputFileName, ReportProgressDelegate progDelegate)
        {
            return Open(inputFileName, progDelegate);
        }

        #endregion
    }
}
