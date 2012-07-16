#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcInstances.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.SelectTypes;
using Xbim.XbimExtensions.Transactions.Extensions;
using Xbim.XbimExtensions.Interfaces;
using Xbim.XbimExtensions;
using Xbim.Common.Logging;
using Xbim.XbimExtensions.Transactions;

#endregion

namespace Xbim.IO
{
    public class IfcMetaProperty
    {
        public PropertyInfo PropertyInfo;
        public IfcAttribute IfcAttribute;
    }


   
  


    /// <summary>
    ///   A collection of IPersistIfcEntity instances, optimised for IFC models
    /// </summary>
    [Serializable]
    public class IfcInstances : ICollection<IPersistIfcEntity>, IEnumerable<long>
    {
        private readonly ILogger Logger = LoggerFactory.GetLogger();
        private readonly Dictionary<Type, ICollection<long>> _typeLookup = new Dictionary<Type, ICollection<long>>();
        private IfcInstanceKeyedCollection _entityHandleLookup = new IfcInstanceKeyedCollection();
        private  bool _buildIndices = true;
        private readonly IModel _model;
        private long _highestLabel;

        private long NextLabel()
        {  
            return _highestLabel+1;
        }


        [NonSerialized] 
        private static IfcTypeDictionary _IfcEntities;

        public static IfcTypeDictionary IfcEntities
        {
            get { return _IfcEntities; }
            set { _IfcEntities = value; }
        }

