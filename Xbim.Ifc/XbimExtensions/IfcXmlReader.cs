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
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

#endregion

namespace Xbim.XbimExtensions
{
    public class IfcXmlReader
    {
        private static readonly Dictionary<string, IfcParserType> primitives;
        private readonly HashSet<long> _instances = new HashSet<long>();

        static IfcXmlReader()
        {
            primitives = new Dictionary<string, IfcParserType>();
            primitives.Add("ex:double-wrapper", IfcParserType.Real);
            primitives.Add("ex:long-wrapper", IfcParserType.Integer);
            primitives.Add("ex:string-wrapper", IfcParserType.String);
            primitives.Add("ex:integer-wrapper", IfcParserType.Integer);
            primitives.Add("ex:boolean-wrapper", IfcParserType.Boolean);
            primitives.Add("ex:logical-wrapper", IfcParserType.Boolean);
            primitives.Add("ex:decimal-wrapper", IfcParserType.Real);
            primitives.Add("ex:hexBinary-wrapper", IfcParserType.HexaDecimal);
            primitives.Add("ex:base64Binary-wrapper", IfcParserType.Entity);
            primitives.Add(typeof(double).Name, IfcParserType.Real);
            primitives.Add(typeof(long).Name, IfcParserType.Integer);
            primitives.Add(typeof(string).Name, IfcParserType.String);
            primitives.Add(typeof(int).Name, IfcParserType.Integer);
            primitives.Add(typeof(bool).Name, IfcParserType.Boolean);
            primitives.Add("Enum", IfcParserType.Enum);
        }

        private abstract class XmlNode
        {
            public readonly XmlNode Parent;
            public int? Position;
            internal XmlNode()
            {

            }
            public XmlNode(XmlNode parent)
            {
                Parent = parent;
            }
        }

        private class XmlEntity : XmlNode
        {
            public IPersistIfcEntity Entity;

            public XmlEntity(XmlNode parent, IPersistIfcEntity ent)
                : base(parent)
            {
                Entity = ent;
            }

        }

        private class XmlExpressType : XmlNode
        {
            private string _value;
            public string Value
            {
                get { return _value; }
                set { _value = value; }
            }
            public readonly Type Type;

            public XmlExpressType(XmlNode parent, Type type)
                : base(parent)
            {
                Type = type;
            }
        }

        private class XmlBasicType : XmlNode
        {
            private string _value;


            public string Value
            {
                get { return _value; }
                set { _value = value; }
            }

            public readonly IfcParserType Type;

            public XmlBasicType(XmlNode parent, IfcParserType type)
                : base(parent)
            {
                Type = type;
            }
        }

        private class XmlProperty : XmlNode
        {
            public readonly PropertyInfo Property;
            public readonly int PropertyIndex;

            public XmlProperty(XmlNode parent, PropertyInfo prop, int propIndex)
                : base(parent)
            {
                Property = prop;
                PropertyIndex = propIndex;
            }

            public void SetValue(string val, IfcParserType parserType)
            {
                PropertyValue propVal = new PropertyValue();
                propVal.Init(val, parserType);
                ((XmlEntity)Parent).Entity.IfcParse(PropertyIndex - 1, propVal);
            }

            public void SetValue(object o)
            {
                PropertyValue propVal = new PropertyValue();
                propVal.Init(o);
                ((XmlEntity)Parent).Entity.IfcParse(PropertyIndex - 1, propVal);
            }
        }

        public enum CollectionType
        {
            List,
            ListUnique,
            Set
        }

        private class XmlUosCollection : XmlCollectionProperty
        {
            public XmlUosCollection()
            {
            }


            internal override void SetCollection(IModel model, XmlReader reader)
            {

            }
        }



        private class XmlCollectionProperty : XmlNode
        {
            internal XmlCollectionProperty()
            {

            }

            public XmlCollectionProperty(XmlNode parent, PropertyInfo prop, int propIndex)
                : base(parent)
            {
                Property = prop;
                PropertyIndex = propIndex;
            }

