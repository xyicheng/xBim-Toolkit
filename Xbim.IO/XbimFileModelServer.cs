﻿#region XbimHeader

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
using Xbim.Ifc2x3.GeometricConstraintResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.Ifc2x3.PresentationOrganizationResource;
using Xbim.Ifc2x3.PresentationResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Transactions;
using Xbim.XbimExtensions.Transactions.Extensions;
using System.Xml;
using System.Windows.Markup;
using Xbim.IO.Parser;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.IO
{


    public class XbimFileModelServer : XbimModelServer
    {
        private BinaryReader _binaryReader;
        
        
        private string _filename;
       
        private BinaryWriter _binaryWriter;
        private Stream _stream;

       

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
                Header.FileName = header.FileName;
                Header.FileDescription = header.FileDescription;
                Header.FileSchema = header.FileSchema;
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
            FileStream inputFile = null;
            IfcInputStream input = null;
            try
            {
                Dispose(); //clear up any issues from previous runs
                inputFile = new FileStream(filename, FileMode.Open, FileAccess.Read);
                //attach it to the Ifc Stream Parser
                input = new IfcInputStream(inputFile);
                using ( FileStream outputFile = new FileStream(xbimFilename, FileMode.Create, FileAccess.ReadWrite)                )
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
            catch (Exception e)
            {
                Dispose();
                throw new Xbim.Common.Exceptions.XbimException("Failed to import " + filename + "\n" + input.ErrorLog.ToString(), e);
            }
            finally
            {
                if (inputFile != null) inputFile.Close();
                
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
                    XbimFileModelServer semModel = new XbimFileModelServer(semanticFilename);
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
                    semModel.Close();
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
            header.Write(semanticBinaryWriter);

            //release anything that is in memory
            instances.DropAll();

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
            header.Write(semanticBinaryWriter);
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

            header = new IfcFileHeader();
            header.Read(_binaryReader);

            _binaryReader.BaseStream.Seek(start, SeekOrigin.Begin);
            long entityCount = _binaryReader.ReadInt64();
            long highestLabel = _binaryReader.ReadInt64();

            instances = new IfcInstances(this, false);
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
                long label = _binaryReader.ReadInt64();
                long offset = _binaryReader.ReadInt64();
                Int16 classIndex = _binaryReader.ReadInt16();

                instances.Add(new XbimInstanceHandle(label, classIndices[classIndex], offset));
            }

            //_entityTypes = new Dictionary<Type, List<long>>(entityTypeCount);
            //foreach (var item in _entityOffsets)
            //{
            //    List<long> labels;
            //    if (!_entityTypes.TryGetValue(item.Type, out labels))
            //    {
            //        labels = new List<long>();
            //        _entityTypes.Add(item.Type, labels);
            //    }
            //    labels.Add(item.EntityLabel);
            //}
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

       

        protected override IPersistIfcEntity GetOrCreateEntity(long label)
        {
            long fileOffset;
            return instances.GetOrCreateEntity(this, label, out fileOffset);
        }

        protected override void ActivateEntity(long offset, IPersistIfcEntity entity)
        {
           //if less than 1 it is not written to the file
            if (offset > 0)
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
                    MemoryStream ms = new MemoryStream(bContent);
                    BinaryReader br = new BinaryReader(ms);
                    PopulateProperties(entity, br);

                }
            }

        }


        public override void Dispose()
        {
            if (_binaryReader != null) _binaryReader.Close();
            _binaryReader = null;

            if (_stream != null) _stream.Close();
            _stream = null;
             
            _filename = null;
            ToWrite.Clear();
            undoRedoSession = null;
            instances = null;
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
            WriteEntity(entityWriter, item);
            int len = Convert.ToInt32(entityStream.Position);
            entityWriter.Seek(0, SeekOrigin.Begin);
            entityWriter.Write(len);
            entityWriter.Seek(0, SeekOrigin.Begin);
            return len;
        }

       


        public override IPersistIfcEntity GetInstance(long entityLabel)
        {
            long fileOffset;
            IPersistIfcEntity entity = instances.GetOrCreateEntity(this, entityLabel, out fileOffset);
            ActivateEntity(fileOffset, entity);
            return entity;
        }

        public byte[] GetEntityBinaryData(IPersistIfcEntity entity)
        {
            if (ToWrite.Contains(entity)) //we have changed it in memory but not written to cache yet
            {
                MemoryStream entityStream = new MemoryStream(4096);
                BinaryWriter entityWriter = new BinaryWriter(entityStream);
                int len = WriteEntityToSteam(entityStream, entityWriter, entity) - sizeof(Int32);
                byte[] buffer = new byte[len];
                entityStream.Seek(sizeof(Int32), SeekOrigin.Begin);
                entityStream.Read(buffer, 0, len);
                return buffer;
            }
            else
            {
                long fileOffset = instances.GetFileOffset(entity.EntityLabel);
                BinaryReader br = _binaryReader;
                br.BaseStream.Seek(fileOffset, SeekOrigin.Begin);
                int len = br.ReadInt32();
                return br.ReadBytes(len);
            }
        }

        public void Activate(IPersistIfcEntity entity, byte[] binaryData)
        {
            PopulateProperties(entity, new BinaryReader(new MemoryStream(binaryData)));
            entity.Bind(this, Math.Abs(entity.EntityLabel));
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

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override void Close()
        {

            if (_stream != null) _stream.Close();
        }

       

        public override void WriteChanges(BinaryWriter dataStream)
        {

            UndoRedo.Exit();
            MemoryStream entityStream = new MemoryStream(Int16.MaxValue);
            BinaryWriter entityWriter = new BinaryWriter(entityStream);
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
                    MemoryStream ms = new MemoryStream(bContent);
                    BinaryReader br = new BinaryReader(ms);
                    PopulateProperties(item.Entity, br);
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
                Stream xmlInStream = new FileStream(xmlFilename, FileMode.Open, FileAccess.Read);

                _stream = new FileStream(xbimFilename, FileMode.Create, FileAccess.ReadWrite);

                WriteHeader();

                header = new IfcFileHeader();
                _binaryWriter = new BinaryWriter(_stream);
                header.FileName.Name = xmlFilename;
                header.Write(_binaryWriter);


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
            catch (Exception e)
            {
                Dispose();
                throw new Xbim.Common.Exceptions.XbimException("Failed to import " + xbimFilename, e);
            }
            finally
            {
                
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
