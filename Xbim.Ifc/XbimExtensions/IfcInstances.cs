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
using Xbim.Ifc.Kernel;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions.Transactions.Extensions;

#endregion

namespace Xbim.XbimExtensions
{
    public class IfcMetaProperty
    {
        public PropertyInfo PropertyInfo;
        public IfcAttribute IfcAttribute;
    }


    public class IfcType
    {
        public Type Type;
        public SortedList<int, IfcMetaProperty> IfcProperties = new SortedList<int, IfcMetaProperty>();
        public List<IfcMetaProperty> IfcInverses = new List<IfcMetaProperty>();
        public IfcType IfcSuperType;
        public List<IfcType> IfcSubTypes = new List<IfcType>();
        private List<Type> _nonAbstractSubTypes;
        private PropertyInfo _primaryIndex;
        private List<PropertyInfo> _secondaryIndices;
        private List<IfcMetaProperty> _expressEnumerableProperties;

        public List<IfcMetaProperty> ExpressEnumerableProperties
        {
            get
            {
                if (_expressEnumerableProperties == null)
                {
                    _expressEnumerableProperties = new List<IfcMetaProperty>();
                    foreach (IfcMetaProperty prop in IfcProperties.Values)
                    {
                        if (typeof (ExpressEnumerable).IsAssignableFrom(prop.PropertyInfo.PropertyType))
                            _expressEnumerableProperties.Add(prop);
                    }
                }
                return _expressEnumerableProperties;
            }
        }

        public override string ToString()
        {
            return Type.Name;
        }

        public IList<Type> NonAbstractSubTypes
        {
            get
            {
                if (_nonAbstractSubTypes == null)
                {
                    _nonAbstractSubTypes = new List<Type>();
                    AddNonAbstractTypes(this, _nonAbstractSubTypes);
                }
                return _nonAbstractSubTypes;
            }
        }

        public List<PropertyInfo> SecondaryIndices
        {
            get { return _secondaryIndices; }
            internal set { _secondaryIndices = value; }
        }

        public PropertyInfo PrimaryIndex
        {
            get { return _primaryIndex; }
            internal set { _primaryIndex = value; }
        }

        /// <summary>
        ///   Returns true if there is a primary or secondar index on this class
        /// </summary>
        public bool HasIndex
        {
            get { return _primaryIndex != null || (_secondaryIndices != null && _secondaryIndices.Count > 0); }
        }


        private void AddNonAbstractTypes(IfcType ifcType, List<Type> nonAbstractTypes)
        {
            if (!ifcType.Type.IsAbstract) //this is a concrete type so add it
                nonAbstractTypes.Add(ifcType.Type);
            foreach (IfcType subType in ifcType.IfcSubTypes)
                AddNonAbstractTypes(subType, nonAbstractTypes);
        }
    }

    public class IfcTypeDictionary : KeyedCollection<Type, IfcType>
    {
        protected override Type GetKeyForItem(IfcType item)
        {
            return item.Type;
        }

        public IfcType this[IPersistIfc ent]
        {
            get { return this[ent.GetType()]; }
        }

        public IfcType this[string ifcTypeName]
        {
            get { return IfcInstances.IfcTypeLookup[ifcTypeName]; }
        }

        public IfcType Add(string ifcTypeName)
        {
            IfcType ret = IfcInstances.IfcTypeLookup[ifcTypeName];
            this.Add(ret);
            return ret;
        }
    }

    [Serializable]
    public class InstanceKeyedCollection : KeyedCollection<long, IPersistIfcEntity>
    {
        protected override long GetKeyForItem(IPersistIfcEntity item)
        {
            return item.EntityLabel;
        }
    }


    /// <summary>
    ///   A collection of IPersistIfcEntity instances, optimised for IFC models
    /// </summary>
    [Serializable]
    public class IfcInstances : ICollection<IPersistIfcEntity>
    {
        private readonly Dictionary<Type, ICollection<IPersistIfcEntity>> _instances =
            new Dictionary<Type, ICollection<IPersistIfcEntity>>();

