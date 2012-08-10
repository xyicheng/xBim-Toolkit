#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.IO
// Filename:    XbimFileModelServer.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Xbim.Ifc.GeometricConstraintResource;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.PresentationAppearanceResource;
using Xbim.Ifc.PresentationOrganizationResource;
using Xbim.Ifc.PresentationResource;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.ProfileResource;
using Xbim.Ifc.RepresentationResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;
using Xbim.XbimExtensions.Transactions;
using Xbim.XbimExtensions.Transactions.Extensions;


#endregion

namespace Xbim.IO
{


    public class XbimFileModelServer : XbimModelServer
    {
        private BinaryReader _binaryReader;
        private Dictionary<Type, List<long>> _entityTypes;
        private XbimIndex _entityOffsets;
        private string _filename;
        private IfcFileHeader _header;
        private BinaryWriter _binaryWriter;
        private Stream _stream;

        public override IfcFileHeader Header
        {

            get { return _header; }
        }

        public string Filename
        {
            get { return _filename; }
        }



        public XbimFileModelServer(Stream stream)
        {
            _stream = stream;
            FileStream fstream = _stream as FileStream;
            if (fstream != null)
                _filename = fstream.Name;
        }

        public XbimFileModelServer()
        {
        }

        public XbimFileModelServer(string fileName, FileAccess fileAccess = FileAccess.Read)
        {
            Open(fileName, fileAccess);
        }

        public XbimIndex EntityOffsets
        {
            get { return _entityOffsets; }
        }

        /// <summary>
        ///   Opens an xbim model server file, exception is thrown if errors are encountered
        /// </summary>
        /// <param name = "filename"></param>
        /// <returns></returns>
        public void Open(string filename, FileAccess fileAccess = FileAccess.Read)
        {
            try
            {
                Dispose();
                // now we have streamReader and streamWriter, choose the right one to use
                _stream = new FileStream(filename, FileMode.Open, fileAccess);

                Initialise();
                _filename = filename;

                // we have _header of the opened file, set that header to the Header property of XbimModelServer
                Header.FileName = _header.FileName;
                Header.FileDescription = _header.FileDescription;
                Header.FileSchema = _header.FileSchema;
            }
            catch (Exception e)
            {
                Dispose();
                throw new Exception("Failed to open " + filename, e);
            }
        }

        /// <summary>
        ///   Imports an Ifc file into the model server, throws exception if errors are encountered
        /// </summary>
        /// <param name = "filename"></param>
        public string ImportIfc(string filename)
        {
            string xbimFilename = Path.ChangeExtension(filename, "xbim");
            return ImportIfc(filename, xbimFilename, null);
        }

        /// <summary>
        ///   Imports an Ifc file into the model server, throws exception if errors are encountered, gives the xbim file the same name as the ifc file
        /// </summary>
        /// <param name = "filename"></param>
        public string ImportIfc(string filename, ReportProgressDelegate progress)
        {
            string xbimFilename = Path.ChangeExtension(filename, "xbim");
            return ImportIfc(filename, xbimFilename, progress);
        }
        public string ImportIfc(string filename, string xbimFilename)
        {
            return ImportIfc(filename, xbimFilename, null);
        }
        /// <summary>
        ///   Imports an Ifc file into the model server, throws exception if errors are encountered
        /// </summary>
        /// <param name = "filename"></param>
        public string ImportIfc(string filename, string xbimFilename, ReportProgressDelegate progress)
        {
            Stream inputFile = null;
            IfcInputStream input = null;

            IfcZipInputStream zipstream = null;
                                   
            try
            {
                Dispose(); //clear up any issues from previous runs

                string ext = Path.GetExtension(filename).ToLower();
                if (ext == ".zip" || ext == ".ifczip")
                {
                   
                    zipstream = new IfcZipInputStream(filename);
                    if (zipstream.FileExt == ".ifc")
                    {
                        inputFile = zipstream.InputFile;
                    }
                    else if (zipstream.FileExt == ".ifcxml")
                    {
                        //TODO: Import from zip with .ifcxml file types to do, uses IModel
                        throw new NotImplementedException("Import from zip with .ifcxml file type: not implemented yet");
                    }
                    
                }
                else
                {
                    inputFile = new FileStream(filename, FileMode.Open, FileAccess.Read);
                }
                //attach it to the Ifc Stream Parser
                input = new IfcInputStream(inputFile);
                
                using (FileStream outputFile = new FileStream(xbimFilename, FileMode.Create, FileAccess.ReadWrite))
                {
                    int errors = input.Index(outputFile, progress);
                    if (errors == 0)
                    {
                        _stream = new FileStream(xbimFilename, FileMode.Open, FileAccess.ReadWrite);
                        Initialise();
                        _filename = xbimFilename;
                        return _filename;
                    }
                    else
                    {
                        throw new Xbim.Common.Exceptions.XbimException("Ifc file reading or initialisation errors\n" + input.ErrorLog.ToString());
                    }
                }

            }
            catch (FileNotFoundException)
            {
                Close();
                throw;
            }
            catch (Exception e)
            {
                Close();
                throw new Xbim.Common.Exceptions.XbimException("Failed to import " + filename + "\n" + input.ErrorLog.ToString(), e);
            }
            finally
            {
                if (inputFile != null) inputFile.Close();
                if (zipstream != null) zipstream.Close();
            }
        }


