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
using Xbim.Ifc2x3.ActorResource;
using Xbim.Ifc2x3.DateTimeResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SelectTypes;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.SharedBldgServiceElements;
using Xbim.Ifc2x3.UtilityResource;
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
    public class XbimMemoryModel : XbimModel, ISupportChangeNotification, INotifyPropertyChanged, INotifyPropertyChanging
    {
        #region Fields
		private readonly ILogger Logger = LoggerFactory.GetLogger();
        
        /// <summary>
        ///   Default true, builds indices to improve access performance for retrieving instances, set to false to increase laod speed where access speed is unimportant
        /// </summary>
        public bool BuildIndices = false;

        private IfcFilterDictionary _parseFilter;

        #endregion


        #region Public Methods

       

        public override bool Delete(IPersistIfcEntity instance)
        {
            return instances.Remove(instance);
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
                return CreateInstance(ifcType.Type, label);
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format("Error creating entity {0}, it is not a supported Xbim type, {1}", ifcEntityName, e.Message));
            }

        }

        internal IPersistIfc CreateInstance(Type ifcType, long? label)
        {
            try
            {
                IPersistIfc instance = (IPersistIfc)Activator.CreateInstance(ifcType);
                IPersistIfcEntity persist = instance as IPersistIfcEntity;
                if (persist != null)
                {
                    Debug.Assert(label.HasValue);
                    _highestLabel = Math.Max(_highestLabel, label.Value);
                    persist.Bind(this, label.Value);
                    instances.AddRaw(persist);
                }

                return instance;
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format("{0} is not a supported Xbim Type", ifcType.Name),
                                            "ifcEntityName)", e);
            }
        }

      

       

        /// <summary>
        ///   Removes all instances in the model, this is an undoable operation
        /// </summary>
        public void ClearAllInstances()
        {
            instances.Clear_Reversible();
        }


        /// <summary>
        ///   Removes all instances that are not referenced by any other instances in the model unless they are of a type specified in the forceRetentionOf list, this is an undoable operation
        /// </summary>
        /// <param name = "forceRetentionOf">Enumeration of Types whose instances will always be retained</param>
        public void PurgeInstances(IEnumerable<Type> forceRetentionOf)
        {
            IfcInstances retainedInstances = new IfcInstances(this);

            foreach (Type type in forceRetentionOf)
            {
                instances.CopyTo(retainedInstances, type);
            }
            IfcInstances refInstances = new IfcInstances(this);
            foreach (IPersistIfcEntity instance in retainedInstances)
            {
                CopyReferences(refInstances, instance);
            }
            foreach (IPersistIfcEntity item in retainedInstances)
            {
                refInstances.Add(item);
            }
            instances = refInstances;
            //  ModelManager.SetModelValue<IfcInstances>(this, ref instances, newInstances, v => instances = v);
        }

        /// <summary>
        ///   Removes all instances that are not referenced by any other instances in the model unless they are of a type Root,(Ifc Exchangeable instances), this is an undoable operation
        /// </summary>
        public void PurgeInstances()
        {
            PurgeInstances(new[] { typeof(IfcRoot) });
        }

       
        //public IEnumerable<TIfcType> InstancesWhere<TIfcType>(Predicate<TIfcType> expr) where TIfcType : IPersistIfc
        //{
        //    return instances.Where<TIfcType>(expr);
        //}
        /// <summary>
        ///   Returns IEnumerable of all instances that are not referenced through any hierarchy that contains instances of the types in forceRetentionOf
        /// </summary>
        /// <param name = "forceRetentionOf">Enumeration of Types whose instances will always be retained</param>
        public IEnumerable<long> UnreferencedInstances(IEnumerable<Type> forceRetentionOf)
        {
            IfcInstances newInstances = new IfcInstances(this);
            foreach (Type type in forceRetentionOf)
            {
                instances.CopyTo(newInstances, type);
            }
            foreach (IPersistIfcEntity instance in newInstances)
            {
                CopyReferences(newInstances, instance);
            }
            return instances.Except((IEnumerable<long>)newInstances);
        }

        /// <summary>
        ///   Returns IEnumerable of all instances that are not referenced through any hierarchy that contains all root instances (Ifc Exchangeable Instances)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<long> UnreferencedInstances()
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
        private XbimP21Parser _part21Parser;

        
        #endregion

        #region Constructors and Initialisation

        /// <summary>
        ///   creates an in Memory xbim model and initialises all defaults
        /// </summary>
        public XbimMemoryModel(bool useUndoRedo)
        {
            if (useUndoRedo)
                undoRedoSession = new UndoRedoSession();
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
                    instances.OfType<IfcBeam>().Cast<IfcProduct>().Concat<IfcProduct>(
                        instances.OfType<IfcColumn>().Cast<IfcProduct>());
            }
        }

        public IEnumerable<IfcProduct> Services
        {
            get { return instances.OfType<IfcDistributionFlowElement>().Cast<IfcProduct>(); }
        }


       

        /// <summary>
        ///   Gets the reversible transaction holding all changes made in this document and that can be
        ///   used to undo and redo operations.
        /// </summary>
        public UndoRedoSession UndoRedoSession
        {
            get { return undoRedoSession; }
        }


        public IEnumerable<IfcApplication> Applications
        {
            get { return instances.OfType<IfcApplication>(); }
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
                reqParams = null;
                try
                {
                    IfcType ifcInstancesIfcTypeLookup = IfcInstances.IfcTypeLookup[className];
                    
                    if (_parseFilter.Contains(ifcInstancesIfcTypeLookup))
                    {
                        IfcFilter filter = _parseFilter[ifcInstancesIfcTypeLookup];
                        if (filter.PropertyIndices != null && filter.PropertyIndices.Length > 0)
                            reqParams = _parseFilter[ifcInstancesIfcTypeLookup].PropertyIndices;
                        return CreateInstance(ifcInstancesIfcTypeLookup.Type, label);
                    }
                    else if (ifcInstancesIfcTypeLookup.Type.IsValueType)
                    {
                        return CreateInstance(ifcInstancesIfcTypeLookup.Type, label);
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception )
                {
                    Logger.ErrorFormat(string.Format("Parse Error, Entity {0} could not be created", className));
                    return null;
                }
            }
        }

       

        /// <summary>
        ///   Parses the part 21 file and returns trhe number of erors found, errorLog contains error details
        /// </summary>
        /// <param name = "inputStream"></param>
        /// <param name = "progressHandler"></param>
        /// <returns></returns>
        public override int ParsePart21(Stream inputStream, ReportProgressDelegate progressHandler)
        {

            int errorCount = 0;

            _part21Parser = new XbimP21Parser(inputStream);
            _parseFilter = null;
            CreateEntityEventHandler creator;
            if (_parseFilter == null)
                creator = _part21Parser_EntityCreate;
            else
                creator = _part21Parser_EntityCreateWithFilter;
            _part21Parser.EntityCreate += creator;
            if (progressHandler != null) _part21Parser.ProgressStatus += progressHandler;

           
            try
            {

                _part21Parser.Parse();
            }
            catch (Exception )
            {
                Logger.Error("Parser errors: The IFC file does not comply with the correct syntax");
                errorCount++;
            }
            finally
            {
                _part21Parser.EntityCreate -= creator;
                if (progressHandler != null) _part21Parser.ProgressStatus -= progressHandler;

            }
            errorCount = _part21Parser.ErrorCount + errorCount;
            if (errorCount == 0 && BuildIndices)
                errorCount += instances.BuildIndices();
            return errorCount;
        }


        private void _part21Parser_ProgressStatus(string operation, int percentage)
        {
            Debug.WriteLine(operation + " " + percentage + "% complete");
        }

        #region Session Methods

       

        

       

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

       

        #region IModel Members

        public IEnumerable<IfcRoot> RootInstances
        {
            get { return instances.OfType<IfcRoot>(); }
        }