            public readonly List<XmlNode> Entities = new List<XmlNode>();
            public readonly PropertyInfo Property;
            public CollectionType CType = CollectionType.Set;
            public readonly int PropertyIndex;
            public static int CompareNodes(XmlNode a, XmlNode b)
            {
                if (a.Position > b.Position)
                    return 1;
                else if (a.Position < b.Position)
                    return -1;
                else
                    return 0;
            }

            internal virtual void SetCollection(IModel model, XmlReader reader)
            {
                switch (CType)
                {
                    case CollectionType.List:
                    case CollectionType.ListUnique:
                        Entities.Sort(CompareNodes);
                        break;
                    case CollectionType.Set:
                        break;
                    default:
                        throw new Exception("Unknown list type, " + CType);
                }
                foreach (XmlNode item in Entities)
                {

                    //if (item.Value != null) SetValue(model, reader, item);
                }
            }
        }

        private XmlNode _currentNode;

        public int Read(IModel model, XmlReader input)
        {
            int errors = 0;
            // Read until end of file

            while (_currentNode == null && input.Read()) //read through to UOS
            {
                switch (input.NodeType)
                {
                    case XmlNodeType.Element:
                        if (string.Compare(input.Name, "uos", true) == 0)
                            _currentNode = new XmlUosCollection();
                        break;
                    default:
                        break;
                }
            }

            XmlNodeType prevInputType = XmlNodeType.None;
            string prevInputName = "";

            // set counter for start of every element that is not empty, and reduce it on every end of that element
            int counter = 0;
            // this will create id of each element
            Dictionary<string, int> ids = new Dictionary<string, int>();

            while (input.Read())
            {
                Application.DoEvents();

                switch (input.NodeType)
                {
                    case XmlNodeType.Element:
                        StartElement(model, input);
                        if (!input.IsEmptyElement)
                        {
                            // save the id
                            if (!String.IsNullOrEmpty(input.GetAttribute("id")))
                            {
                                ids.Add(GetId(input).ToString(), counter);
                            }
                            counter++;
                        }
                        break;
                    case XmlNodeType.EndElement:
                        EndElement(model, input, prevInputType, prevInputName);
                        counter--;
                        foreach (KeyValuePair<string, int> item in ids)
                        {
                            if (item.Value == counter)
                            {
                                AppendToStream(null, Convert.ToInt64(item.Key));
                                ids.Remove(item.Key);
                                break;
                            }
                        }
                        break;
                    case XmlNodeType.Text:
                        SetValue(model, input);
                        break;
                    default:
                        break;
                }
                prevInputType = input.NodeType;
                prevInputName = input.Name;



                }

            //long highestId = 0;


            return errors;
        }

        public delegate void EventHandler(BinaryWriter br, long el);
        public event EventHandler AppendToStream = delegate { };

