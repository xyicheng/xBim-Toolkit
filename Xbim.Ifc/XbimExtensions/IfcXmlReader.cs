#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcXmlReader.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Xbim.Ifc.SelectTypes;
using System.Text.RegularExpressions;
using Xbim.XbimExtensions.Parser;
using Xbim.Ifc.MeasureResource;
using System.Globalization;

#endregion

namespace Xbim.XbimExtensions
{
    public class IfcXmlReader
    {
        private static readonly Dictionary<string, Type> primitives;
        private readonly Dictionary<long, IPersistIfcEntity> _instances = new Dictionary<long, IPersistIfcEntity>();

        static IfcXmlReader()
        {
            primitives = new Dictionary<string, Type>();
            primitives.Add("ex:double-wrapper", typeof (double));
            primitives.Add("ex:long-wrapper", typeof (long));
            primitives.Add("ex:string-wrapper", typeof (string));
            primitives.Add("ex:integer-wrapper", typeof (int));
            primitives.Add("ex:boolean-wrapper", typeof (bool));
            primitives.Add("ex:logical-wrapper", typeof (bool?));
            primitives.Add("ex:decimal-wrapper", typeof (double));
            primitives.Add("ex:hexBinary-wrapper", typeof (int));
            primitives.Add("ex:base64Binary-wrapper", typeof (int));
        }

        private abstract class XmlNode
        {
            public readonly XmlNode Parent;
            public int? Position;

            public XmlNode(XmlNode parent)
            {
                Parent = parent;
            }

            public abstract object Value { get; set; }
        }

        private class XmlEntity : XmlNode
        {
            public IPersistIfcEntity Entity;

            public XmlEntity(XmlNode parent, IPersistIfcEntity ent)
                : base(parent)
            {
                Entity = ent;
            }

            public override object Value
            {
                get { return Entity; }
                set { Entity = (IPersistIfcEntity)value; }
            }
        }

        private class XmlValueType : XmlNode
        {
            private object _value;

            public override object Value
            {
                get { return _value; }
                set { _value = value; }
            }

            public readonly Type Type;

            public XmlValueType(XmlNode parent, Type type)
                : base(parent)
            {
                Type = type;
            }
        }

        private class XmlPropertyValue : XmlNode
        {
            public readonly PropertyInfo Property;

            public XmlPropertyValue(XmlNode parent, PropertyInfo prop)
                : base(parent)
            {
                Property = prop;
            }


            public override object Value
            {
                get { return Property.GetValue(((XmlEntity) Parent).Entity, null); }
                set { Property.SetValue(((XmlEntity) Parent).Entity, value, null); }
            }
        }

        public enum CollectionType
        {
            List,
            ListUnique,
            Set
        }

        private class XmlPropertyValueCollection : XmlNode
        {
            public override object Value
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public XmlPropertyValueCollection(XmlNode parent, PropertyInfo prop)
                : base(parent)
            {
                Property = prop;
            }

            public readonly List<XmlNode> Entities = new List<XmlNode>();
            public readonly PropertyInfo Property;
            public CollectionType CType = CollectionType.Set;

            private static int CompareNodes(XmlNode a, XmlNode b)
            {
                if (a.Position > b.Position)
                    return 1;
                else if (a.Position < b.Position)
                    return -1;
                else
                    return 0;
            }