#if SupportActivation



        public override IPersistIfcEntity GetInstance(long label)
        {
            long fileOffset;
            return instances.GetOrCreateEntity(this, label, out fileOffset);
        }


#endif

        #endregion

        #region IModel Members


        #endregion


        public override bool ReOpen()
        {
            //nothing to do
            return true;
        }

        public override void Close()
        {
            //nothing to do
        }




        public override IEnumerable<Tuple<string, long>> ModelStatistics()
        {
            List<Tuple<string, long>> results = new List<Tuple<string, long>>();
            IfcType ifcType = IfcInstances.IfcEntities[typeof(IfcBuildingElement)];
            foreach (Type elemType in ifcType.NonAbstractSubTypes)
            {
                if (this.instances.ContainsKey(elemType))
                {
                    long cnt = this.instances[elemType].Count;
                    results.Add(Tuple.Create(elemType.Name, cnt));
                }
            }
            return results;
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


       
        public override string Open(string inputFileName)
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



        public override void Import(string inputFileName)
        {
            throw new NotImplementedException("Import functionality: not implemented yet");
        }



        #region IModel Members


        public override string Open(string inputFileName, ReportProgressDelegate progDelegate)
        {
            return Open(inputFileName, progDelegate);
        }

        #endregion





        public override bool Save()
        {
            throw new NotImplementedException();
        }

        protected override void ActivateEntity(long offset, IPersistIfcEntity entity)
        {
            //do nothing it is already activated in a memory model
        }
    }
}