        private void StartElement(IModel model, XmlReader input)
        {
            string elementName = input.Name;
            //bool isRefType = false;
            long id = GetId(input);

            IfcType ifcType;
            
            IfcParserType parserType;
            IfcMetaProperty prop;
            int propIndex;


            if (id > -1 && IsIfcEntity(elementName, out ifcType)) //we have an element which is an Ifc Entity
            {
                IPersistIfcEntity ent;
                if (!_instances.Contains(id))
                {
                    // not been declared in a ref yet
                    // model.New creates an instance uisng type and id
                    ent = model.AddNew(ifcType, id);
                    _instances.Add(id);
                }
                else
                {
                    ent = model.GetInstance(id);
                }

                XmlEntity xmlEnt = new XmlEntity(_currentNode, ent);
                string pos = input.GetAttribute("pos");
                if (!string.IsNullOrEmpty(pos))
                    xmlEnt.Position = Convert.ToInt32(pos);


                if (!input.IsEmptyElement)
                    _currentNode = xmlEnt;
                else if (_currentNode is XmlProperty)
                {
                    // if it is a ref then it will be empty element and wont have an end tag
                    // so nither SetValue nor EndElement will be called, so set the value of ref here e.g. #3
                    ((XmlProperty)(_currentNode)).SetValue(ent);
                }
                else if (_currentNode is XmlCollectionProperty && !(_currentNode.Parent is XmlUosCollection))
                    ((XmlCollectionProperty)_currentNode).Entities.Add(xmlEnt);
            }
            else if (input.IsEmptyElement)
            {
                if (IsIfcProperty(elementName, out propIndex, out prop))
                {
                    XmlProperty node = new XmlProperty(_currentNode, prop.PropertyInfo, propIndex); ;
                    PropertyValue propVal = new PropertyValue();
                    Type t = node.Property.PropertyType;

                    if (!typeof(ExpressEnumerable).IsAssignableFrom(t)) // if its a empty collection then dont do anything
                    {
                        if (t != null && t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                            t = Nullable.GetUnderlyingType(t);
                        ExpressType et = null;
                        if (t != null && typeof(ExpressType).IsAssignableFrom(t))
                            et = (ExpressType)(Activator.CreateInstance(t));

                        IfcParserType pt;
                        if (et != null)
                            pt = primitives[et.UnderlyingSystemType.Name];
                        else
                        {
                            if (t.IsEnum)
                            {
                                pt = IfcParserType.Enum;
                            }
                            else
                                pt = primitives[t.Name];
                        }

                        if (pt.ToString().ToLower() == "string")
                            propVal.Init("'" + input.Value + "'", pt);
                        else
                        {
                            if (pt.ToString().ToLower() == "boolean")
                                propVal.Init(Convert.ToBoolean(input.Value) ? ".T." : ".F", pt);
                            else
                                propVal.Init(input.Value, pt);
                        }
                        ((XmlEntity)node.Parent).Entity.IfcParse(node.PropertyIndex - 1, propVal);
                    }

                }
                else if (IsIfcType(elementName, out ifcType))
                {
                    IPersistIfc ent;
                    object[] param = new object[1];
                    param[0] = ""; // empty element
                    ent = (IPersistIfc)Activator.CreateInstance(ifcType.Type, param);

                    ((XmlProperty)_currentNode).SetValue(ent);
                }

                return;
            }
            else if (id == -1 && IsIfcProperty(elementName, out propIndex, out prop)) //we have an element which is a property
            {

                string cType = input.GetAttribute("ex:cType");

                if (!string.IsNullOrEmpty(cType) && IsCollection(prop)) //the property is a collection
                {
                    XmlCollectionProperty xmlColl = new XmlCollectionProperty(_currentNode, prop.PropertyInfo, propIndex);
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

                    _currentNode = xmlColl;
                }
                else //it is a simple value property;
                {


                    // its parent can be a collection, if yes then this property needs to be added to parent
                    XmlNode n = new XmlProperty(_currentNode, prop.PropertyInfo, propIndex);
                    if (_currentNode is XmlCollectionProperty && !(_currentNode.Parent is XmlUosCollection))
                        ((XmlCollectionProperty)_currentNode).Entities.Add(n);

                    if (!input.IsEmptyElement) _currentNode = n;
                }
            }
            else if (id == -1 && IsIfcType(elementName, out ifcType)) // we have an Ifc ExpressType
            {


                // its parent can be a collection, if yes then this property needs to be added to parent
                XmlNode n = new XmlExpressType(_currentNode, ifcType.Type);
                if (_currentNode is XmlCollectionProperty && !(_currentNode.Parent is XmlUosCollection))
                    ((XmlCollectionProperty)_currentNode).Entities.Add(n);

                if (!input.IsEmptyElement) _currentNode = n;
            }
            else if (id == -1 && IsPrimitiveType(elementName, out parserType)) // we have an basic type i.e. double, bool etc
            {
                // its parent can be a collection, if yes then this property needs to be added to parent
                XmlNode n = new XmlBasicType(_currentNode, parserType);
                if (_currentNode is XmlCollectionProperty && !(_currentNode.Parent is XmlUosCollection))
                    ((XmlCollectionProperty)_currentNode).Entities.Add(n);

                if (!input.IsEmptyElement) _currentNode = n;
            }
            else
                throw new Exception("Illegal XML element tag");
        }

        private bool IsIfcProperty(string elementName, out int index, out IfcMetaProperty prop)
        {
            IfcType ifcType;
            XmlEntity xmlEntity = _currentNode as XmlEntity;
            if (xmlEntity != null && !IfcInstances.IfcTypeLookup.TryGetValue(elementName.ToUpper(), out ifcType))
            {
                IfcType t = IfcInstances.IfcEntities[xmlEntity.Entity];

                foreach (KeyValuePair<int, IfcMetaProperty> p in t.IfcProperties)
                {
                    int propIndex = p.Key;
                    if (p.Value.PropertyInfo.Name == elementName)
                    //this is the property to set
                    {
                        prop = p.Value;
                        index = p.Key;
                        return true;
                    }
                }
            }
            prop = null;
            index = -1;
            return false;
        }

        private bool IsCollection(IfcMetaProperty prop)
        {
            return typeof(ExpressEnumerable).IsAssignableFrom(prop.PropertyInfo.PropertyType);
        }

        private bool IsPrimitiveType(string elementName, out IfcParserType basicType)
        {
            return primitives.TryGetValue(elementName, out basicType); //we have a primitive type

        }

        private bool IsIfcType(string elementName, out IfcType ifcType)
        {
            bool ok = IfcInstances.IfcTypeLookup.TryGetValue(elementName.ToUpper(), out ifcType);
            if (!ok)
            {

                if (elementName.Contains("-wrapper") && elementName.StartsWith("ex:") == false) // we have an inline type definition
                {
                    string inputName = elementName.Substring(0, elementName.LastIndexOf("-"));
                    ok = IfcInstances.IfcTypeLookup.TryGetValue(inputName.ToUpper(), out ifcType);
                }
            }
            return ok && typeof(ExpressType).IsAssignableFrom(ifcType.Type);
        }

        private long GetId(XmlReader input)
        {
            string strId = input.GetAttribute("id");
            if (string.IsNullOrEmpty(strId))
                strId = input.GetAttribute("ref");

            if (!string.IsNullOrEmpty(strId)) //must be a new instance or a reference to an existing one  
            {
                // if we have id or refid then remove letters and get the number part
                Match match = Regex.Match(strId, @"\d+");
                if (!match.Success)
                    throw new Exception(String.Format("Illegal entity id: {0}", strId));
                return Convert.ToInt64(match.Value);
            }
            else
                return -1;

        }

        private bool IsIfcEntity(string elementName, out IfcType ifcType)
        {

            return IfcInstances.IfcTypeLookup.TryGetValue(elementName.ToUpper(), out ifcType);
        }

        private void EndElement(IModel model, XmlReader input, XmlNodeType prevInputType, string prevInputName)
        {
            try
            {
                // before end element, we need to deal with SetCollection
                if (_currentNode is XmlCollectionProperty)
                {
                    // SetCollection will handle SetValue for Collection
                    CollectionType CType = ((XmlCollectionProperty)_currentNode).CType;
                    switch (CType)
                    {
                        case CollectionType.List:
                        case CollectionType.ListUnique:
                            ((XmlCollectionProperty)_currentNode).Entities.Sort(XmlCollectionProperty.CompareNodes);
                            break;
                        case CollectionType.Set:
                            break;
                        default:
                            throw new Exception("Unknown list type, " + CType);
                    }
                    foreach (XmlNode item in ((XmlCollectionProperty)_currentNode).Entities)
                    {

                        if (item is XmlEntity)
                        {
                            XmlEntity node = (XmlEntity)item;
                            XmlEntity collectionOwner = item.Parent.Parent as XmlEntity;
                            XmlCollectionProperty collection = item.Parent as XmlCollectionProperty; //the collection to add to;
                            IPersistIfc ifcCollectionOwner = collectionOwner.Entity;
                            PropertyValue pv = new PropertyValue();
                            pv.Init(node.Entity);
                            ifcCollectionOwner.IfcParse(collection.PropertyIndex - 1, pv);
                        }

                    }

                }
                else if (_currentNode.Parent is XmlProperty)
                {
                    XmlProperty propNode = (XmlProperty)_currentNode.Parent;
                    if (_currentNode is XmlEntity)
                    {
                        XmlEntity entityNode = (XmlEntity)_currentNode;
                        propNode.SetValue(entityNode.Entity);
                    }
                    else if (_currentNode is XmlExpressType)
                    {
                        //create ExpressType, call ifcparse with propindex and object
                        //((XmlProperty)_currentNode.Parent).SetValue((XmlExpressType)_currentNode);

                        XmlExpressType expressNode = (XmlExpressType)_currentNode;
                        if (expressNode.Type != propNode.Property.PropertyType)
                        {
                            //propNode.SetValue(expressNode);
                            IfcType ifcType;
                            if (IsIfcType(input.Name, out ifcType))
                            //we have an IPersistIfc
                            {
                                IPersistIfc ent;
                                object[] param = new object[1];
                                param[0] = expressNode.Value;
                                ent = (IPersistIfc)Activator.CreateInstance(ifcType.Type, param);

                                propNode.SetValue(ent);
                            }
                        }
                        else
                        {
                            propNode.SetValue(expressNode.Value, primitives[expressNode.Type.Name]);
                        }
                    }
                    else if (_currentNode is XmlBasicType)
                    {
                        //set PropertyValue to write type boolean, integer, call ifcparse with string

                        XmlBasicType basicNode = (XmlBasicType)_currentNode;
                        propNode.SetValue(basicNode.Value, basicNode.Type);
                    }
                }


                else if (prevInputType == XmlNodeType.Element && prevInputName == input.Name &&
                    _currentNode is XmlProperty && _currentNode.Parent != null && _currentNode.Parent is XmlEntity)
                {
                    // WE SHOULDNT EXECUTE THE FOLLOWING CODE IF THIS PROPERTY ALREADY CALLED SETVALUE
                    XmlProperty node = (XmlProperty)_currentNode;
                    PropertyValue propVal = new PropertyValue();
                    Type t = node.Property.PropertyType;
                    if (t != null && t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                        t = Nullable.GetUnderlyingType(t);
                    ExpressType et = null;
                    if (t != null && typeof(ExpressType).IsAssignableFrom(t))
                        et = (ExpressType)(Activator.CreateInstance(t));

                    IfcParserType pt = IfcParserType.Undefined;
                    if (et != null)
                        pt = primitives[et.UnderlyingSystemType.Name];
                    else
                    {
                        if (t.IsEnum)
                        {
                            pt = IfcParserType.Enum;
                        }
                        else if (primitives.ContainsKey(t.Name))
                            pt = primitives[t.Name];
                    }

                    if (pt != IfcParserType.Undefined)
                    {
                        if (pt.ToString().ToLower() == "string")
                            propVal.Init("'" + input.Value + "'", pt);
                        else
                        {
                            if (pt.ToString().ToLower() == "boolean")
                                propVal.Init(Convert.ToBoolean(input.Value) ? ".T." : ".F", pt);
                            else
                                propVal.Init(input.Value, pt);
                        }

                        ((XmlEntity)node.Parent).Entity.IfcParse(node.PropertyIndex - 1, propVal);
                    }


                }

                else if (_currentNode.Parent is XmlCollectionProperty && !(_currentNode.Parent is XmlUosCollection))
                {
                    if (_currentNode is XmlEntity)
                    {
                        XmlEntity node = (XmlEntity)_currentNode;
                        XmlEntity collectionOwner = _currentNode.Parent.Parent as XmlEntity;
                        XmlCollectionProperty collection = _currentNode.Parent as XmlCollectionProperty; //the collection to add to;
                        IPersistIfc ifcCollectionOwner = collectionOwner.Entity;
                        PropertyValue pv = new PropertyValue();
                        pv.Init(node.Entity);
                        ifcCollectionOwner.IfcParse(collection.PropertyIndex - 1, pv);
                    }
                    else if (_currentNode is XmlExpressType)
                    {
                        XmlExpressType expressType = (XmlExpressType)_currentNode;
                        Type t = expressType.Type;
                        if (t != null && t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                            t = Nullable.GetUnderlyingType(t);
                        ExpressType et = null;
                        if (t != null && typeof(ExpressType).IsAssignableFrom(t))
                            et = (ExpressType)(Activator.CreateInstance(t));

                        IfcParserType pt;
                        if (et != null)
                            pt = primitives[et.UnderlyingSystemType.Name];
                        else
                        {
                            if (t.IsEnum)
                            {
                                pt = IfcParserType.Enum;
                            }
                            else
                                pt = primitives[t.Name];
                        }

                        XmlEntity collectionOwner = _currentNode.Parent.Parent as XmlEntity; //go to owner of collection
                        XmlCollectionProperty collection = _currentNode.Parent as XmlCollectionProperty; //the collection to add to;
                        IPersistIfc ifcCollectionOwner = collectionOwner.Entity;
                        PropertyValue pv = new PropertyValue();

                        if (pt.ToString().ToLower() == "string")
                            pv.Init("'" + (expressType).Value + "'", pt);
                        else
                            pv.Init((expressType).Value, pt);

                        bool found = false;
                        if (collection.Property.PropertyType.BaseType != null)
                        {
                            // this can be a scenario of IfcTrimmedCurve
                            if (collection.Property.PropertyType.BaseType.GetGenericArguments().Length > 0)
                            {
                                if (collection.Property.PropertyType.BaseType.GetGenericArguments()[0].Name != input.Name)
                                {
                                    found = true;
                                    object[] param = new object[1];
                                    param[0] = expressType.Value;
                                    et = (ExpressType)(Activator.CreateInstance(expressType.Type, param));

                                    pv.Init(et);
                                    ifcCollectionOwner.IfcParse(collection.PropertyIndex - 1, pv);
                                }
                            }
                        }

                        if (!found)
                            ifcCollectionOwner.IfcParse(collection.PropertyIndex - 1, pv);
                    }
                    else if (_currentNode is XmlBasicType)
                    {
                        XmlBasicType basicNode = (XmlBasicType)_currentNode;
                        XmlEntity collectionOwner = _currentNode.Parent.Parent as XmlEntity;
                        XmlCollectionProperty collection = _currentNode.Parent as XmlCollectionProperty; //the collection to add to;
                        IPersistIfc ifcCollectionOwner = collectionOwner.Entity;
                        PropertyValue pv = new PropertyValue();
                        pv.Init(basicNode.Value, basicNode.Type);
                        ifcCollectionOwner.IfcParse(collection.PropertyIndex - 1, pv);
                    }


                }

                if (_currentNode.Parent != null) // we are not at UOS yet
                    _currentNode = _currentNode.Parent;


            }
            catch (Exception e)
            {
                throw new Exception("Error reading IfcXML data at node " + input.Name, e);
            }
        }



        private void SetValue(IModel model, XmlReader input)
        {
            try
            {
                if (_currentNode is XmlExpressType)
                {
                    XmlExpressType node = (XmlExpressType)_currentNode;
                    node.Value = input.Value;
                }
                else if (_currentNode is XmlBasicType)
                {
                    XmlBasicType node = (XmlBasicType)_currentNode;
                    node.Value = input.Value;

                }
                else if (_currentNode is XmlProperty)
                {
                    XmlProperty node = (XmlProperty)_currentNode;
                    PropertyValue propVal = new PropertyValue();
                    Type t = node.Property.PropertyType;
                    if (t != null && t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                        t = Nullable.GetUnderlyingType(t);
                    ExpressType et = null;
                    if (t != null && typeof(ExpressType).IsAssignableFrom(t))
                        et = (ExpressType)(Activator.CreateInstance(t));

                    IfcParserType pt;
                    if (et != null)
                        pt = primitives[et.UnderlyingSystemType.Name];
                    else
                    {
                        if (t.IsEnum)
                        {
                            pt = IfcParserType.Enum;
                        }
                        else
                            pt = primitives[t.Name];
                    }

                    if (pt.ToString().ToLower() == "string")
                        propVal.Init("'" + input.Value + "'", pt);
                    else
                    {
                        if (pt.ToString().ToLower() == "boolean")
                            propVal.Init(Convert.ToBoolean(input.Value) ? ".T." : ".F", pt);
                        else
                            propVal.Init(input.Value, pt);
                    }

                    ((XmlEntity)node.Parent).Entity.IfcParse(node.PropertyIndex - 1, propVal);

                }

            }
            catch (Exception e)
            {
                throw new Exception("Error reading IfcXML data at node " + input.Name, e);
            }
        }


    }
}