        static IfcInstances()
        {
            Module ifcModule = typeof (IfcActor).Module;
            IEnumerable<Type> types =
                ifcModule.GetTypes().Where(
                    t =>
                    typeof (IPersistIfc).IsAssignableFrom(t) && t != typeof (IPersistIfc) && !t.IsEnum && !t.IsAbstract &&
                    t.IsPublic && !typeof (ExpressHeaderType).IsAssignableFrom(t));

            _IfcTypeLookup = new Dictionary<string, IfcType>(types.Count());
            _IfcEntities = new IfcTypeDictionary();

            try
            {
                foreach (Type type in types)
                {
                    IfcType ifcType;

                    if (_IfcEntities.Contains(type))
                        ifcType = _IfcEntities[type];
                    else
                        ifcType = new IfcType {Type = type};

                    string typeLookup = type.Name.ToUpper();
                    if (!_IfcTypeLookup.ContainsKey(typeLookup))
                    {
                        _IfcTypeLookup.Add(typeLookup, ifcType);
                    }
                    if (!_IfcEntities.Contains(ifcType))
                    {
                        _IfcEntities.Add(ifcType);
                        AddParent(ifcType);
                        AddProperties(ifcType);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error reading Ifc Entity Meta Data", e);
            }
        }

        private static Dictionary<string, IfcType> _IfcTypeLookup;

        public static Dictionary<string, IfcType> IfcTypeLookup
        {
            get { return _IfcTypeLookup; }
            set { _IfcTypeLookup = value; }
        }


        internal static void AddProperties(IfcType ifcType)
        {
            PropertyInfo[] properties =
                ifcType.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            foreach (PropertyInfo propInfo in properties)
            {
                IfcAttribute[] ifcAttributes =
                    (IfcAttribute[]) propInfo.GetCustomAttributes(typeof (IfcAttribute), false);
                if (ifcAttributes.GetLength(0) > 0) //we have an ifc property
                {
                    if (ifcAttributes[0].Order > 0)
                        ifcType.IfcProperties.Add(ifcAttributes[0].Order,
                                                  new IfcMetaProperty
                                                      {PropertyInfo = propInfo, IfcAttribute = ifcAttributes[0]});
                    else
                        ifcType.IfcInverses.Add(new IfcMetaProperty
                                                    {PropertyInfo = propInfo, IfcAttribute = ifcAttributes[0]});
                }
                IfcPrimaryIndex[] ifcPrimaryIndices =
                    (IfcPrimaryIndex[]) propInfo.GetCustomAttributes(typeof (IfcPrimaryIndex), false);
                if (ifcPrimaryIndices.GetLength(0) > 0) //we have an ifc primary index
                    ifcType.PrimaryIndex = propInfo;
                IfcSecondaryIndex[] ifcSecondaryIndices =
                    (IfcSecondaryIndex[]) propInfo.GetCustomAttributes(typeof (IfcSecondaryIndex), false);
                if (ifcSecondaryIndices.GetLength(0) > 0) //we have an ifc primary index
                {
                    if (ifcType.SecondaryIndices == null) ifcType.SecondaryIndices = new List<PropertyInfo>();
                    ifcType.SecondaryIndices.Add(propInfo);
                }
            }
        }

        public static void GenerateSchema(TextWriter res)
        {
            IndentedTextWriter iw = new IndentedTextWriter(res);
            foreach (IfcType ifcType in IfcEntities)
            {
                iw.WriteLine(string.Format("ENTITY Ifc{0}", ifcType.Type.Name));
                if (ifcType.IfcSuperType != null)
                {
                    iw.Indent++;
                    iw.WriteLine(string.Format("SUBTYPE OF ({0})", ifcType.IfcSuperType.Type.Name));
                }
                if (ifcType.IfcProperties.Count > 0)
                {
                    iw.Indent++;
                    foreach (IfcMetaProperty prop in ifcType.IfcProperties.Values)
                    {
                        iw.WriteLine(string.Format("{0}\t: {1};", prop.PropertyInfo.Name,
                                                   prop.PropertyInfo.PropertyType.Name));
                    }
                    iw.Indent--;
                }
                if (ifcType.IfcInverses.Count > 0)
                {
                    iw.WriteLine("INVERSE");
                    iw.Indent++;
                    foreach (IfcMetaProperty prop in ifcType.IfcInverses)
                    {
                        iw.Write(string.Format("{0}\t: ", prop.PropertyInfo.Name));
                        int min = prop.IfcAttribute.MinCardinality;
                        int max = prop.IfcAttribute.MaxCardinality;
                        switch (prop.IfcAttribute.IfcType)
                        {
                            case IfcAttributeType.Set:
                                iw.WriteLine(string.Format("SET OF {0}:{1}", min > -1 ? "[" + min : "",
                                                           min > -1 ? max > 0 ? max + "] " : "?" + "] " : ""));
                                break;
                            case IfcAttributeType.List:
                            case IfcAttributeType.ListUnique:
                                iw.WriteLine(string.Format("LIST OF {0}:{1}", min > -1 ? "[" + min : "",
                                                           min > -1 ? max > 0 ? max + "] " : "?" + "] " : ""));
                                break;
                            default:
                                break;
                        }
                    }
                    iw.Indent--;
                }
                iw.Indent--;
                iw.WriteLine("END_ENTITY");
            }
        }

        internal static void AddParent(IfcType child)
        {
            Type baseParent = child.Type.BaseType;
            if (typeof (object) == baseParent || typeof (ValueType) == baseParent)
                return;
            IfcType ifcParent;
            if (!IfcEntities.Contains(baseParent))
            {
                IfcEntities.Add(ifcParent = new IfcType {Type = baseParent});
                string typeLookup = baseParent.Name.ToUpper();
                if (!IfcTypeLookup.ContainsKey(typeLookup))
                    IfcTypeLookup.Add(typeLookup, ifcParent);
                ifcParent.IfcSubTypes.Add(child);
                child.IfcSuperType = ifcParent;
                AddParent(ifcParent);
                AddProperties(ifcParent);
            }
            else
            {
                ifcParent = IfcEntities[baseParent];
                child.IfcSuperType = ifcParent;
                if (!ifcParent.IfcSubTypes.Contains(child))
                    ifcParent.IfcSubTypes.Add(child);
            }
        }

        public IfcInstances(IModel model)
        {
            _model = model;
        }

        public IfcInstances(IModel model, bool buildIndices)
        {
            _buildIndices = buildIndices;
            _model = model;
        }

        public override string ToString()
        {
            return string.Format("Count = {0}", Count);
        }

        public IEnumerable<TIfcType> OfType<TIfcType>() where TIfcType : IPersistIfcEntity
        {
            Type type = typeof (TIfcType);
            IfcType ifcType = IfcEntities[type];
            foreach (Type item in ifcType.NonAbstractSubTypes)
            {
                ICollection<long> entities;
                if (_typeLookup.TryGetValue(item, out entities))
                {
                    foreach (long label in entities)
                    {
                        yield return (TIfcType)_entityHandleLookup.GetEntity(label);
                    }
                }
            }
        }


        public void CopyTo(IfcInstances copyTo, Type type)
        {
            foreach (KeyValuePair<Type, ICollection<long>> label in _typeLookup)
            {
                ICollection<long> entities;
                if (_typeLookup.TryGetValue(type, out entities))
                {
                    foreach (long entLabel in entities)
                    {
                        copyTo.Add_Reversible(_entityHandleLookup.GetEntity(entLabel));
                    }
                }
            }
        }

        #region IEnumerable<object> Members

       

        #endregion

        #region IEnumerable Members

       

        #endregion

        #region ICollection<object> Dummy Members

        private void Add_Reversible(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<long> entities;
            if (!_typeLookup.TryGetValue(type, out entities))
            {
                IfcType ifcType = IfcEntities[type];
                if (_buildIndices && ifcType.PrimaryIndex != null)
                    entities = new XbimIndexedCollection<long>(ifcType.SecondaryIndices);
                else
                    entities = new List<long>();
                _typeLookup.Add_Reversible(new KeyValuePair<Type, ICollection<long>>(type, entities));
            }
            entities.Add_Reversible(instance.EntityLabel);
            _entityHandleLookup.Add_Reversible(instance);
        }

        public IPersistIfcEntity GetInstance(long label)
        {
            return _entityHandleLookup.GetEntity(label);
        }

        public bool ContainsInstance(long label)
        {
            return _entityHandleLookup.Keys.Contains(label);
        }

        internal void AddRaw(IPersistIfcEntity instance)
        {
            AddRawTypeReference(instance);
            _entityHandleLookup.Add(instance);
        }

        private void AddRawTypeReference(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<long> entities;
            if (!_typeLookup.TryGetValue(type, out entities))
            {
                entities = new List<long>();
                _typeLookup.Add_Reversible(type, entities);
            }
            entities.Add_Reversible(instance.EntityLabel);
        }
        
        internal IPersistIfcEntity AddNew(XbimModel xbimModel, Type ifcType, long label)
        {
            Transaction txn = Transaction.Current;
            if (txn != null)
                Transaction.AddPropertyChange<long>(h => _highestLabel = h, Math.Max(label, _highestLabel), label);
            IPersistIfcEntity ent = _entityHandleLookup.CreateEntity(xbimModel, ifcType, label);
            _highestLabel = Math.Max(label, _highestLabel);
            AddTypeReference(ent);
            return ent;
        }
        
        internal IPersistIfcEntity AddNew(IModel xbimModel, Type ifcType)
        {
            long label = NextLabel();
            Transaction txn = Transaction.Current;
            if (txn != null)
                Transaction.AddPropertyChange<long>(h => _highestLabel = h, _highestLabel, label);
            IPersistIfcEntity ent = _entityHandleLookup.CreateEntity(xbimModel, ifcType, label);
            _highestLabel = label;
            AddTypeReference(ent);
            return ent;
        }

        public void Add(IPersistIfcEntity instance)
        {
            AddTypeReference(instance);
            _entityHandleLookup.Add(instance);
            
        }

        private void AddTypeReference(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<long> entities;
            if (!_typeLookup.TryGetValue(type, out entities))
            {
                IfcType ifcType = IfcEntities[type];
                if (_buildIndices && ifcType.PrimaryIndex != null)
                    entities = new XbimIndexedCollection<long>(ifcType.SecondaryIndices);
                else
                    entities = new List<long>();
                _typeLookup.Add(type, entities);
            }
            entities.Add(instance.EntityLabel);
        }

        public void Clear_Reversible()
        {
            _typeLookup.Clear_Reversible();
            _entityHandleLookup.Clear_Reversible();
        }

        public bool Contains(IPersistIfcEntity instance)
        {
            return _entityHandleLookup.Contains(instance.EntityLabel);
        }

        /// <summary>
        ///   Copies all instances of the specified type into copyTo. This is a reversable action
        /// </summary>
        /// <param name = "copyTo"></param>
        public void CopyTo<TIfcType>(IfcInstances copyTo) where TIfcType : IPersistIfcEntity
        {
            foreach (TIfcType item in this.OfType<TIfcType>())
                copyTo.Add_Reversible(item);
        }

        public int Count
        {
            get
            {
                return _entityHandleLookup.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return ((IDictionary) _typeLookup).IsReadOnly; }
        }

        private bool Remove_Reversible(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<long> entities;
            if (_typeLookup.TryGetValue(type, out entities))
                entities.Remove_Reversible(instance.EntityLabel);
            return _entityHandleLookup.Remove_Reversible(instance.EntityLabel);
        }

        public bool Remove(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<long> entities;
            if (_typeLookup.TryGetValue(type, out entities))
                 entities.Remove(instance.EntityLabel);
            return _entityHandleLookup.Remove(instance.EntityLabel);
        }

        #endregion

        public bool TryGetValue(Type key, out ICollection<long> value)
        {
            return _typeLookup.TryGetValue(key, out value);
        }

        #region ICollection<IPersistIfc> Members

        public void Clear()
        {
            _typeLookup.Clear();
            _entityHandleLookup.Clear();
        }

        #endregion

        #region ICollection<IPersistIfc> Members

        public void CopyTo(IPersistIfcEntity[] array, int arrayIndex)
        {
            int i = arrayIndex;
            foreach (IPersistIfcEntity item in this)
            {
                array[i] = item;
                i++;
            }
        }

        #endregion

        public ICollection<Type> Types
        {
            get { return _typeLookup.Keys; }
        }

        public ICollection<long> this[Type type]
        {
            get { return _typeLookup[type]; }
        }


        public int BuildIndices()
        {
            int err = 0;
            foreach (IfcType ifcType in IfcEntities)
            {
                
                if (!ifcType.Type.IsAbstract && ifcType.HasIndex)
                {
                    ICollection<long> entities;
                    if (_typeLookup.TryGetValue(ifcType.Type, out entities))
                    {
                        try
                        {
                            XbimIndexedCollection<long> index =
                                new XbimIndexedCollection<long>(ifcType.PrimaryIndex,
                                                                             ifcType.SecondaryIndices, entities);
                            _typeLookup.Remove(ifcType.Type);
                            _typeLookup.Add(ifcType.Type, index);
                        }
                        catch (Exception)
                        {
                            err++;
                            PropertyInfo pi = ifcType.PrimaryIndex;
                            Logger.WarnFormat("{0} is defined as a unique key in {1}, Duplicate values found. Index could not be built",
                                    ifcType.PrimaryIndex.Name, ifcType.Type.Name);
                        }
                    }
                }
            }
            return err;
        }


        public bool ContainsKey(Type key)
        {
            return _typeLookup.ContainsKey(key);
        }

        internal void Add(XbimInstanceHandle xbimInstanceHandle)
        {
            Type type = xbimInstanceHandle.EntityType;
            ICollection<long> entities;
            if (!_typeLookup.TryGetValue(type, out entities))
            {
                IfcType ifcType = IfcEntities[type];
                if (_buildIndices && ifcType.PrimaryIndex != null)
                    entities = new XbimIndexedCollection<long>(ifcType.SecondaryIndices);
                else
                    entities = new List<long>();
                _typeLookup.Add(type, entities);
            }
            entities.Add(xbimInstanceHandle.EntityLabel);
            _entityHandleLookup.Add(xbimInstanceHandle.EntityLabel, xbimInstanceHandle);

        }

        internal void DropAll()
        {
            IfcInstanceKeyedCollection newEntityHandleLookup = new IfcInstanceKeyedCollection();
            foreach (IXbimInstance item in _entityHandleLookup.Values)
            {
                newEntityHandleLookup.Add(item.EntityLabel, new XbimInstanceHandle(item));
            }
            _entityHandleLookup = newEntityHandleLookup;
        }

        internal IPersistIfcEntity GetOrCreateEntity(IModel model, long label, out long fileOffset)
        {
            return _entityHandleLookup.GetOrCreateEntity(model, label, out fileOffset);
        }

        internal bool Contains(long entityLabel)
        {
            return _entityHandleLookup.Contains(entityLabel);
        }

        public IEnumerator<IPersistIfcEntity> GetEnumerator()
        {
            return _entityHandleLookup.GetEnumerator(_model);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _entityHandleLookup.GetEnumerator(_model);
        }

        internal long GetFileOffset(long label)
        {
            return _entityHandleLookup.GetFileOffset(label);
        }

        internal long InstancesOfTypeCount(Type type)
        {
            ICollection<long> entities;
            if (_typeLookup.TryGetValue(type, out entities))
                return entities.Count;
            else
                return 0;
        }

        IEnumerator<long> IEnumerable<long>.GetEnumerator()
        {
            return _entityHandleLookup.Keys.GetEnumerator();
        }
    }

   
}