        /// <summary>
        ///   Strips only semantic objects from the file, ignores representation and creates only an Xbim model server file
        /// </summary>
        /// <param name = "semanticFilename"></param>
        public void ExtractSemantic(string semanticFilename)
        {
            ExtractSemantic(semanticFilename, null, null);
        }

        /// <summary>
        ///   This creates an Xbim model server file that contains the high level semantic objects without their representation
        ///   If exportFormat is specified that version of the sematic is writtent out too, to the appropriate file
        /// </summary>
        public void ExtractSemantic(string semanticFilename, XbimStorageType? exportFormat,
                                    HashSet<string> ignorableProperties)
        {

            FileStream semanticFile = null;
            BinaryWriter semanticBinaryWriter = null;
            try
            {

                semanticFile = new FileStream(semanticFilename, FileMode.Create, FileAccess.Write);
                semanticBinaryWriter = new BinaryWriter(semanticFile);

                HashSet<ulong> toDrop = new HashSet<ulong>();
                XbimIndex written = null;

                foreach (var product in InstancesOfType<IfcProduct>())
                {
                    DropAll(product.Representation, toDrop);
                    DropAll(product.ObjectPlacement, toDrop);
                }
                foreach (var typeProduct in InstancesOfType<IfcTypeProduct>())
                {
                    RepresentationMapList maps = typeProduct.RepresentationMaps;
                    if (maps != null)
                    {
                        foreach (var item in maps)
                            DropAll(item, toDrop);
                    }
                }
                foreach (var relSpaceBoundary in InstancesOfType<IfcRelSpaceBoundary>())
                {
                    DropAll(relSpaceBoundary.ConnectionGeometry, toDrop);
                }

                written = WriteToFileSemantic(semanticBinaryWriter, toDrop);
                semanticBinaryWriter.Close();
                semanticFile.Close();
                semanticFile = null;
                semanticBinaryWriter = null;
                if (exportFormat.HasValue)
                {
                    using (XbimFileModelServer semModel = new XbimFileModelServer(semanticFilename))
                    {
                        if (exportFormat.Value.HasFlag(XbimStorageType.IFC))
                        {
                            string ifcFileName = "";
                            try
                            {
                                ifcFileName = Path.ChangeExtension(semanticFilename, "ifc");
                                semModel.ExportIfc(ifcFileName);
                            }
                            catch (Exception e)
                            {
                                throw new Exception("Failed exporting Ifc File " + ifcFileName, e);
                            }
                        }

                        if (exportFormat.Value.HasFlag(XbimStorageType.IFCZIP))
                        {
                            string ifcFileName = "";
                            try
                            {
                                ifcFileName = Path.ChangeExtension(semanticFilename, "ifcx");
                                semModel.ExportIfc(ifcFileName, true);
                            }
                            catch (Exception e)
                            {
                                throw new Exception("Failed exporting Ifc File " + ifcFileName, e);
                            }
                        }

                        if (exportFormat.Value.HasFlag(XbimStorageType.IFCXML))
                        {
                            string ifcxmlFileName = "";
                            try
                            {
                                ifcxmlFileName = Path.ChangeExtension(semanticFilename, "ifcxml");
                                semModel.ExportIfcXml(ifcxmlFileName);
                            }
                            catch (Exception e)
                            {
                                throw new Exception("Failed exporting IfcXml File " + ifcxmlFileName, e);
                            }
                        }
                    }
                }
                toDrop.Clear();

            }
            catch (Exception e)
            {
                throw new Exception("Error splitting semantic and representation data\n", e);
            }
            finally
            {
                if (semanticFile != null) semanticFile.Close();
            }
        }


