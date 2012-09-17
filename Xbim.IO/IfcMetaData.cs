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
using Xbim.XbimExtensions.SelectTypes;
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
    public class IfcMetaData
    {
        /// <summary>
        /// Look up the numeric id of an Ifc Entity and return the string name in upper case
        /// </summary>
        private static Dictionary<short, string> TypeIdToTypeNameLookup = new Dictionary<short, string>();
       /// <summary>
       /// Look up for the if of an Ifc entity that returns the IfcType
       /// </summary>
        private static Dictionary<short, IfcType> TypeIdToIfcTypeLookup = new Dictionary<short, IfcType>();
        /// <summary>
        /// Look up the entity Type and return the IfcType
        /// </summary>
        private static IfcTypeDictionary TypeToIfcTypeLookup;
        /// <summary>
        /// Look up the name of an ifc entity and return the IfcType
        /// </summary>
        private static Dictionary<string, IfcType> TypeNameToIfcTypeLookup;

        static IfcMetaData()
        {
            Module ifcModule = typeof (IfcActor).Module;
            IEnumerable<Type> types =
                ifcModule.GetTypes().Where(
                    t =>
                    typeof (IPersistIfc).IsAssignableFrom(t) && t != typeof (IPersistIfc) && !t.IsEnum && !t.IsAbstract &&
                    t.IsPublic && !typeof (ExpressHeaderType).IsAssignableFrom(t));

            TypeNameToIfcTypeLookup = new Dictionary<string, IfcType>(types.Count());
            TypeToIfcTypeLookup = new IfcTypeDictionary();
            try
            {
                short typeId = 0;
                foreach (Type type in types)
                {
                    IfcType ifcType;

                    if (TypeToIfcTypeLookup.Contains(type))
                        ifcType = TypeToIfcTypeLookup[type];
                    else
                        ifcType = new IfcType {Type = type};

                    string typeLookup = type.Name.ToUpper();
                    if (!TypeNameToIfcTypeLookup.ContainsKey(typeLookup))
                    {
                        TypeNameToIfcTypeLookup.Add(typeLookup, ifcType);
                       
                    }
                    
                    if (!TypeToIfcTypeLookup.Contains(ifcType))
                    {
                        TypeToIfcTypeLookup.Add(ifcType);
                        AddParent(ifcType);
                        AddProperties(ifcType);
                    }
                }

                foreach (var item in TypeNameToIfcTypeLookup)
                {
                   
                        TypeIdToTypeNameLookup.Add(++typeId, item.Key);
                }

                //add the Type Ids to each of the IfcTypes
                foreach (var item in TypeIdToTypeNameLookup)
                {
                     
                    IfcType ifcType = TypeNameToIfcTypeLookup[item.Value];
                    if(item.Value!="IFCCARTESIANPOINT")
                        ifcType.TypeId = item.Key;
                    TypeIdToIfcTypeLookup.Add(item.Key, ifcType);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error reading Ifc Entity Meta Data", e);
            }
        }
       
        internal static void AddProperties(IfcType ifcType)
        {
            PropertyInfo[] properties =
                ifcType.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            foreach (PropertyInfo propInfo in properties)
            {
                int attributeIdx = -1;
                IfcAttribute[] ifcAttributes =
                    (IfcAttribute[]) propInfo.GetCustomAttributes(typeof (IfcAttribute), false);
                if (ifcAttributes.GetLength(0) > 0) //we have an ifc property
                {
                    if (ifcAttributes[0].Order > 0)
                    {
                        ifcType.IfcProperties.Add(ifcAttributes[0].Order,
                                                  new IfcMetaProperty { PropertyInfo = propInfo, IfcAttribute = ifcAttributes[0] });
                        attributeIdx = ifcAttributes[0].Order;
                    }

                    else
                        ifcType.IfcInverses.Add(new IfcMetaProperty { PropertyInfo = propInfo, IfcAttribute = ifcAttributes[0] });
                }
                IfcIndex[] ifcPrimaryIndices =
                    (IfcIndex[]) propInfo.GetCustomAttributes(typeof (IfcIndex), false);
                if (ifcPrimaryIndices.GetLength(0) > 0) //we have an ifc primary index
                {
                    ifcType.PrimaryIndex = propInfo;
                    ifcType.PrimaryKeyIndex = attributeIdx;
                }
                IfcSecondaryIndex[] ifcSecondaryIndices =
                    (IfcSecondaryIndex[]) propInfo.GetCustomAttributes(typeof (IfcSecondaryIndex), false);
                if (ifcSecondaryIndices.GetLength(0) > 0) //we have an ifc primary index
                {
                    if (ifcType.SecondaryIndices == null) ifcType.SecondaryIndices = new List<PropertyInfo>();
                    ifcType.SecondaryIndices.Add(propInfo);
                }
            }
        }

       
        internal static void AddParent(IfcType child)
        {
            Type baseParent = child.Type.BaseType;
            if (typeof (object) == baseParent || typeof (ValueType) == baseParent)
                return;
            IfcType ifcParent;
            if (!TypeToIfcTypeLookup.Contains(baseParent))
            {
                TypeToIfcTypeLookup.Add(ifcParent = new IfcType {Type = baseParent});
                string typeLookup = baseParent.Name.ToUpper();
                if (!TypeNameToIfcTypeLookup.ContainsKey(typeLookup))
                    TypeNameToIfcTypeLookup.Add(typeLookup, ifcParent);
                ifcParent.IfcSubTypes.Add(child);
                child.IfcSuperType = ifcParent;
                AddParent(ifcParent);
                AddProperties(ifcParent);
            }
            else
            {
                ifcParent = TypeToIfcTypeLookup[baseParent];
                child.IfcSuperType = ifcParent;
                if (!ifcParent.IfcSubTypes.Contains(child))
                    ifcParent.IfcSubTypes.Add(child);
            }
        }

        /// <summary>
        /// Returns the IfcType witht he specified name
        /// </summary>
        /// <param name="typeName">The name of the type in uppercase</param>
        /// <returns></returns>
        public static IfcType IfcType(string typeName)
        {
            return TypeNameToIfcTypeLookup[typeName];
        }

        /// <summary>
        /// returns the IfcType corresponding to the TypeId
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        public static IfcType IfcType(short typeId)
        {
            return TypeIdToIfcTypeLookup[typeId];
        }

        /// <summary>
        /// returns the ifc type id of the type, if the type is not an ifc entity and excpetion will be thrown
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static short? IfcTypeId(Type type)
        {
            return TypeToIfcTypeLookup[type].TypeId;
        }
        /// <summary>
        /// Returns the ifc typeId for the named type
        /// </summary>
        /// <param name="typeName">the name of the type, this is in uppercase</param>
        /// <returns></returns>
        public static short? IfcTypeId(string typeName)
        {
            return TypeNameToIfcTypeLookup[typeName].TypeId;
        }

        public static short? IfcTypeId(IPersistIfc entity)
        {
            return TypeToIfcTypeLookup[entity.GetType()].TypeId;
        }
        /// <summary>
        /// Returns the Type of the Ifc Entity with typeId
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        public static Type GetType(short typeId)
        {
            return IfcType(typeId).Type;
        }

        /// <summary>
        /// Returns the IfcType of the specified entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static IfcType IfcType(IPersistIfc entity)
        {
            return TypeToIfcTypeLookup[entity.GetType()];
        }

        /// <summary>
        /// Returns the IfcType of the specified Type
        /// </summary>
        /// <param name="theType"></param>
        /// <returns></returns>
        internal static IfcType IfcType(Type theType)
        {
            return TypeToIfcTypeLookup[theType];
        }

        /// <summary>
        /// Trys to get the specified Ifc Type with the typeName, if the ifcType does not exist false is returned
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="ifcType"></param>
        /// <returns></returns>
        public static bool TryGetIfcType(string typeName, out IfcType ifcType)
        {
            return TypeNameToIfcTypeLookup.TryGetValue(typeName,out ifcType);
        }
    }
}