            internal void SetCollection()
            {
                try
                {
                    if (Parent == null) //we are at UOS and there is no more to do
                        return;
                    object val = Property.GetValue(((XmlEntity) Parent).Entity, null);
                    Type pt = Property.PropertyType;
                    if (pt.IsGenericType && pt.GetGenericTypeDefinition().Equals(typeof (Nullable<>)) &&
                        typeof (ExpressComplexType).IsAssignableFrom(Nullable.GetUnderlyingType(pt)))
                        // We are dealing with a generic type that is nullable get underlying type    
                    {
                        val = Activator.CreateInstance(Nullable.GetUnderlyingType(pt), null);
                        Property.SetValue(((XmlEntity) Parent).Entity, val, null);
                        Entities.Sort(CompareNodes);
                        foreach (XmlNode item in Entities)
                            if (item.Value != null) ((ExpressComplexType) val).Add(item.Value);
                    }
                    else
                    {
                        if (val == null) //we need to add to a list but one does not exist, create it
                        {
                            object[] param = new object[1];
                            param[0] = ((XmlEntity)Parent).Entity;

                            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                            CultureInfo culture = null; // use InvariantCulture or other if you prefer
                            val = Activator.CreateInstance(pt, flags, null, param, culture);

                            
                            //val = Activator.CreateInstance(pt, param);
                            Property.SetValue(((XmlEntity) Parent).Entity, val, null);

                            
                        }
                        if (val is ExpressEnumerable)
                        {
                            switch (CType)
                            {
                                case CollectionType.List:
                                case CollectionType.ListUnique:
                                    Entities.Sort(CompareNodes);
                                    foreach (XmlNode item in Entities)
                                        if (item.Value != null) ((ExpressEnumerable) val).Add(item.Value);
                                    break;
                                case CollectionType.Set:
                                    foreach (XmlNode item in Entities)
                                        if (item.Value != null) ((ExpressEnumerable) val).Add(item.Value);
                                    break;
                                default:
                                    throw new Exception("Unknown list type, " + CType);
                            }
                        }
                        else if (val is ExpressComplexType)
                        {
                            Entities.Sort(CompareNodes);
                            foreach (XmlNode item in Entities)
                                if (item.Value != null) ((ExpressComplexType) val).Add(item.Value);
                        }
                        else
                            throw new Exception("Property is being treated as an enumerable but is not, " +
                                                Property.Name);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Error population collection " + Property.Name, e);
                }
            }
        }

        private XmlNode _currentNode;

        public int Read(XbimMemoryModel model, XmlReader input)
        {
            int errors = 0;
            // Read until end of file

            while (_currentNode == null && input.Read()) //read through to UOS
            {
                switch (input.NodeType)
                {
                    case XmlNodeType.Element:
                        if (string.Compare(input.Name, "uos", true) == 0)
                            _currentNode = new XmlPropertyValueCollection(null, null);
                        break;
                    default:
                        break;
                }
            }
            while (input.Read())
            {
                switch (input.NodeType)
                {
                    case XmlNodeType.Element:
                        StartElement(input, model);
                        break;
                    case XmlNodeType.EndElement:
                        EndElement(input);
                        break;
                    case XmlNodeType.Text:
                        SetValue(input);
                        break;
                    default:
                        break;
                }
            }
            if (errors == 0)
            {
                foreach (var instance in _instances.Values)
                {
                    model.Instances.Add(instance);
                }
            }
            return errors;
        }