        private XbimIndex WriteToFileSemantic(BinaryWriter semanticBinaryWriter, HashSet<ulong> toDrop)
        {
            XbimIndex index = new XbimIndex(EntityOffsets.HighestLabel);
            semanticBinaryWriter.Write(0L); //data
            int reservedSize = 32;
            semanticBinaryWriter.Write(reservedSize);
            semanticBinaryWriter.Write(new byte[reservedSize]);
            // semanticBinaryWriter.Write(Assembly.GetAssembly(typeof(Xbim.XbimExtensions.Parser.P21Parser)).GetName().Version.ToString());
            // semanticBinaryWriter.Write(Assembly.GetExecutingAssembly().GetName().Version.ToString());
            _header.Write(semanticBinaryWriter);

            //release anything that is in memory
            _entityOffsets.DropAll();

            Dictionary<Type, List<long>> entityTypes = new Dictionary<Type, List<long>>();
            foreach (var entity in Instances.Where(inst =>
                                                   !(inst is IfcRepresentationItem
                                                     || (inst is IfcObjectPlacement)
                                                     || (inst is IfcPresentationLayerAssignment)
                                                     || (inst is IfcMaterialDefinitionRepresentation)
                                                     || (inst is IfcPresentationStyleAssignment)
                                                     || (inst is IfcPresentationStyle)
                                                     || (inst is IfcPreDefinedItem)
                                                     || (inst is IfcStyleModel)
                                                     || (inst is IfcProfileDef)
                                                     || (inst is IfcConnectionGeometry)
                                                     || (inst is IfcSurfaceStyleElementSelect)
                                                     || (inst is IfcCurveStyleFontPattern)
                                                     || (inst is IfcCurveStyleFont))
                ))
            {
                ulong id = (ulong)Math.Abs(entity.EntityLabel);
                if (!toDrop.Contains(id))
                    WriteRecursively(semanticBinaryWriter, entity, index, entityTypes, toDrop);
            }
            long start = semanticBinaryWriter.BaseStream.Position;
            semanticBinaryWriter.Write((long)index.Count);
            semanticBinaryWriter.Write(index.HighestLabel);

            Dictionary<Type, short> classNames = new Dictionary<Type, short>(entityTypes.Count);
            semanticBinaryWriter.Write(entityTypes.Count);
            short i = 0;
            foreach (var eType in entityTypes)
            {
                classNames.Add(eType.Key, i);
                semanticBinaryWriter.Write(eType.Key.Name.ToUpper());
                semanticBinaryWriter.Write(i);
                i++;
            }
            foreach (var item in index)
            {
                semanticBinaryWriter.Write(item.EntityLabel);
                semanticBinaryWriter.Write(item.Offset);
                semanticBinaryWriter.Write(classNames[item.Type]);
            }
            semanticBinaryWriter.Write(0L); //no changes following
            semanticBinaryWriter.Seek(0, SeekOrigin.Begin);
            semanticBinaryWriter.Write(start);
            return index;
        }

        private XbimIndex WriteToFileRepresentation(BinaryWriter semanticBinaryWriter, HashSet<ulong> toDrop)
        {
            XbimIndex index = new XbimIndex(EntityOffsets.HighestLabel);
            semanticBinaryWriter.Write(0L); //data
            int reservedSize = 32;
            semanticBinaryWriter.Write(reservedSize);
            semanticBinaryWriter.Write(new byte[reservedSize]);
            _header.Write(semanticBinaryWriter);
            Dictionary<Type, List<long>> entityTypes = new Dictionary<Type, List<long>>();
            foreach (var entity in Instances.Where(inst => !toDrop.Contains((ulong)Math.Abs(inst.EntityLabel))))
            {
                WriteRecursively(semanticBinaryWriter, entity, index, entityTypes, toDrop);
            }
            long start = semanticBinaryWriter.BaseStream.Position;
            semanticBinaryWriter.Write((long)index.Count);
            semanticBinaryWriter.Write(index.HighestLabel);

            Dictionary<Type, short> classNames = new Dictionary<Type, short>(entityTypes.Count);
            semanticBinaryWriter.Write(entityTypes.Count);
            short i = 0;
            foreach (var eType in entityTypes)
            {
                classNames.Add(eType.Key, i);
                semanticBinaryWriter.Write(eType.Key.Name.ToUpper());
                semanticBinaryWriter.Write(i);
                i++;
            }
            foreach (var item in index)
            {
                semanticBinaryWriter.Write(item.EntityLabel);
                semanticBinaryWriter.Write(item.Offset);
                semanticBinaryWriter.Write(classNames[item.Type]);
            }
            semanticBinaryWriter.Seek(0, SeekOrigin.Begin);
            semanticBinaryWriter.Write(start);
            return index;
        }


        private void WriteRecursively(BinaryWriter binaryWriter, IPersistIfcEntity entity, XbimIndex index,
                                      Dictionary<Type, List<long>> entityTypes, HashSet<ulong> toDrop)
        {
            long label = Math.Abs(entity.EntityLabel);

            if (index.Contains(label)) return; //don't write twice
            long fileOffset = binaryWriter.BaseStream.Position;
            Type t = entity.GetType();
            index.Add(new XbimIndexEntry(label, fileOffset, t));
            List<long> offsets;
            if (!entityTypes.TryGetValue(t, out offsets))
            {
                offsets = new List<long>();
                entityTypes.Add(t, offsets);
            }
            offsets.Add(fileOffset);

            byte[] binaryData = GetEntityBinaryData(entity);
            byte[] dereferencedStream;

            bool modified = DerefenceEntities(binaryData, toDrop, out dereferencedStream);

            binaryWriter.Write(dereferencedStream.Length);
            binaryWriter.Write(dereferencedStream);

            if (modified || !entity.Activated) Activate(entity, dereferencedStream);

            IfcType ifcType = IfcInstances.IfcEntities[entity];

            foreach (var ifcProperty in ifcType.IfcProperties.Values.Where(p =>
                                                                           typeof(IPersistIfcEntity).IsAssignableFrom(
                                                                               p.PropertyInfo.PropertyType)
                                                                           ||
                                                                           typeof(ExpressSelectType).IsAssignableFrom(
                                                                               p.PropertyInfo.PropertyType)
                                                                           ||
                                                                           (
                                                                               typeof(ExpressEnumerable).
                                                                                   IsAssignableFrom(
                                                                                       p.PropertyInfo.PropertyType)
                                                                               &&
                                                                               (
                                                                                   typeof(IPersistIfcEntity).
                                                                                       IsAssignableFrom(
                                                                                           GetItemTypeFromGenericType(
                                                                                               p.PropertyInfo.
                                                                                                   PropertyType))
                                                                                   ||
                                                                                   typeof(ExpressEnumerable).
                                                                                       IsAssignableFrom(
                                                                                           p.PropertyInfo.PropertyType)
                                                                               )
                                                                           )))
            {
                object pVal = ifcProperty.PropertyInfo.GetValue(entity, null);
                if (pVal != null && !ifcProperty.IfcAttribute.IsDerivedOverride)
                {
                    IPersistIfcEntity ifcPersistsVal = pVal as IPersistIfcEntity;

                    if (ifcPersistsVal != null)
                        WriteRecursively(binaryWriter, ifcPersistsVal, index, entityTypes, toDrop);
                    else if (!typeof(ExpressSelectType).IsAssignableFrom(pVal.GetType()))
                    //it must be a list of IPersistIfcEntity
                    {
                        if (typeof(IPersistIfcEntity).IsAssignableFrom(GetItemTypeFromGenericType(pVal.GetType())))
                        {
                            IEnumerable<IPersistIfcEntity> ifcListVal = pVal as IEnumerable<IPersistIfcEntity>;
                            if (ifcListVal != null)
                                foreach (var item in ifcListVal)
                                {
                                    WriteRecursively(binaryWriter, item, index, entityTypes, toDrop);
                                }
                        }
                    }
                }
            }
        }