        private readonly InstanceKeyedCollection _entityHandleLookup = new InstanceKeyedCollection();
        private readonly bool _buildIndices = true;
        [NonSerialized] private static IfcTypeDictionary _IfcEntities;

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

        public IfcInstances()
        {
        }

        public IfcInstances(bool buildIndices)
        {
            _buildIndices = buildIndices;
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
                ICollection<IPersistIfcEntity> entities;
                if (_instances.TryGetValue(item, out entities))
                {
                    foreach (IPersistIfcEntity ent in entities)
                    {
                        yield return (TIfcType) ent;
                    }
                }
            }
        }


        public void CopyTo(IfcInstances copyTo, Type type)
        {
            foreach (KeyValuePair<Type, ICollection<IPersistIfcEntity>> item in _instances)
            {
                ICollection<IPersistIfcEntity> entities;
                if (_instances.TryGetValue(type, out entities))
                {
                    foreach (IPersistIfcEntity ent in entities)
                    {
                        copyTo.Add_Reversible(ent);
                    }
                }
            }
        }

        #region IEnumerable<object> Members

        public IEnumerator<IPersistIfcEntity> GetEnumerator()
        {
            IfcInstanceEnumerator enumer = new IfcInstanceEnumerator(_instances);
            return enumer;
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            IfcInstanceEnumerator enumer = new IfcInstanceEnumerator(_instances);
            return enumer;
        }

        #endregion

        #region ICollection<object> Dummy Members

        private void Add_Reversible(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<IPersistIfcEntity> entities;
            if (!_instances.TryGetValue(type, out entities))
            {
                IfcType ifcType = IfcEntities[type];
                if (_buildIndices && ifcType.PrimaryIndex != null)
                    entities = new XbimIndexedCollection<IPersistIfcEntity>(ifcType.SecondaryIndices);
                else
                    entities = new HashSet<IPersistIfcEntity>();
                _instances.Add_Reversible(new KeyValuePair<Type, ICollection<IPersistIfcEntity>>(type, entities));
            }
            entities.Add_Reversible(instance);
            _entityHandleLookup.Add_Reversible(instance);
        }

        public IPersistIfcEntity GetInstance(long label)
        {
            return _entityHandleLookup[label];
        }

        public void AddRaw(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<IPersistIfcEntity> entities;
            if (!_instances.TryGetValue(type, out entities))
            {
                entities = new HashSet<IPersistIfcEntity>();
                _instances.Add(type, entities);
            }
            entities.Add(instance);
            _entityHandleLookup.Add(instance);
        }

        public void Add(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<IPersistIfcEntity> entities;
            if (!_instances.TryGetValue(type, out entities))
            {
                IfcType ifcType = IfcEntities[type];
                if (_buildIndices && ifcType.PrimaryIndex != null)
                    entities = new XbimIndexedCollection<IPersistIfcEntity>(ifcType.SecondaryIndices);
                else
                    entities = new HashSet<IPersistIfcEntity>();
                _instances.Add(type, entities);
            }
            entities.Add(instance);
            _entityHandleLookup.Add(instance);
        }

        public void Clear_Reversible()
        {
            _instances.Clear_Reversible();
            _entityHandleLookup.Clear_Reversible();
        }

