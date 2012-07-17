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
using System.CodeDom.Compiler;
using Xbim.Ifc2x3.SelectTypes;
using System.Collections;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;
using System.IO.Compression;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.GeometryResource;
using System.Linq.Expressions;
using System.Diagnostics;
using Xbim.XbimExtensions.Transactions.Extensions;
namespace Xbim.IO
{
    abstract public class XbimModel : IModel
    {
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
        protected UndoRedoSession undoRedoSession;

        protected IIfcFileHeader header;
        protected IfcInstances instances;
        protected HashSet<IPersistIfcEntity> ToWrite = new HashSet<IPersistIfcEntity>();
       

        public static Type GetItemTypeFromGenericType(Type genericType)
        {
            if (genericType == typeof(ICoordinateList))
                return typeof(IfcLengthMeasure); //special case for coordinates
            if (genericType.IsGenericType || genericType.IsInterface)
            {
                Type[] genericTypes = genericType.GetGenericArguments();
                if (genericTypes.GetUpperBound(0) >= 0)
                    return genericTypes[genericTypes.GetUpperBound(0)];
                return null;
            }
            if (genericType.BaseType != null)
                return GetItemTypeFromGenericType(genericType.BaseType);
            return null;
        }

        protected abstract void ActivateEntity(long offset, IPersistIfcEntity entity);