        private static void DropAll(IPersistIfcEntity entity, HashSet<ulong> toDrop)
        {
            if (entity == null || entity is IfcGeometricRepresentationContext) return; //don't drop geom rep contexts
            ulong label = (ulong)Math.Abs(entity.EntityLabel);
            if (!toDrop.Contains(label)) toDrop.Add(label);
            else return; //already processed it
            IfcType ifcType = IfcInstances.IfcEntities[entity];
            foreach (var ifcProperty in ifcType.IfcProperties.Values.Where(p =>
                                                                           typeof(IPersistIfcEntity).IsAssignableFrom(
                                                                               p.PropertyInfo.PropertyType)
                                                                           ||
                                                                           typeof(ExpressSelectType).IsAssignableFrom(
                                                                               p.PropertyInfo.PropertyType)
                                                                           ||
                                                                           (
                                                                               typeof(ExpressEnumerable).
                                                                                   IsAssignableFrom(
                                                                                       p.PropertyInfo.PropertyType)
                                                                               &&
                                                                               (
                                                                                   typeof(IPersistIfcEntity).
                                                                                       IsAssignableFrom(
                                                                                           GetItemTypeFromGenericType(
                                                                                               p.PropertyInfo.
                                                                                                   PropertyType))
                                                                                   ||
                                                                                   typeof(ExpressEnumerable).
                                                                                       IsAssignableFrom(
                                                                                           p.PropertyInfo.PropertyType)
                                                                               )
                                                                           )))
            {
                object pVal = ifcProperty.PropertyInfo.GetValue(entity, null);
                if (pVal != null && !ifcProperty.IfcAttribute.IsDerivedOverride)
                {
                    IPersistIfcEntity ifcPersistsVal = pVal as IPersistIfcEntity;

                    if (ifcPersistsVal != null)
                        DropAll(ifcPersistsVal, toDrop);
                    else if (!typeof(ExpressSelectType).IsAssignableFrom(pVal.GetType()))
                    //it must be a list of IPersistIfcEntity
                    {
                        if (typeof(IPersistIfcEntity).IsAssignableFrom(GetItemTypeFromGenericType(pVal.GetType())))
                        {
                            IEnumerable<IPersistIfcEntity> ifcListVal = pVal as IEnumerable<IPersistIfcEntity>;
                            if (ifcListVal != null)
                                foreach (var item in ifcListVal)
                                {
                                    DropAll(item, toDrop);
                                }
                        }
                    }
                }
            }
        }

        private void WriteHeader()
        {
            if (!_stream.CanSeek)
                throw new Exception("Input Stream must be able to support Seek operations");
            _stream.Seek(0, SeekOrigin.Begin);
            BinaryWriter binaryWriter = new BinaryWriter(_stream);
            binaryWriter.Write(0L); //data
            int reservedSize = 32;
            binaryWriter.Write(reservedSize);
            binaryWriter.Write(new byte[reservedSize]);
        }