        private void SetValue(XmlReader input)
        {
            try
            {
                //Debug.WriteLine("Value is " + input.Value);
                XmlValueType vt = _currentNode as XmlValueType;
                XmlPropertyValue pv = _currentNode as XmlPropertyValue;
                if (vt == null && pv == null)
                {
                    throw new Exception("Trying to assign value to non-value type, value =  " + input.Value);
                }
                else
                {
                    Type t;
                    if (pv == null)
                        t = vt.Type;
                    else
                        t = pv.Property.PropertyType;
                    object[] param = new object[1];
                    param[0] = input.Value;
                    object o;
                    if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof (Nullable<>)))
                        // We are dealing with a generic type that is nullable get underlying type
                        t = Nullable.GetUnderlyingType(t);
                    if (t.IsEnum)
                        o = Enum.Parse(t, input.Value, true);
                    
                    else if (typeof(ExpressType).IsAssignableFrom(t))
                    {
                        ExpressType et = (ExpressType)(Activator.CreateInstance(t));
                        //param[0] = (et.UnderlyingSystemType)input.Value;
                        //dynamic d = Activator.CreateInstance(et.UnderlyingSystemType);
                        param[0] = Convert.ChangeType(param[0], et.UnderlyingSystemType);

                        o = Activator.CreateInstance(t, param);

                        
                    }
                   
                    else if (t == typeof(double))
                        o = Convert.ToDouble(input.Value);
                    else if (t == typeof(int))
                        o = Convert.ToInt32(input.Value);
                    else if (t == typeof(long))
                        o = Convert.ToInt64(input.Value);
                    else if (t == typeof(bool))
                        o = Convert.ToBoolean(input.Value);
                    else if (t == typeof(string))
                        o = input.Value;
                    else
                        throw new Exception("Error converting Text to type, " + t.Name);
                    if (vt == null)
                        pv.Value = o;
                    else
                        vt.Value = o;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        private void StartElement(XmlReader input, IModel model)
        {
            //Debug.Write(input.Name);
            try
            {
                string strId = input.GetAttribute("id");
                long id;

                string inputName = input.Name;
                if (inputName.Contains("-wrapper") && inputName.StartsWith("ex:") == false)
                    inputName = input.Name.Substring(0, input.Name.LastIndexOf("-"));

                if (!string.IsNullOrEmpty(strId)) //could be a property or a value type   
                {
                    Match match =  Regex.Match(strId, @"\d+");
                    if (!match.Success)
                        throw new Exception(String.Format("Illegal entity id: {0}", strId));
                    
                    id = Convert.ToInt64(match.Value); 

                    //Debug.Write(" is Instance, id = " + id);
                    IfcType ifcType;
                    if (IfcInstances.IfcTypeLookup.TryGetValue(input.Name.ToUpper(), out ifcType))
                        //we have an IPersistIfc
                    {
                        IPersistIfcEntity ent;
                        if (!_instances.TryGetValue(id, out ent))
                        {
                            //not been declared in a ref yet
                            ent = (IPersistIfcEntity) Activator.CreateInstance(ifcType.Type);
                            ent.Bind(model, id);
                            _instances.Add(id, ent);
                        }
                        XmlEntity xmlEnt = new XmlEntity(_currentNode, ent);
                        string pos = input.GetAttribute("pos");
                        if (!string.IsNullOrEmpty(pos))
                            xmlEnt.Position = Convert.ToInt32(pos);

                        if (!input.IsEmptyElement) //trap in case endelement isn't called
                            _currentNode = xmlEnt;
                        else
                        {
                            if (_currentNode is XmlPropertyValue)
                                (_currentNode).Value = ent;
                            else if (_currentNode is XmlPropertyValueCollection)
                                ((XmlPropertyValueCollection) _currentNode).Entities.Add(xmlEnt);
                        }
                    }
                    else
                        throw new Exception("Unexpected Element found " + input.Name);
                }
                else
                {
                    string strRefid = input.GetAttribute("ref");
                    if (!string.IsNullOrEmpty(strRefid)) //it is a reference to an instance
                    {
                        Match match = Regex.Match(strRefid, @"\d+");
                        if (!match.Success)
                            throw new Exception(String.Format("Illegal entity refid: {0}", strRefid));

                        long refid = Convert.ToInt64(match.Value); 

                        //Debug.Write(" is a reference = " + refid);
                        IPersistIfcEntity refEnt;
                        if (!_instances.TryGetValue(refid, out refEnt))
                        {
                            //not declared yet
                            IfcType ifcType;
                            if (IfcInstances.IfcTypeLookup.TryGetValue(input.Name.ToUpper(), out ifcType))
                                //we have an IPersistIfc
                            {
                                refEnt = (IPersistIfcEntity) Activator.CreateInstance(ifcType.Type);
                                refEnt.Bind(model, Convert.ToInt64(refid));
                                _instances.Add(refid, refEnt);
                            }
                            else
                                throw new Exception("Unexpected Element found " + input.Name);
                        }
                        XmlEntity xmlEnt = new XmlEntity(_currentNode, refEnt);
                        string pos = input.GetAttribute("pos");
                        if (!string.IsNullOrEmpty(pos))
                            xmlEnt.Position = Convert.ToInt32(pos);

                        if (!input.IsEmptyElement) //trap in case endelement isn't called
                            _currentNode = xmlEnt;
                        else
                        {
                            if (_currentNode is XmlPropertyValue)
                                (_currentNode).Value = refEnt;
                            else if (_currentNode is XmlPropertyValueCollection)
                                ((XmlPropertyValueCollection) _currentNode).Entities.Add(xmlEnt);
                        }
                    }
                    else
                    {
                        IfcType ifcType;
                        if (IfcInstances.IfcTypeLookup.TryGetValue(inputName.ToUpper(), out ifcType))
                            //we have a value or an entity
                        {
                            if (typeof (ExpressType).IsAssignableFrom(ifcType.Type))
                            {
                                ////Debug.WriteLine(" is Value Type");
                                XmlValueType xmlValt = new XmlValueType(_currentNode, ifcType.Type);
                                string pos = input.GetAttribute("pos");
                                if (!string.IsNullOrEmpty(pos))
                                    xmlValt.Position = Convert.ToInt32(pos);
                                if (!input.IsEmptyElement) _currentNode = xmlValt;
                            }
                            else //it is an entity with no ID or REFID
                            {
                                //not been declared in a ref yet
                                IPersistIfcEntity ent = (IPersistIfcEntity) Activator.CreateInstance(ifcType.Type);
                                XmlEntity xmlEnt = new XmlEntity(_currentNode, ent);
                                string pos = input.GetAttribute("pos");
                                if (!string.IsNullOrEmpty(pos))
                                    xmlEnt.Position = Convert.ToInt32(pos);
                                if (!input.IsEmptyElement) //trap in case endelement isn't called
                                    _currentNode = xmlEnt;
                                else
                                {
                                    if (_currentNode is XmlPropertyValue)
                                        (_currentNode).Value = ent;
                                    else if (_currentNode is XmlPropertyValueCollection)
                                        ((XmlPropertyValueCollection) _currentNode).Entities.Add(xmlEnt);
                                }
                            }
                        }
                        else //we have a property, could be a value or a collection or an express basic type
                        {
                            Type basicType;
                            if (primitives.TryGetValue(input.Name, out basicType)) //we have a basic type
                            {
                                //Debug.WriteLine(" is Basic Type " + basicType.Name);
                                XmlValueType xmlValt = new XmlValueType(_currentNode, basicType);
                                string pos = input.GetAttribute("pos");
                                if (!string.IsNullOrEmpty(pos))
                                    xmlValt.Position = Convert.ToInt32(pos);
                                if (!input.IsEmptyElement) _currentNode = xmlValt;
                            }
                            else //we have a property
                            {
                                string cType = input.GetAttribute("ex:cType");
                                if (string.IsNullOrEmpty(cType)) //we have a simple property
                                {
                                    //parent must contain the instance
                                    if (_currentNode is XmlEntity)
                                    {
                                        if (!input.IsEmptyElement)
                                        {
                                            IfcType t = IfcInstances.IfcEntities[((XmlEntity) _currentNode).Entity];

                                            foreach (KeyValuePair<int, IfcMetaProperty> p in t.IfcProperties)
                                            {
                                                if (p.Value.PropertyInfo.Name == input.Name)
                                                    //this is the property to set
                                                {
                                                    Type ct = p.Value.PropertyInfo.PropertyType;
                                                    if (ct.IsGenericType &&
                                                        ct.GetGenericTypeDefinition().Equals(typeof (Nullable<>)) &&
                                                        typeof (ExpressComplexType).IsAssignableFrom(
                                                            Nullable.GetUnderlyingType(ct)))
                                                        // We are dealing with a generic type that is nullable get underlying type    
                                                    {
                                                        XmlPropertyValueCollection xmlColl =
                                                            new XmlPropertyValueCollection(_currentNode,
                                                                                           p.Value.PropertyInfo);
                                                        _currentNode = xmlColl;
                                                        xmlColl.CType = CollectionType.List;
                                                    }
                                                    else
                                                    {
                                                        XmlPropertyValue xmlPropVal = new XmlPropertyValue(
                                                            _currentNode, p.Value.PropertyInfo);
                                                        _currentNode = xmlPropVal;
                                                    }
                                                    break;
                                                }
                                            }
                                            if (!input.IsEmptyElement && !(_currentNode is XmlPropertyValue) &&
                                                !(_currentNode is XmlPropertyValueCollection))
                                                throw new Exception("Could not locate and bind property " + input.Name);
                                        }
                                    }

                                    else
                                        throw new Exception("Failed to locate an instance to bind the property " +
                                                            input.Name);
                                }
                                else //we have a collection
                                {
                                    //parent must contain the instance
                                    if (_currentNode is XmlEntity)
                                    {
                                        IfcType t = IfcInstances.IfcEntities[((XmlEntity) _currentNode).Entity];

                                        foreach (KeyValuePair<int, IfcMetaProperty> p in t.IfcProperties)
                                        {
                                            if (p.Value.PropertyInfo.Name == input.Name) //this is the property to set
                                            {
                                                Type pt = p.Value.PropertyInfo.PropertyType;
                                                bool isNullable = pt.IsGenericType &&
                                                                  pt.GetGenericTypeDefinition().Equals(
                                                                      typeof (Nullable<>))
                                                                  &&
                                                                  typeof (ExpressComplexType).IsAssignableFrom(
                                                                      Nullable.GetUnderlyingType(pt));
                                                if (
                                                    !(typeof (ExpressEnumerable).IsAssignableFrom(pt) ||
                                                      typeof (ExpressComplexType).IsAssignableFrom(pt)
                                                      || isNullable))
                                                    throw new Exception("Property is not an Express Collection " +
                                                                        input.Name);
                                                if (!input.IsEmptyElement)
                                                {
                                                    XmlPropertyValueCollection xmlColl =
                                                        new XmlPropertyValueCollection(_currentNode,
                                                                                       p.Value.PropertyInfo);
                                                    _currentNode = xmlColl;
                                                    switch (cType)
                                                    {
                                                        case "list":
                                                            xmlColl.CType = CollectionType.List;
                                                            break;
                                                        case "list-unique":
                                                            xmlColl.CType = CollectionType.ListUnique;
                                                            break;
                                                        case "set":
                                                            xmlColl.CType = CollectionType.Set;
                                                            break;
                                                        default:
                                                            break;
                                                    }
                                                }
                                                break;
                                            }
                                        }
                                        if (!input.IsEmptyElement && !(_currentNode is XmlPropertyValueCollection))
                                            throw new Exception("Could not locate and bind property " + input.Name);
                                    }
                                    else
                                        throw new Exception("Failed to locate an instance to bind the property " +
                                                            input.Name);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error reading IfcXml file at " + input.Name, e);
            }
        }

        private void EndElement(XmlReader input)
        {
            try
            {
                //Debug.WriteLine("End " + input.Name);
                if (_currentNode is XmlValueType) //assign value to parent property
                {
                    if (_currentNode.Parent is XmlPropertyValue)
                    {
                        (_currentNode.Parent).Value = (_currentNode).Value;
                    }
                    else if (_currentNode.Parent is XmlPropertyValueCollection)
                    {
                        ((XmlPropertyValueCollection) _currentNode.Parent).Entities.Add(_currentNode);
                    }
                    else
                        throw new Exception("Could not assign a value to a property, " + input.Name);
                }
                else if (_currentNode is XmlEntity) //assign entity to parent property
                {
                    if (_currentNode.Parent is XmlPropertyValue)
                    {
                        (_currentNode.Parent).Value = ((XmlEntity) _currentNode).Entity;
                    }
                    else if (_currentNode.Parent is XmlPropertyValueCollection)
                    {
                        ((XmlPropertyValueCollection) _currentNode.Parent).Entities.Add(_currentNode);
                    }
                    else
                        throw new Exception("Could not assing a entity to a property, " + input.Name);
                }
                else if (_currentNode is XmlPropertyValueCollection)
                    //roll up all elements into the collection observing the order flag
                {
                    ((XmlPropertyValueCollection) _currentNode).SetCollection();
                }

                if (_currentNode.Parent != null)
                    _currentNode = _currentNode.Parent; //drop this node we have reached UOS
            }
            catch (Exception e)
            {
                throw new Exception("Error reading IfcXML data at node " + input.Name, e);
            }
        }
    }
}