        public long Activate(IPersistIfcEntity entity, bool write)
        {

            long label = entity.EntityLabel;
            //   Debug.Assert((!write && label < 0) || (write && label > 0)); //cannot call to write if we hven't read current state;
            long posLabel = Math.Abs(label);

            if (!write) //we want to activate for reading, if entry offset == 0 it is a new oject with no data set
            {
                long fileOffset = instances.GetFileOffset(label);
                ActivateEntity(fileOffset, entity);
            }
            else //it is activated for reading and we now want to write so remember until the transaction is committed
            {

                if (!Transaction.IsRollingBack)
                {
                    // Debug.Assert(Transaction.Current != null); //don't write things if not in a transaction
                    if (!ToWrite.Contains(entity))
                    {
                        ToWrite.Add_Reversible(entity);
                    }
                }
            }

            return posLabel;
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






        /// <summary>
        ///   Returns all instances in the model of IfcType, IfcType may be an abstract Type
        /// </summary>
        /// <typeparam name = "TIfcType"></typeparam>
        /// <returns></returns>
        public IEnumerable<TIfcType> InstancesOfType<TIfcType>() where TIfcType : IPersistIfcEntity
        {
            return instances.OfType<TIfcType>();
        }

        /// <summary>
        ///   Filters the Ifc Instances based on their Type and the predicate
        /// </summary>
        /// <typeparam name = "TIfcType">Ifc Type to filter</typeparam>
        /// <param name = "expression">function to execute</param>
        /// <returns></returns>
        public IEnumerable<TIfcType> InstancesWhere<TIfcType>(Expression<Func<TIfcType, bool>> expression) where TIfcType : IPersistIfcEntity
        {
            return instances.Where(expression);
        }

        /// <summary>
        ///   Creates an Ifc Persistent Instance, this is an undoable operation
        /// </summary>
        /// <typeparam name = "TIfcType"> The Ifc Type, this cannot be an abstract class. An exception will be thrown if the type is not a valid Ifc Type  </typeparam>
        public TIfcType New<TIfcType>() where TIfcType : IPersistIfcEntity, new()
        {
            Transaction txn = Transaction.Current;
            Debug.Assert(txn != null); //model must be in the active transaction to create new entities
            Type t = typeof(TIfcType);
            IPersistIfcEntity newEntity = instances.AddNew_Reversable(this, t);
            if (typeof(IfcRoot).IsAssignableFrom(t))
                ((IfcRoot)newEntity).OwnerHistory = OwnerHistoryAddObject;
            return (TIfcType)newEntity;
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

        abstract public bool Delete(IPersistIfcEntity instance);

        public bool ContainsInstance(IPersistIfcEntity instance)
        {
            return instances.Contains(instance.EntityLabel);
        }

        public bool ContainsInstance(long entityLabel)
        {
             return instances.Contains(entityLabel);
        }

        public IEnumerable<IPersistIfcEntity> Instances 
        {
            get
            {
                return instances;
            }
        }

        public long InstancesCount 
        {
            get
            {
                return instances.Count;
            }
        }

        /// <summary>
        /// Creates a new instance of ifcType with entity label, this operation is NOT undoable
        /// </summary>
        /// <param name="ifcType"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public IPersistIfcEntity AddNew(Type ifcType, long label)
        {
            return instances.AddNew_Reversable(this, ifcType, label);
        }

        abstract public int ParsePart21(System.IO.Stream inputStream, ReportProgressDelegate progressHandler);

        IPersistIfcEntity IModel.OwnerHistoryAddObject
        {
            get { return OwnerHistoryAddObject; }
        }

        IPersistIfcEntity IModel.OwnerHistoryModifyObject
        {
            get { return OwnerHistoryModifyObject; }
        }

        public IfcProject IfcProject
        {
            get { return InstancesOfType<IfcProject>().FirstOrDefault(); }
        }

        IPersistIfcEntity IModel.IfcProject
        {
            get { return IfcProject; }
        }

        public IfcProducts IfcProducts
        {
            get { return new IfcProducts(this); }
        }

        IEnumerable<IPersistIfcEntity> IModel.IfcProducts
        {
            get { return InstancesOfType<IfcProduct>().Cast<IPersistIfcEntity>(); }
        }

        IPersistIfcEntity IModel.DefaultOwningApplication
        {
            get { return DefaultOwningApplication; }
        }

        IPersistIfcEntity IModel.DefaultOwningUser
        {
            get { return DefaultOwningUser; }
        }



        public IIfcFileHeader Header
        {

            get { return header; }
            set { header = value; }
        }

        abstract public IEnumerable<Tuple<string, long>> ModelStatistics();

        


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

            foreach (IPersistIfcEntity ent in Instances)
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


        abstract public string Open(string inputFileName);

        abstract public string Open(string inputFileName, ReportProgressDelegate progDelegate);

        abstract public bool Save();

        public bool SaveAs(string outputFileName)
        {
            // always save file as xbim, with user given filename
            Export(XbimStorageType.XBIM, outputFileName);

            return true;
        }

        abstract public void Import(string inputFileName);


        public IPersistIfcEntity GetInstance(long label)
        {
            return instances.GetOrCreateEntity(label);
        }


        abstract public bool ReOpen();

        abstract public void Close();


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
           return instances.InstancesOfTypeCount(t);
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

        /// <summary>
        ///   Convert xBim file to IFC, IFCXml Or Zip format
        /// </summary>
        /// <param name = "fileType">file type to convert to</param>
        /// <param name = "outputFileName">output filename for the new file after Export</param>
        /// <returns>outputFileName</returns>
        public void Export(XbimStorageType fileType, string outputFileName)
        {
            if (fileType.HasFlag(XbimStorageType.IFCXML))
            {
                // modelServer would have been created with xbim file
                ExportIfcXml(outputFileName);
            }
            else if (fileType.HasFlag(XbimStorageType.IFC))
            {
                // modelServer would have been created with xbim file and readwrite fileaccess
                ExportIfc(outputFileName);
            }
            else if (fileType.HasFlag(XbimStorageType.IFCZIP))
            {
                // modelServer would have been created with xbim file and readwrite fileaccess
                ExportIfc(outputFileName, true, false);
            }
            else
                throw new Exception("Invalid file type. Expected filetypes to Export: IFC, IFCXml, IFCZip");

        }
        public void ExportIfc(string fileName)
        {
            ExportIfc(fileName, false);
        }

        public void ExportIfc(string fileName, bool compress, bool isGZip = true)
        {
            TextWriter ifcFile = null;
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
                ExportIfc(ifcFile);
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

        public void ExportIfc(TextWriter entityWriter)
        {
            WriteHeader(entityWriter);
            foreach (var item in Instances)
            {
                WriteEntity(entityWriter, item);
            }
            WriteFooter(entityWriter);
        }

        private void WriteHeader(TextWriter tw)
        {
            //FileDescription fileDescriptor = new FileDescription("2;1");
            IIfcFileDescription fileDescriptor = Header.FileDescription;
            IIfcFileName fileName = Header.FileName;
            //FileName fileName = new FileName(DateTime.Now)
            //                        {
            //                            //PreprocessorVersion =
            //                            //    string.Format("Xbim.Ifc File Processor version {0}",
            //                            //                  Assembly.GetAssembly(typeof (P21Parser)).GetName().Version),
            //                            //OriginatingSystem =
            //                            //    string.Format("Xbim version {0}",
            //                            //                  Assembly.GetExecutingAssembly().GetName().Version),

            //                            PreprocessorVersion = Header.FileName.PreprocessorVersion,
            //                            OriginatingSystem = Header.FileName.OriginatingSystem,
            //                            Name = Header.FileName.Name,
            //                        };
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



        private void WriteEntity(TextWriter entityWriter, IPersistIfcEntity entity)
        {
      
            entityWriter.Write(string.Format("#{0}={1}(", Math.Abs(entity.EntityLabel), entity.GetType().Name.ToUpper()));
            IfcType ifcType = IfcInstances.IfcEntities[entity.GetType()];
            bool first = true;
            entity.Activate(false);
            foreach (IfcMetaProperty ifcProperty in ifcType.IfcProperties.Values)
            //only write out persistent attributes, ignore inverses
            {
                if (ifcProperty.IfcAttribute.State == IfcAttributeState.DerivedOverride)
                {
                    if (!first)
                        entityWriter.Write(',');
                    entityWriter.Write('*');
                    first = false;
                }
                else
                {
                    Type propType = ifcProperty.PropertyInfo.PropertyType;
                    object propVal = ifcProperty.PropertyInfo.GetValue(entity, null);
                    if (!first)
                        entityWriter.Write(',');
                    WriteProperty(propType, propVal, entityWriter);
                    first = false;
                }
            }
            entityWriter.WriteLine(");");

        }

        private void WriteProperty(Type propType, object propVal, TextWriter entityWriter)
        {
            Type itemType;
            if (propVal == null) //null or a value type that maybe null
                entityWriter.Write('$');

            else if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
            //deal with undefined types (nullables)
            {
                if (typeof(ExpressComplexType).IsAssignableFrom(propVal.GetType()))
                {
                    entityWriter.Write('(');
                    bool first = true;
                    foreach (var compVal in ((ExpressComplexType)propVal).Properties)
                    {
                        if (!first)
                            entityWriter.Write(',');
                        WriteProperty(compVal.GetType(), compVal, entityWriter);
                        first = false;
                    }
                    entityWriter.Write(')');
                }
                else if ((typeof(ExpressType).IsAssignableFrom(propVal.GetType())))
                {
                    ExpressType expressVal = (ExpressType)propVal;
                    WriteValueType(expressVal.UnderlyingSystemType, expressVal.Value, entityWriter);
                }
                else // if (propVal.GetType().IsEnum)
                {
                    WriteValueType(propVal.GetType(), propVal, entityWriter);
                }

            }
            else if (typeof(ExpressComplexType).IsAssignableFrom(propType))
            {
                entityWriter.Write('(');
                bool first = true;
                foreach (var compVal in ((ExpressComplexType)propVal).Properties)
                {
                    if (!first)
                        entityWriter.Write(',');
                    WriteProperty(compVal.GetType(), compVal, entityWriter);
                    first = false;
                }
                entityWriter.Write(')');
            }
            else if (typeof(ExpressType).IsAssignableFrom(propType))
            //value types with a single property (IfcLabel, IfcInteger)
            {
                Type realType = propVal.GetType();
                if (realType != propType)
                //we have a type but it is a select type use the actual value but write out explricitly
                {
                    entityWriter.Write(realType.Name.ToUpper());
                    entityWriter.Write('(');
                    WriteProperty(realType, propVal, entityWriter);
                    entityWriter.Write(')');
                }
                else //need to write out underlying property value
                {
                    ExpressType expressVal = (ExpressType)propVal;
                    WriteValueType(expressVal.UnderlyingSystemType, expressVal.Value, entityWriter);
                }
            }
            else if (typeof(ExpressEnumerable).IsAssignableFrom(propType) &&
                     (itemType = GetItemTypeFromGenericType(propType)) != null)
            //only process lists that are real lists, see cartesianpoint
            {
                entityWriter.Write('(');
                bool first = true;
                foreach (var item in ((ExpressEnumerable)propVal))
                {
                    if (!first)
                        entityWriter.Write(',');
                    WriteProperty(itemType, item, entityWriter);
                    first = false;
                }
                entityWriter.Write(')');
            }
            else if (typeof(IPersistIfcEntity).IsAssignableFrom(propType))
            //all writable entities must support this interface and ExpressType have been handled so only entities left
            {
                entityWriter.Write('#');
                entityWriter.Write(Math.Abs(((IPersistIfcEntity)propVal).EntityLabel));
            }
            else if (propType.IsValueType) //it might be an in-built value type double, string etc
            {
                WriteValueType(propVal.GetType(), propVal, entityWriter);
            }
            else if (typeof(ExpressSelectType).IsAssignableFrom(propType))
            // a select type get the type of the actual value
            {
                if (propVal.GetType().IsValueType) //we have a value type, so write out explicitly
                {
                    entityWriter.Write(propVal.GetType().Name.ToUpper());
                    entityWriter.Write('(');
                    WriteProperty(propVal.GetType(), propVal, entityWriter);
                    entityWriter.Write(')');
                }
                else //could be anything so re-evaluate actual type
                {
                    WriteProperty(propVal.GetType(), propVal, entityWriter);
                }
            }
            else
                throw new Exception(string.Format("Entity  has illegal property {0} of type {1}",
                                                  propType.Name, propType.Name));
        }

        private void WriteValueType(Type pInfoType, object pVal, TextWriter entityWriter)
        {
            if (pInfoType == typeof(Double))
                entityWriter.Write(string.Format(new Part21Formatter(), "{0:R}", pVal));
            else if (pInfoType == typeof(String)) //convert  string
            {
                if (pVal == null)
                    entityWriter.Write('$');
                else
                {
                    entityWriter.Write('\'');
                    entityWriter.Write(IfcText.Escape((string)pVal));
                    entityWriter.Write('\'');
                }
            }
            else if (pInfoType == typeof(Int16) || pInfoType == typeof(Int32) || pInfoType == typeof(Int64))
                entityWriter.Write(pVal.ToString());
            else if (pInfoType.IsEnum) //convert enum
                entityWriter.Write(string.Format(".{0}.", pVal.ToString().ToUpper()));
            else if (pInfoType == typeof(Boolean))
            {
                bool b = (bool)pVal;
                entityWriter.Write(string.Format(".{0}.", b ? "T" : "F"));
            }
            else if (pInfoType == typeof(DateTime)) //convert  TimeStamp
                entityWriter.Write(string.Format(new Part21Formatter(), "{0:T}", pVal));
            else if (pInfoType == typeof(Guid)) //convert  Guid string
            {
                if (pVal == null)
                    entityWriter.Write('$');
                else
                    entityWriter.Write(string.Format(new Part21Formatter(), "{0:G}", pVal));
            }
            else if (pInfoType == typeof(bool?)) //convert  logical
            {
                bool? b = (bool?)pVal;
                entityWriter.Write(!b.HasValue ? "$" : string.Format(".{0}.", b.Value ? "T" : "F"));
            }
            else
                throw new ArgumentException(string.Format("Invalid Value Type {0}", pInfoType.Name), "pInfoType");
        }


        private void WriteFooter(TextWriter tw)
        {
            tw.WriteLine("ENDSEC;");
            tw.WriteLine("END-ISO-10303-21;");
        }

        public void ExportIfcXml(string ifcxmlFileName)
        {
            FileStream xmlOutStream = null;
            try
            {
                xmlOutStream = new FileStream(ifcxmlFileName, FileMode.Create, FileAccess.ReadWrite);
                XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
                using (XmlWriter xmlWriter = XmlWriter.Create(xmlOutStream, settings))
                {
                    IfcXmlWriter writer = new IfcXmlWriter();

                    // when onverting ifc to xml, 
                    // 1. you can specify perticular lines in fic file as below below 
                    // 2. OR pass null to convert full ifc format to xml
                    //List<IPersistIfcEntity> instances = new List<IPersistIfcEntity>();
                    //instances.Add(this.GetInstance(79480));
                    //instances.Add(this.GetInstance(2717770));
                    //writer.Write(this, xmlWriter, instances);

                    writer.Write(this, xmlWriter, null);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to write IfcXml file " + ifcxmlFileName, e);
            }
            finally
            {
                if (xmlOutStream != null) xmlOutStream.Close();
            }
        }

        public IEnumerable<long> EntityLabels
        {
            get
            {
                return instances;
            }
        }



        /// <summary>
        ///   Creates an Ifc Persistent Instance from an entity name string and label, this is NOT an undoable operation
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
        /// <summary>
        /// Creates an Ifc Persistent Instance from an entity type and label, this is NOT an undoable operation
        /// </summary>
        /// <param name="ifcType"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        internal IPersistIfc CreateInstance(Type ifcType, long ?label)
        {
          
             return instances.AddNew(this,ifcType,label.Value);
                
        }

      

    }
}