        private void Initialise()
        {
            if (!_stream.CanSeek)
                throw new Exception("Input Stream must be able to support Seek operations");

            Transaction currentTrans = Transaction.Current;
            if (currentTrans != null) currentTrans.Exit();
            _binaryReader = new BinaryReader(_stream);
            long start = _binaryReader.ReadInt64();
            int reservedSize = _binaryReader.ReadInt32();
            byte[] reservedBytes = _binaryReader.ReadBytes(reservedSize);

            _header = new IfcFileHeader();
            _header.Read(_binaryReader);

            _binaryReader.BaseStream.Seek(start, SeekOrigin.Begin);
            long entityCount = _binaryReader.ReadInt64();
            long highestLabel = _binaryReader.ReadInt64();
            _entityOffsets = new XbimIndex(entityCount);
            //set up the required ownership objects

            int entityTypeCount = _binaryReader.ReadInt32();
            Dictionary<short, Type> classIndices = new Dictionary<short, Type>(entityTypeCount);
            for (int i = 0; i < entityTypeCount; i++)
            {
                string className = _binaryReader.ReadString();
                short _idx = _binaryReader.ReadInt16();
                classIndices.Add(_idx, IfcInstances.IfcTypeLookup[className].Type);
            }
            for (int i = 0; i < entityCount; i++)
            {
                _entityOffsets.Add(new XbimIndexEntry(_binaryReader.ReadInt64(), _binaryReader.ReadInt64(),
                                                      classIndices[_binaryReader.ReadInt16()]));
            }

            _entityTypes = new Dictionary<Type, List<long>>(entityTypeCount);
            foreach (var item in _entityOffsets)
            {
                List<long> labels;
                if (!_entityTypes.TryGetValue(item.Type, out labels))
                {
                    labels = new List<long>();
                    _entityTypes.Add(item.Type, labels);
                }
                labels.Add(item.EntityLabel);
            }
            long nextIndexStart = _binaryReader.ReadInt64();

            while (nextIndexStart > 0)
            {
                _stream.Seek(-sizeof(long), SeekOrigin.Current); //move back
                MergeChanges(_stream);
                nextIndexStart = _binaryReader.ReadInt64();
            }
            ToWrite.Clear();
            if (currentTrans != null) currentTrans.Enter();
        }

        public Dictionary<Type, List<long>> EntityTypes
        {
            get { return _entityTypes; }
        }

        public IPersistIfcEntity GetOrCreateEntity(long label)
        {
            return GetOrCreateEntity(label, null);
        }

        protected override IPersistIfcEntity GetOrCreateEntity(long label, Type type)
        {
            //try
            //{

            XbimIndexEntry paramEntry = _entityOffsets[label];
            IPersistIfcEntity paramEntity = paramEntry.Entity;
            if (paramEntity == null)
            {
                //Debug.Assert(type == paramEntry.Type);
                paramEntity = CreateEntity(paramEntry.EntityLabel, paramEntry.Type);
                paramEntry.Entity = paramEntity;
            }
            return paramEntity;

            //}
            //catch (Exception e)
            //{

            //    throw;
            //}
        }

        protected void ActivateEntity(XbimIndexEntry entry, IPersistIfcEntity entity)
        {
            long offset = Math.Abs(entry.Offset);

            if (entry.Offset > 0)
            {
                lock (this)
                {
                    byte[] bLen = new byte[sizeof(int)];
                    _stream.Seek(offset, SeekOrigin.Begin);
                    _stream.Read(bLen, 0, sizeof(int));
                    int len = BitConverter.ToInt32(bLen, 0);

#if DEBUG
                    long maxByte = 0xffff; //should be about as big as an object should ever get
                    if (_stream is FileStream)
                        maxByte = ((FileStream)_stream).Length;

                    if (len < 0 || len > maxByte)
                    {
                        
                        throw new Xbim.Common.Exceptions.XbimException("Error in xbim file: invalid entity binary length. Length: " + len.ToString());
                    }
#endif


                    byte[] bContent = new byte[len];
                    _stream.Read(bContent, 0, len);
                    using (MemoryStream ms = new MemoryStream(bContent))
                    {
                        using (BinaryReader br = new BinaryReader(ms))
                        {
                            PopulateProperties(entity, br);
                        }
                    }
                }
            }

        }


        public override void Dispose()
        {
            Close();
            // Prevent us Re-opening the file
            _filename = null;
        }

        public override IEnumerable<TIfcType> InstancesOfType<TIfcType>()
        {
            if (InstancesCount > 0)
            {
                Type type = typeof(TIfcType);
                IfcType ifcType = IfcInstances.IfcEntities[type];
                IList<Type> types = ifcType.NonAbstractSubTypes;

                foreach (var entType in _entityTypes)
                {
                    if (types.Contains(entType.Key))
                    {
                        foreach (var entityLabel in entType.Value)
                        {
                            IPersistIfcEntity entity = GetOrCreateEntity(entityLabel);
                            yield return (TIfcType)entity;
                        }
                    }
                }
            }
        }

        public override IPersistIfcEntity AddNew(IfcType ifcType, long label)
        {

            Debug.Assert(typeof(IPersistIfcEntity).IsAssignableFrom(ifcType.Type), "Type mismacth: IPersistIfcEntity");
            //return (IPersistIfcEntity)CreateInstance(ifcType, label);

            Type t = ifcType.Type;
            IPersistIfcEntity newEntity;
            XbimIndexEntry entry = _entityOffsets.AddNew(t, out newEntity, label);
            List<long> labels;
            if (!_entityTypes.TryGetValue(t, out labels))
            {
                labels = new List<long>();
                _entityTypes.Add_Reversible(t, labels);
            }
            labels.Add_Reversible(label);

            newEntity.Bind(this, label);


            return newEntity;

        }