        public bool Contains(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<IPersistIfcEntity> entities;
            if (_instances.TryGetValue(type, out entities))
                return entities.Contains(instance);
            else
                return false;
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
                int cnt = 0;
                foreach (ICollection<IPersistIfcEntity> item in _instances.Values)
                {
                    cnt += item.Count;
                }
                return cnt;
            }
        }

        public bool IsReadOnly
        {
            get { return ((IDictionary) _instances).IsReadOnly; }
        }

        private bool Remove_Reversible(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<IPersistIfcEntity> entities;
            if (_instances.TryGetValue(type, out entities))
                return entities.Remove_Reversible(instance);
            else
                return false;
        }

        public bool Remove(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<IPersistIfcEntity> entities;
            if (_instances.TryGetValue(type, out entities))
                return entities.Remove(instance);
            else
                return false;
        }

        #endregion

        public bool TryGetValue(Type key, out ICollection<IPersistIfcEntity> value)
        {
            return _instances.TryGetValue(key, out value);
        }

        #region ICollection<IPersistIfc> Members

        public void Clear()
        {
            _instances.Clear();
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
            get { return _instances.Keys; }
        }

        public ICollection<IPersistIfcEntity> this[Type type]
        {
            get { return _instances[type]; }
        }


        public int BuildIndices(TextWriter errorLog)
        {
            int err = 0;
            foreach (IfcType ifcType in IfcEntities)
            {
                //System.Diagnostics.Debug.WriteLine(ifcType.Type.Name);
                if (!ifcType.Type.IsAbstract && ifcType.HasIndex)
                {
                    ICollection<IPersistIfcEntity> entities;
                    if (_instances.TryGetValue(ifcType.Type, out entities))
                    {
                        try
                        {
                            XbimIndexedCollection<IPersistIfcEntity> index =
                                new XbimIndexedCollection<IPersistIfcEntity>(ifcType.PrimaryIndex,
                                                                             ifcType.SecondaryIndices, entities);
                            _instances.Remove(ifcType.Type);
                            _instances.Add(ifcType.Type, index);
                        }
                        catch (Exception)
                        {
                            err++;
                            PropertyInfo pi = ifcType.PrimaryIndex;
                            errorLog.WriteLine(
                                string.Format(
                                    "{0} is defined as a unique key in {1}, Duplicate values found. Index could not be built",
                                    ifcType.PrimaryIndex.Name, ifcType.Type.Name));
                        }
                    }
                }
            }
            return err;
        }


        public bool ContainsKey(Type key)
        {
            return _instances.ContainsKey(key);
        }
    }

    #region Extensions

    public static class IfcInstancesExtensions
    {
        public static IEnumerable<T> Where<T>(this IfcInstances instances, Expression<Func<T, bool>> expr)
        {
            Type type = typeof (T);
            IfcType ifcType = IfcInstances.IfcEntities[type];
            foreach (Type itemType in ifcType.NonAbstractSubTypes)
            {
                ICollection<IPersistIfcEntity> entities;

                if (instances.TryGetValue(itemType, out entities))
                {
                    bool noIndex = true;
                    XbimIndexedCollection<IPersistIfcEntity> indexColl =
                        entities as XbimIndexedCollection<IPersistIfcEntity>;
                    if (indexColl != null)
                    {
                        //our indexes work from the hash values of that which is indexed, regardless of type
                        object hashRight = null;

                        //indexes only work on equality expressions here
                        if (expr.Body.NodeType == ExpressionType.Equal)
                        {
                            //Equality is a binary expression
                            BinaryExpression binExp = (BinaryExpression) expr.Body;
                            //Get some aliases for either side
                            Expression leftSide = binExp.Left;
                            Expression rightSide = binExp.Right;

                            hashRight = GetRight(leftSide, rightSide);

                            //if we were able to create a hash from the right side (likely)
                            MemberExpression returnedEx = GetIndexablePropertyOnLeft<T>(leftSide);
                            if (returnedEx != null)
                            {
                                //cast to MemberExpression - it allows us to get the property
                                MemberExpression propExp = returnedEx;
                                string property = propExp.Member.Name;
                                if (indexColl.HasIndex(property))
                                {
                                    IEnumerable<IPersistIfcEntity> values = indexColl.GetValues(property, hashRight);
                                    if (values != null)
                                    {
                                        foreach (T item in values.Cast<T>())
                                        {
                                            yield return item;
                                        }
                                        noIndex = false;
                                    }
                                }
                            }
                        }
                        else if (expr.Body.NodeType == ExpressionType.Call)
                        {
                            MethodCallExpression callExp = (MethodCallExpression) expr.Body;
                            if (callExp.Method.Name == "Contains")
                            {
                                Expression keyExpr = callExp.Arguments[0];
                                if (keyExpr.NodeType == ExpressionType.Constant)
                                {
                                    ConstantExpression constExp = (ConstantExpression) keyExpr;
                                    object key = constExp.Value;
                                    if (callExp.Object.NodeType == ExpressionType.MemberAccess)
                                    {
                                        MemberExpression memExp = (MemberExpression) callExp.Object;

                                        string property = memExp.Member.Name;
                                        if (indexColl.HasIndex(property))
                                        {
                                            IEnumerable<IPersistIfcEntity> values = indexColl.GetValues(property, key);
                                            if (values != null)
                                            {
                                                foreach (T item in values.Cast<T>())
                                                {
                                                    yield return item;
                                                }
                                                noIndex = false;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (noIndex)
                    {
                        IEnumerable<T> sourceEnum = entities.Cast<T>();
                        Func<T, bool> predicate = expr.Compile();
                        // IEnumerable<T> result = sourceEnum.Where<T>(expr.Compile());
                        foreach (T resultItem in sourceEnum)
                            if (predicate(resultItem)) yield return resultItem;
                    }
                }
            }
        }


        private static MemberExpression GetIndexablePropertyOnLeft<T>(Expression leftSide)
        {
            MemberExpression mex = leftSide as MemberExpression;
            if (leftSide.NodeType == ExpressionType.Call)
            {
                MethodCallExpression call = leftSide as MethodCallExpression;
                if (call.Method.Name == "CompareString")
                {
                    mex = call.Arguments[0] as MemberExpression;
                }
            }

            return mex;
        }


        private static object GetRight(Expression leftSide, Expression rightSide)
        {
            if (leftSide.NodeType == ExpressionType.Call)
            {
                MethodCallExpression call = leftSide as MethodCallExpression;
                if (call.Method.Name == "CompareString")
                {
                    LambdaExpression evalRight = Expression.Lambda(call.Arguments[1], null);
                    //Compile it, invoke it, and get the resulting hash
                    return (evalRight.Compile().DynamicInvoke(null));
                }
            }
            //rightside is where we get our hash...
            switch (rightSide.NodeType)
            {
                    //shortcut constants, dont eval, will be faster
                case ExpressionType.Constant:
                    ConstantExpression constExp
                        = (ConstantExpression) rightSide;
                    return (constExp.Value);

                    //if not constant (which is provably terminal in a tree), convert back to Lambda and eval to get the hash.
                default:
                    //Lambdas can be created from expressions... yay
                    LambdaExpression evalRight = Expression.Lambda(rightSide, null);
                    //Compile and invoke it, and get the resulting hash
                    return (evalRight.Compile().DynamicInvoke(null));
            }
        }
    }

    #endregion

    #region Helper Class

    internal class IfcInstanceEnumerator : IEnumerator<IPersistIfcEntity>
    {
        private IPersistIfcEntity _current;
        private readonly Dictionary<Type, ICollection<IPersistIfcEntity>> _instances;
        private Dictionary<Type, ICollection<IPersistIfcEntity>>.Enumerator _typeEnumerator;
        private IEnumerator<IPersistIfcEntity> _instanceEnumerator;

        public IfcInstanceEnumerator(Dictionary<Type, ICollection<IPersistIfcEntity>> instances)
        {
            _instances = instances;
            Reset();
        }

        #region IEnumerator<ISupportIfcParser> Members

        public object Current
        {
            get { return _current; }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        #region IEnumerator Members

        public bool MoveNext()
        {
            if (_instanceEnumerator != null && _instanceEnumerator.MoveNext()) //can we get an instance
            {
                _current = _instanceEnumerator.Current;
                return true;
            }

            while (_typeEnumerator.MoveNext()) //we can get a type collection and see if it has any instances
            {
                _instanceEnumerator = _typeEnumerator.Current.Value.GetEnumerator();
                if (_instanceEnumerator != null && _instanceEnumerator.MoveNext()) //can we get an instance
                {
                    _current = _instanceEnumerator.Current;
                    return true;
                }
            }
            return false;
        }

        public void Reset()
        {
            _current = null;
            _typeEnumerator = _instances.GetEnumerator();
            _instanceEnumerator = null;
        }

        #endregion

        #region IEnumerator<ISupportIfcParser> Members

        IPersistIfcEntity IEnumerator<IPersistIfcEntity>.Current
        {
            get { return _current; }
        }

        #endregion
    }

    #endregion
}