        public override TIfcType New<TIfcType>()
        {
            Transaction txn = Transaction.Current;
            Debug.Assert(txn != null); //model must be in the active transaction to create new entities
            Type t = typeof(TIfcType);
            IPersistIfcEntity newEntity;
            XbimIndexEntry entry = _entityOffsets.AddNew<TIfcType>(out newEntity);
            List<long> labels;
            if (!_entityTypes.TryGetValue(t, out labels))
            {
                labels = new List<long>();
                _entityTypes.Add_Reversible(t, labels);
            }
            labels.Add_Reversible(entry.EntityLabel);

            newEntity.Bind(this, entry.EntityLabel);
            if (typeof(IfcRoot).IsAssignableFrom(t))
                ((IfcRoot)newEntity).OwnerHistory = OwnerHistoryAddObject;
            ToWrite.Add_Reversible(newEntity);
            return (TIfcType)newEntity;
        }

        public override bool ContainsInstance(IPersistIfcEntity instance)
        {
            Type targetType = instance.GetType();
            foreach (var eType in _entityTypes)
            {
                if (eType.Key == targetType)
                {
                    foreach (var offset in eType.Value)
                    {
                        if (_entityOffsets.Contains(offset))
                            return _entityOffsets[offset].Entity != null;
                    }
                }
            }
            return false;
        }

        public override bool ContainsInstance(long entityLabel)
        {
            return _entityOffsets.Contains(entityLabel);
        }

        protected override void TransactionFinalised()
        {

        }

        /// <summary>
        ///   Writes the in memory data of the entity to a stream
        /// </summary>
        /// <param name = "entityStream"></param>
        /// <param name = "entityWriter"></param>
        /// <param name = "item"></param>
        private int WriteEntityToSteam(MemoryStream entityStream, BinaryWriter entityWriter, IPersistIfcEntity item)
        {
            entityWriter.Seek(0, SeekOrigin.Begin);
            entityWriter.Write((int)0);
            XbimIndexEntry entry = _entityOffsets[item.EntityLabel];
            WriteEntity(entityWriter, item);
            int len = Convert.ToInt32(entityStream.Position);
            entityWriter.Seek(0, SeekOrigin.Begin);
            entityWriter.Write(len);
            entityWriter.Seek(0, SeekOrigin.Begin);
            return len;
        }

        public override IEnumerable<IPersistIfcEntity> Instances
        {
            get
            {
                foreach (XbimIndexEntry entry in _entityOffsets)
                {
                    IPersistIfcEntity entity = GetOrCreateEntity(entry.EntityLabel);
                    yield return entity;
                }
            }
        }

        public override long InstancesCount
        {
            get { return _entityOffsets == null ? 0 : _entityOffsets.Count; }
        }


        public override IPersistIfcEntity GetInstance(long entityLabel)
        {
            long posLabel = Math.Abs(entityLabel);
            XbimIndexEntry entry = _entityOffsets[posLabel];
            IPersistIfcEntity entity = entry.Entity;
            if (entity != null && entity.Activated)
                return entity; //already loaded and activated
            else if (entity == null) //Create one
            {
                entity = CreateEntity(entry.EntityLabel, entry.Type);
                entry.Entity = entity;
            }
            ActivateEntity(entry, entity);
            entity.Bind(this, posLabel);
            return entity;
        }

        public byte[] GetEntityBinaryData(IPersistIfcEntity entity)
        {
            if (ToWrite.Contains(entity)) //we have changed it in memory but not written to cache yet
            {
                byte[] buffer;
                using (MemoryStream entityStream = new MemoryStream(4096))
                {
                    using (BinaryWriter entityWriter = new BinaryWriter(entityStream))
                    {
                        int len = WriteEntityToSteam(entityStream, entityWriter, entity) - sizeof(Int32);
                        buffer = new byte[len];
                        entityStream.Seek(sizeof(Int32), SeekOrigin.Begin);
                        entityStream.Read(buffer, 0, len);
                    }
                }
                return buffer;
            }
            else
            {
                XbimIndexEntry entry = _entityOffsets[Math.Abs(entity.EntityLabel)];
                BinaryReader br = _binaryReader;
                long offset = Math.Abs(entry.Offset);
                br.BaseStream.Seek(offset, SeekOrigin.Begin);
                int len = br.ReadInt32();
                return br.ReadBytes(len);
            }
        }

        public void Activate(IPersistIfcEntity entity, byte[] binaryData)
        {
            using (MemoryStream memoryStream = new MemoryStream(binaryData))
            {
                using (BinaryReader reader = new BinaryReader(memoryStream))
                {
                    PopulateProperties(entity, reader);
                    entity.Bind(this, Math.Abs(entity.EntityLabel));
                }
            }
        }

        public override long Activate(IPersistIfcEntity entity, bool write)
        {

            long label = entity.EntityLabel;
            //   Debug.Assert((!write && label < 0) || (write && label > 0)); //cannot call to write if we hven't read current state;
            long posLabel = Math.Abs(label);

            if (!write) //we want to activate for reading, if entry offset == 0 it is a new oject with no data set
            {
                XbimIndexEntry entry = _entityOffsets[posLabel];
                if (entry.Offset != 0)
                    ActivateEntity(entry, entity);
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

        public long Count<TIfcType>()
        {
            Type type = typeof(TIfcType);
            IfcType ifcType = IfcInstances.IfcEntities[type];
            IList<Type> types = ifcType.NonAbstractSubTypes;
            long count = 0;
            foreach (var entType in _entityTypes)
            {
                if (types.Contains(entType.Key))
                    count += entType.Value.Count;
            }
            return count;
        }

        public override bool ReOpen()
        {
            try
            {
                if (string.IsNullOrEmpty(_filename))
                    return false;

                _stream = new FileStream(_filename, FileMode.Open, FileAccess.Read);
                _binaryReader = new BinaryReader(_stream);
                // Reinitialise everthing after we previously closed
                Initialise();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override void Close()
        {
            if (_binaryReader != null)
            {
                _binaryReader.Dispose();
                _binaryReader = null;
            }

            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }

            if (_binaryWriter != null)
            {
                _binaryWriter.Dispose();
                _binaryWriter = null;
            }

            ToWrite.Clear();
            undoRedoSession = null;
            _entityOffsets = null;
        }

        /// <summary>
        ///   Returns the number of instances of a specific type, NB does not include subtypes
        /// </summary>
        /// <param name = "t"></param>
        /// <returns></returns>
        public override long InstancesOfTypeCount(Type t)
        {
            if (_entityTypes.Keys.Contains(t))
                return _entityTypes[t].Count;
            else
                return 0;
        }

        /// <summary>
        ///   Only executes the flagged validation routines
        /// </summary>
        /// <param name = "errStream"></param>
        /// <param name = "progressDelegate"></param>
        /// <param name = "validateFlags"></param>
        /// <returns></returns>
        public override int Validate(TextWriter errStream, ReportProgressDelegate progressDelegate,
                                     ValidationFlags validateFlags)
        {
            IndentedTextWriter tw = new IndentedTextWriter(errStream, "    ");
            tw.Indent = 0;
            double total = InstancesCount;
            int idx = 0;
            int errors = 0;
            int percentage = -1;

            foreach (var ent in Instances)
            {
                idx++;
                errors += XbimMemoryModel.Validate(ent, tw, validateFlags);

                if (progressDelegate != null)
                {
                    int newPercentage = (int)((double)idx / total * 100.0);
                    if (newPercentage != percentage) progressDelegate(percentage, "");
                    percentage = newPercentage;
                }
            }
            return errors;
        }

        public override void WriteChanges(BinaryWriter dataStream)
        {

            UndoRedo.Exit();
            using (MemoryStream entityStream = new MemoryStream(Int16.MaxValue))
            {
                using (BinaryWriter entityWriter = new BinaryWriter(entityStream))
                {
                    XbimIndex changesIndex = new XbimIndex(_entityOffsets.HighestLabel);
                    long start = dataStream.BaseStream.Position;
                    dataStream.Write(new Byte[sizeof(long)], 0, sizeof(long));
                    foreach (var item in ToWrite)
                    {

                        entityWriter.Seek(0, SeekOrigin.Begin);
                        entityWriter.Write((int)0);
                        XbimIndexEntry entry = new XbimIndexEntry(_entityOffsets[item.EntityLabel]);
                        changesIndex.Add(entry);
                        WriteEntity(entityWriter, item);
                        int len = Convert.ToInt32(entityStream.Position);
                        entityWriter.Seek(0, SeekOrigin.Begin);
                        entityWriter.Write(len);
                        entityWriter.Seek(0, SeekOrigin.Begin);
                        dataStream.Seek(0, SeekOrigin.End);
                        entry.Offset = dataStream.BaseStream.Position;
                        dataStream.Write(entityStream.GetBuffer(), 0, len);
                    }

                    long indexStart = dataStream.BaseStream.Position;
                    changesIndex.Write(dataStream);
                    dataStream.BaseStream.Seek(start, SeekOrigin.Begin);
                    dataStream.Write(BitConverter.GetBytes(indexStart), 0, sizeof(long));
                    dataStream.Seek(0, SeekOrigin.End);
                }

            }

        }
        public override void MergeChanges(Stream dataStream)
        {

            byte[] indexLen = new byte[sizeof(long)];
            dataStream.Read(indexLen, 0, sizeof(long));
            long indexStart = BitConverter.ToInt64(indexLen, 0);
            dataStream.Seek(indexStart, SeekOrigin.Begin);
            XbimIndex changes = XbimIndex.Read(dataStream);
            long endIndex = dataStream.Position;
            foreach (var item in changes)
            {
                item.Entity = CreateEntity(item.EntityLabel, item.Type);
                List<long> labels;
                if (!_entityTypes.TryGetValue(item.Type, out labels))
                {
                    labels = new List<long>();
                    _entityTypes.Add_Reversible(item.Type, labels);
                }
                labels.Add_Reversible(item.EntityLabel);
                if (_entityOffsets.Contains(item.EntityLabel)) //we have one to amend
                {
                    XbimIndexEntry idx = _entityOffsets[item.EntityLabel];
                    idx.Entity = item.Entity; //update entity
                }
                else //new entity
                    _entityOffsets.Add(item);
                item.Entity.Bind(this, item.EntityLabel);
                ToWrite.Add(item.Entity);

            }
            foreach (var item in changes) //load the data
            {
                lock (this)
                {
                    byte[] bLen = new byte[sizeof(int)];
                    dataStream.Seek(item.Offset, SeekOrigin.Begin);
                    dataStream.Read(bLen, 0, sizeof(int));
                    int len = BitConverter.ToInt32(bLen, 0);
                    byte[] bContent = new byte[len];
                    dataStream.Read(bContent, 0, len);
                    using (MemoryStream ms = new MemoryStream(bContent))
                    {
                        using (BinaryReader br = new BinaryReader(ms))
                        {
                            PopulateProperties(item.Entity, br);
                        }
                    }
                }
            }
            //leave us at the end of the index
            dataStream.Seek(endIndex, SeekOrigin.Begin);
        }

        override public bool Save()
        {
            lock (this)
            {
                _stream.Seek(-sizeof(long), SeekOrigin.End); //move to tail long that states size of next change block
                WriteChanges(_binaryWriter);
                _stream.Write(BitConverter.GetBytes(0L), 0, sizeof(long)); //no following changes
                ToWrite.Clear();
                return true;
            }


        }

        private void WriteToStream(IPersistIfcEntity entity)
        {
            lock (this)
            {
                //BinaryWriter entityWriter = new BinaryWriter(_streamReader);
                _stream.Seek(0, SeekOrigin.End);
                long posIndex = _stream.Position;
                //IPersistIfcEntity entity = GetOrCreateEntity(el);
                //var offset = _entityOffsets[el];
                _entityOffsets[entity.EntityLabel].Offset = posIndex;
                _binaryWriter.Write((int)0); // reserve 4 bytes of length of stream
                int len = WriteEntity(_binaryWriter, entity); // write data and get length
                long prevPos = _stream.Position; // record current pos for later
                _stream.Seek(posIndex, SeekOrigin.Begin); // seak the position for the length of stream
                _binaryWriter.Write(len); // write the len and move back to prev position
                _stream.Seek(prevPos, SeekOrigin.Begin);
            }

        }
        /// <summary>
        ///   Imports an Xml file memory model into the model server, throws exception if errors are encountered
        /// </summary>
        /// <param name = "xbimFilename"></param>
        public string ImportXml(string xmlFilename, string xbimFilename)
        {
            try
            {
                Dispose(); //clear up any issues from previous runs

                XmlReaderSettings settings = new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = false };
                using (Stream xmlInStream = new FileStream(xmlFilename, FileMode.Open, FileAccess.Read))
                {

                    _stream = new FileStream(xbimFilename, FileMode.Create, FileAccess.ReadWrite);

                    WriteHeader();

                    _header = new IfcFileHeader();
                    _binaryWriter = new BinaryWriter(_stream);
                    _header.FileName.Name = xmlFilename;
                    _header.Write(_binaryWriter);


                    _entityOffsets = new XbimIndex();
                    _entityTypes = new Dictionary<Type, List<long>>();


                    int errors = 0;

                    //ParserContext pc = new ParserContext();
                    //pc.XmlSpace = "preserve";
                    using (XmlReader xmlReader = XmlReader.Create(xmlInStream, settings))
                    {
                        IfcXmlReader reader = new IfcXmlReader();

                        reader.AppendToStream += WriteToStream;

                        errors = reader.Read(this, xmlReader);
                    }



                    if (errors == 0)
                    {
                        long posIndex = -1;

                        posIndex = _stream.Position;

                        _entityOffsets.Write(_binaryWriter);

                        _stream.Write(BitConverter.GetBytes(0L), 0, sizeof(long));

                        _stream.Seek(0, SeekOrigin.Begin);
                        _stream.Write(BitConverter.GetBytes(posIndex), 0, sizeof(long));
                        _stream.Flush();
                        _stream.Close();
                        _stream = new FileStream(xbimFilename, FileMode.Open, FileAccess.ReadWrite);
                        Initialise();

                        _filename = xbimFilename;
                        return _filename;
                    }
                    else
                    {
                        throw new Exception("xBIM file reading or initialisation errors\n");
                    }
                }
            }
            catch (Exception e)
            {
                Dispose();
                throw new Xbim.Common.Exceptions.XbimException("Failed to import " + xbimFilename, e);
            }

        }

        public override string Open(string inputFileName)
        {
            return Open(inputFileName, null);
        }

        public override string Open(string inputFileName, ReportProgressDelegate progReport)
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

            if (fileType.HasFlag(XbimStorageType.XBIM))
            {
                Open(inputFileName, FileAccess.ReadWrite);
            }
            else if (fileType.HasFlag(XbimStorageType.IFCXML))
            {
                // input to be xml file, output will be xbim file
                ImportXml(inputFileName, outputFileName);
            }
            else if (fileType.HasFlag(XbimStorageType.IFC))
            {
                // input to be ifc file, output will be xbim file
                ImportIfc(inputFileName, outputFileName, progReport);
            }
            else if (fileType.HasFlag(XbimStorageType.IFCZIP))
            {
                // get the ifc file from zip
                string ifcFileName = ExportZippedIfc(inputFileName);
                // convert ifc to xbim
                ImportIfc(ifcFileName, outputFileName);
            }

            return outputFileName;
        }

        public override void Import(string inputFileName)
        {
            throw new NotImplementedException("Import functionality: not implemented yet");
        }

    }

}
